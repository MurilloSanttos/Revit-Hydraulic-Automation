# Padrão de Execução — Pipeline / Chain of Responsibility

> Especificação completa do padrão Pipeline para controle de fluxo do sistema hidráulico, com contexto compartilhado, aprovação humana, retry/rollback e extensibilidade, para uso no PluginCore.

---

## 1. Definição do Padrão

### 1.1 O que é

O **Pipeline** combina dois padrões:

- **Chain of Responsibility** — cada etapa decide se pode executar e delega para a próxima
- **Pipeline** — dados fluem por uma sequência de transformações, acumulando resultados

No nosso sistema, cada etapa recebe um `PipelineContext` (bag de dados compartilhada), executa sua lógica, escreve seu resultado no contexto, e devolve o controle ao Pipeline Runner.

### 1.2 Por que foi escolhido

| Alternativa | Problema |
|-------------|---------|
| Monolito sequencial | Impossível pausar para aprovação, impossível retry parcial |
| Event-driven | Complexidade excessiva para dev solo, difícil de debugar |
| State Machine pura | Bom para estado, ruim para dados (precisa do Context) |
| **Pipeline + Context** | ✅ Sequencial + pausável + dados compartilhados + testável |

### 1.3 Benefícios

```
✅ Cada etapa é uma classe isolada → testável com mock
✅ Contexto compartilhado → dados fluem sem acoplamento
✅ Extensível → nova etapa = nova classe + registro
✅ Pausável → WaitingApproval entre etapas
✅ Rollback → cada etapa sabe desfazer seu trabalho
✅ Retry → reexecutar uma etapa com mesmo contexto
✅ Logging → automático via interceptor no Runner
✅ Independente do Revit → zero imports de API
```

---

## 2. Arquitetura da Solução

```
╔═══════════════════════════════════════════════════════════════════╗
║                        PipelineRunner                             ║
║  (controla fluxo, pausa, retry, rollback, logging)                ║
╠═══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐            ║
║  │ Step 01 │→│ Step 02 │→│ Step 03 │→│ Step 04 │→ ...         ║
║  │ Detect  │  │Classify │  │ Equip   │  │ Points  │             ║
║  │ Rooms   │  │ Rooms   │  │ Insert  │  │ Generate│             ║
║  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘            ║
║       │            │            │            │                    ║
║       ▼            ▼            ▼            ▼                    ║
║  ╔═══════════════════════════════════════════════════╗            ║
║  ║              PipelineContext                       ║            ║
║  ║  ┌──────────────────────────────────────────────┐ ║            ║
║  ║  │ Rooms, Equipment, Points, Networks,          │ ║            ║
║  ║  │ SizingResults, Tables, Logs, State           │ ║            ║
║  ║  └──────────────────────────────────────────────┘ ║            ║
║  ╚═══════════════════════════════════════════════════╝            ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

### 2.1 Componentes

| Componente | Responsabilidade |
|-----------|-----------------|
| `IProcessingStep` | Contrato de uma etapa (Execute, Validate, Rollback) |
| `PipelineContext` | Bag de dados compartilhada entre etapas |
| `PipelineRunner` | Controla execução, pausa, retry, rollback |
| `StepResult` | Resultado de uma etapa (sucesso/falha/dados) |
| `PipelineConfig` | Configuração (quais etapas, opções) |

---

## 3. IProcessingStep — Interface

```csharp
namespace HidraulicoPlugin.Core.Pipeline
{
    /// <summary>
    /// Contrato de uma etapa do pipeline.
    /// Cada etapa é uma classe isolada que:
    /// - Lê dados do contexto (escritos por etapas anteriores)
    /// - Executa sua lógica
    /// - Escreve resultados no contexto
    /// </summary>
    public interface IProcessingStep
    {
        // ── Identidade ──────────────────────────────────────────

        /// <summary>ID único da etapa (ex: "E01").</summary>
        string StepId { get; }

        /// <summary>Nome técnico (ex: "DetectRooms").</summary>
        string Name { get; }

        /// <summary>Descrição humana (ex: "Detecção de ambientes").</summary>
        string Description { get; }

        /// <summary>Ordem de execução (1, 2, 3...).</summary>
        int Order { get; }

        // ── Configuração ────────────────────────────────────────

        /// <summary>IDs das etapas que devem estar concluídas antes.</summary>
        IReadOnlyList<string> Dependencies { get; }

        /// <summary>Se a etapa é obrigatória.</summary>
        bool IsMandatory { get; }

        /// <summary>Se requer aprovação humana após execução.</summary>
        bool RequiresApproval { get; }

        /// <summary>Máximo de tentativas.</summary>
        int MaxAttempts { get; }

        // ── Execução ────────────────────────────────────────────

        /// <summary>
        /// Verifica se a etapa PODE executar dado o contexto atual.
        /// Checa: dependências satisfeitas, dados de entrada presentes.
        /// </summary>
        /// <param name="context">Contexto compartilhado.</param>
        /// <returns>Se pode executar + razão se não pode.</returns>
        StepCanExecuteResult CanExecute(PipelineContext context);

        /// <summary>
        /// Executa a lógica da etapa.
        /// Lê dados do contexto, processa, escreve resultados no contexto.
        /// </summary>
        /// <param name="context">Contexto compartilhado.</param>
        /// <returns>Resultado da execução.</returns>
        StepResult Execute(PipelineContext context);

        /// <summary>
        /// Valida os resultados após execução.
        /// Verifica se os dados de saída são consistentes.
        /// </summary>
        /// <param name="context">Contexto com dados já escritos.</param>
        /// <returns>Resultado da validação.</returns>
        StepValidationResult Validate(PipelineContext context);

        /// <summary>
        /// Desfaz os efeitos da etapa.
        /// Limpa dados escritos no contexto.
        /// </summary>
        /// <param name="context">Contexto a limpar.</param>
        void Rollback(PipelineContext context);
    }
}
```

---

## 4. PipelineContext — Dados Compartilhados

```csharp
namespace HidraulicoPlugin.Core.Pipeline
{
    /// <summary>
    /// Bag de dados compartilhada entre todas as etapas.
    /// Cada etapa lê dados de etapas anteriores e escreve seus resultados.
    /// Não contém lógica — é um Data Object puro.
    /// </summary>
    public class PipelineContext
    {
        // ══════════════════════════════════════════════════════════
        //  IDENTIDADE DA SESSÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>ID da sessão do pipeline.</summary>
        public string SessionId { get; set; }

        /// <summary>CorrelationId para logs.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Quando a sessão iniciou.</summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        // ══════════════════════════════════════════════════════════
        //  DADOS DE ENTRADA (fornecidos antes do pipeline)
        // ══════════════════════════════════════════════════════════

        /// <summary>Dados brutos dos Rooms do Revit (entrada do E01).</summary>
        public List<RoomRawData> RawRooms { get; set; } = new();

        /// <summary>Configurações do projeto.</summary>
        public ProjectConfig ProjectConfig { get; set; }

        // ══════════════════════════════════════════════════════════
        //  DADOS PRODUZIDOS POR CADA ETAPA
        // ══════════════════════════════════════════════════════════

        // ── E01: DetectRooms ────────────────────────────────────
        public List<RoomInfo> DetectedRooms { get; set; } = new();

        // ── E02: ClassifyRooms ──────────────────────────────────
        public List<RoomInfo> ClassifiedRooms { get; set; } = new();
        public List<RoomInfo> WetAreas { get; set; } = new();

        // ── E03: IdentifyEquipment ──────────────────────────────
        public Dictionary<string, List<EquipmentInfo>> RequiredEquipmentByRoom { get; set; } = new();

        // ── E04: InsertEquipment ────────────────────────────────
        public List<EquipmentInfo> PositionedEquipment { get; set; } = new();
        public List<HydraulicPoint> HydraulicPoints { get; set; } = new();

        // ── E05: Validate ───────────────────────────────────────
        public bool ModelValidated { get; set; }

        // ── E06: BuildRisers ────────────────────────────────────
        public List<Riser> Risers { get; set; } = new();

        // ── E07: ColdWater ──────────────────────────────────────
        public PipeNetwork ColdWaterNetwork { get; set; }

        // ── E08: Sewer ──────────────────────────────────────────
        public PipeNetwork SewerNetwork { get; set; }

        // ── E08b: Ventilation ───────────────────────────────────
        public PipeNetwork VentilationNetwork { get; set; }

        // ── E09: Optimize ───────────────────────────────────────
        public bool NetworksOptimized { get; set; }

        // ── E10: ExportToRevit ──────────────────────────────────
        public List<string> CreatedRevitElementIds { get; set; } = new();

        // ── E11: Sizing ─────────────────────────────────────────
        public NetworkSizingResult ColdWaterSizing { get; set; }
        public NetworkSizingResult SewerSizing { get; set; }
        public NetworkSizingResult VentilationSizing { get; set; }
        public PressureTraversalResult PressureTraversal { get; set; }
        public CriticalPointResult CriticalPoint { get; set; }

        // ── E12: Export ─────────────────────────────────────────
        public ExportPackage ExportPackage { get; set; }

        // ══════════════════════════════════════════════════════════
        //  ESTADO DO PIPELINE
        // ══════════════════════════════════════════════════════════

        /// <summary>Estado de cada etapa.</summary>
        public Dictionary<string, StepState> StepStates { get; set; } = new();

        /// <summary>Histórico de execução.</summary>
        public List<StepExecutionRecord> ExecutionHistory { get; set; } = new();

        // ══════════════════════════════════════════════════════════
        //  STORE GENÉRICO (extensibilidade)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dados extras (para etapas futuras ou customizações).
        /// Chave = "NomeEtapa:NomeDado".
        /// </summary>
        private readonly Dictionary<string, object> _store = new();

        public void Set<T>(string key, T value) => _store[key] = value;

        public T Get<T>(string key) =>
            _store.TryGetValue(key, out var value) ? (T)value : default;

        public bool Has(string key) => _store.ContainsKey(key);

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS DE CONVENIÊNCIA
        // ══════════════════════════════════════════════════════════

        /// <summary>Retorna todas as redes como lista.</summary>
        public List<PipeNetwork> GetAllNetworks()
        {
            var networks = new List<PipeNetwork>();
            if (ColdWaterNetwork != null) networks.Add(ColdWaterNetwork);
            if (SewerNetwork != null) networks.Add(SewerNetwork);
            if (VentilationNetwork != null) networks.Add(VentilationNetwork);
            return networks;
        }

        /// <summary>Retorna todos os sizing results.</summary>
        public List<NetworkSizingResult> GetAllSizingResults()
        {
            var results = new List<NetworkSizingResult>();
            if (ColdWaterSizing != null) results.Add(ColdWaterSizing);
            if (SewerSizing != null) results.Add(SewerSizing);
            if (VentilationSizing != null) results.Add(VentilationSizing);
            return results;
        }
    }

    /// <summary>
    /// Estado persistido de uma etapa.
    /// </summary>
    public class StepState
    {
        public string StepId { get; set; }
        public StepStatus Status { get; set; } = StepStatus.Pending;
        public int AttemptCount { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; }
        public string UserComment { get; set; }
        public bool? UserApproved { get; set; }
    }

    /// <summary>
    /// Registro de execução (para histórico).
    /// </summary>
    public class StepExecutionRecord
    {
        public string StepId { get; set; }
        public int AttemptNumber { get; set; }
        public StepStatus ResultStatus { get; set; }
        public DateTime ExecutedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public string Summary { get; set; }
        public string ErrorMessage { get; set; }
    }
}
```

---

## 5. StepResult — Resultado de Execução

```csharp
namespace HidraulicoPlugin.Core.Pipeline
{
    /// <summary>
    /// Resultado da execução de uma etapa.
    /// </summary>
    public class StepResult
    {
        public string StepId { get; set; }
        public bool IsSuccessful { get; set; }
        public string Summary { get; set; }
        public TimeSpan Duration { get; set; }
        public int AttemptNumber { get; set; }

        /// <summary>Mensagem de erro (se falhou).</summary>
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

        /// <summary>Warnings (não impeditivos).</summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>Métricas da etapa.</summary>
        public Dictionary<string, object> Metrics { get; set; } = new();

        // ── Factories ───────────────────────────────────────────

        public static StepResult Success(string stepId, string summary,
            Dictionary<string, object> metrics = null)
        {
            return new StepResult
            {
                StepId = stepId,
                IsSuccessful = true,
                Summary = summary,
                Metrics = metrics ?? new()
            };
        }

        public static StepResult Failure(string stepId, string error,
            Exception ex = null)
        {
            return new StepResult
            {
                StepId = stepId,
                IsSuccessful = false,
                ErrorMessage = error,
                Exception = ex
            };
        }
    }

    public class StepCanExecuteResult
    {
        public bool CanExecute { get; set; }
        public string Reason { get; set; }

        public static StepCanExecuteResult Yes() =>
            new() { CanExecute = true };

        public static StepCanExecuteResult No(string reason) =>
            new() { CanExecute = false, Reason = reason };
    }

    public class StepValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();

        public static StepValidationResult Valid() =>
            new() { IsValid = true };

        public static StepValidationResult Invalid(params string[] issues) =>
            new() { IsValid = false, Issues = issues.ToList() };
    }
}
```

---

## 6. ProcessingStepBase — Classe Base

```csharp
namespace HidraulicoPlugin.Core.Pipeline
{
    /// <summary>
    /// Classe base para etapas. Fornece comportamento padrão.
    /// Etapas concretas herdam e implementam ExecuteCore().
    /// </summary>
    public abstract class ProcessingStepBase : IProcessingStep
    {
        // ── Identidade (implementar na classe concreta) ─────────

        public abstract string StepId { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract int Order { get; }

        // ── Configuração (defaults que podem ser sobrescritos) ───

        public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();
        public virtual bool IsMandatory => true;
        public virtual bool RequiresApproval => true;
        public virtual int MaxAttempts => 3;

        // ── CanExecute: verifica dependências ───────────────────

        public virtual StepCanExecuteResult CanExecute(PipelineContext context)
        {
            foreach (var dep in Dependencies)
            {
                if (!context.StepStates.TryGetValue(dep, out var state))
                    return StepCanExecuteResult.No(
                        $"Dependência '{dep}' não encontrada no contexto");

                if (state.Status != StepStatus.Completed)
                    return StepCanExecuteResult.No(
                        $"Dependência '{dep}' não concluída (status: {state.Status})");
            }

            return StepCanExecuteResult.Yes();
        }

        // ── Execute: wrapper com try/catch + métricas ───────────

        public StepResult Execute(PipelineContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = ExecuteCore(context);
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new StepResult
                {
                    StepId = StepId,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    Duration = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        /// Lógica real da etapa. Implementar na classe concreta.
        /// </summary>
        protected abstract StepResult ExecuteCore(PipelineContext context);

        // ── Validate: default = válido ──────────────────────────

        public virtual StepValidationResult Validate(PipelineContext context)
        {
            return StepValidationResult.Valid();
        }

        // ── Rollback: default = no-op ───────────────────────────

        public virtual void Rollback(PipelineContext context)
        {
            // Etapas concretas sobrescrevem para limpar seus dados
        }
    }
}
```

---

## 7. PipelineRunner — Motor de Execução

```csharp
namespace HidraulicoPlugin.Core.Pipeline
{
    /// <summary>
    /// Motor de execução do pipeline.
    /// Controla sequência, pausa, retry, rollback e logging.
    /// </summary>
    public class PipelineRunner
    {
        private readonly List<IProcessingStep> _steps;
        private readonly ILogService _log;
        private PipelineContext _context;
        private int _currentIndex;

        public PipelineRunner(List<IProcessingStep> steps, ILogService log)
        {
            _steps = steps.OrderBy(s => s.Order).ToList();
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  INICIALIZAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Inicializa o pipeline com um contexto.
        /// </summary>
        public PipelineRunnerStatus Initialize(PipelineContext context)
        {
            _context = context;
            _currentIndex = 0;

            // Inicializar estado de cada etapa
            foreach (var step in _steps)
            {
                if (!_context.StepStates.ContainsKey(step.StepId))
                {
                    _context.StepStates[step.StepId] = new StepState
                    {
                        StepId = step.StepId,
                        Status = StepStatus.Pending
                    };
                }
            }

            _context.SessionId = $"sess_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            _context.CorrelationId = _log.StartSession(_context.SessionId);

            _log.LogInfo("Pipeline inicializado",
                new { totalSteps = _steps.Count, sessionId = _context.SessionId });

            return GetStatus();
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa a próxima etapa pendente.
        /// Se requer aprovação, para em WaitingApproval.
        /// </summary>
        public StepResult ExecuteNext()
        {
            var step = GetNextPendingStep();
            if (step == null)
            {
                _log.LogInfo("Pipeline concluído: nenhuma etapa pendente");
                return null;
            }

            return ExecuteStep(step);
        }

        /// <summary>
        /// Executa uma etapa específica.
        /// </summary>
        public StepResult ExecuteStep(string stepId)
        {
            var step = _steps.FirstOrDefault(s => s.StepId == stepId);
            if (step == null)
                throw new InvalidOperationException($"Etapa '{stepId}' não encontrada");

            return ExecuteStep(step);
        }

        /// <summary>
        /// Executa todas as etapas até encontrar uma que requer aprovação.
        /// </summary>
        public List<StepResult> ExecuteUntilApproval()
        {
            var results = new List<StepResult>();

            while (true)
            {
                var step = GetNextPendingStep();
                if (step == null) break;

                var result = ExecuteStep(step);
                results.Add(result);

                if (!result.IsSuccessful) break;

                var state = _context.StepStates[step.StepId];
                if (state.Status == StepStatus.WaitingApproval) break;
            }

            return results;
        }

        private StepResult ExecuteStep(IProcessingStep step)
        {
            var state = _context.StepStates[step.StepId];

            // 1. Verificar se pode executar
            var canExecute = step.CanExecute(_context);
            if (!canExecute.CanExecute)
            {
                _log.LogWarning($"Etapa {step.StepId} não pode executar: {canExecute.Reason}");
                return StepResult.Failure(step.StepId, canExecute.Reason);
            }

            // 2. Atualizar estado
            state.Status = StepStatus.Running;
            state.StartedAt = DateTime.UtcNow;
            state.AttemptCount++;

            _log.LogStepStart(step.Name, step.Description,
                new { attempt = state.AttemptCount });

            // 3. Executar
            var result = step.Execute(_context);
            result.AttemptNumber = state.AttemptCount;

            // 4. Validar resultado
            if (result.IsSuccessful)
            {
                var validation = step.Validate(_context);
                if (!validation.IsValid)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = "Validação pós-execução falhou: " +
                        string.Join("; ", validation.Issues);
                    result.Warnings.AddRange(validation.Issues);
                }
            }

            // 5. Atualizar estado
            if (result.IsSuccessful)
            {
                state.Status = step.RequiresApproval
                    ? StepStatus.WaitingApproval
                    : StepStatus.Completed;
                state.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                state.Status = StepStatus.Failed;
                state.ErrorMessage = result.ErrorMessage;
            }

            // 6. Registrar histórico
            _context.ExecutionHistory.Add(new StepExecutionRecord
            {
                StepId = step.StepId,
                AttemptNumber = state.AttemptCount,
                ResultStatus = state.Status,
                ExecutedAt = DateTime.UtcNow,
                Duration = result.Duration,
                Summary = result.Summary,
                ErrorMessage = result.ErrorMessage
            });

            // 7. Logar
            _log.LogStepEnd(step.Name, result.IsSuccessful,
                new
                {
                    status = state.Status,
                    summary = result.Summary,
                    metrics = result.Metrics
                },
                result.Duration);

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  APROVAÇÃO HUMANA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aprova uma etapa em WaitingApproval.
        /// </summary>
        public void Approve(string stepId, string comment = null)
        {
            var state = _context.StepStates[stepId];
            if (state.Status != StepStatus.WaitingApproval)
                throw new InvalidOperationException(
                    $"Etapa '{stepId}' não está em WaitingApproval (atual: {state.Status})");

            state.Status = StepStatus.Completed;
            state.UserApproved = true;
            state.UserComment = comment;

            _log.LogInfo($"Etapa {stepId} aprovada", new { comment });
        }

        /// <summary>
        /// Rejeita uma etapa em WaitingApproval.
        /// </summary>
        public void Reject(string stepId, string reason)
        {
            var state = _context.StepStates[stepId];
            if (state.Status != StepStatus.WaitingApproval)
                throw new InvalidOperationException(
                    $"Etapa '{stepId}' não está em WaitingApproval");

            state.Status = StepStatus.Rejected;
            state.UserApproved = false;
            state.UserComment = reason;

            _log.LogWarning($"Etapa {stepId} rejeitada: {reason}");
        }

        // ══════════════════════════════════════════════════════════
        //  RETRY E ROLLBACK
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Reexecuta uma etapa que falhou ou foi rejeitada.
        /// </summary>
        public StepResult Retry(string stepId)
        {
            var step = _steps.FirstOrDefault(s => s.StepId == stepId);
            var state = _context.StepStates[stepId];

            if (state.Status is not (StepStatus.Failed or StepStatus.Rejected))
                throw new InvalidOperationException(
                    $"Etapa '{stepId}' não pode ser reexecutada (status: {state.Status})");

            if (state.AttemptCount >= step.MaxAttempts)
                throw new InvalidOperationException(
                    $"Etapa '{stepId}' excedeu máximo de tentativas ({step.MaxAttempts})");

            // Rollback antes de retry
            step.Rollback(_context);
            state.Status = StepStatus.Pending;

            _log.LogInfo($"Retry da etapa {stepId} (tentativa {state.AttemptCount + 1})");

            return ExecuteStep(step);
        }

        /// <summary>
        /// Desfaz uma etapa e todas as dependentes (cascata).
        /// </summary>
        public List<string> Rollback(string stepId)
        {
            var rolledBack = new List<string>();

            // Encontrar etapas dependentes (cascata)
            var toRollback = GetDependentSteps(stepId);
            toRollback.Add(stepId);

            // Rollback em ordem reversa
            foreach (var id in toRollback.OrderByDescending(id =>
                _steps.First(s => s.StepId == id).Order))
            {
                var step = _steps.First(s => s.StepId == id);
                var state = _context.StepStates[id];

                if (state.Status is StepStatus.Completed or StepStatus.Failed
                    or StepStatus.WaitingApproval)
                {
                    step.Rollback(_context);
                    state.Status = StepStatus.RolledBack;
                    rolledBack.Add(id);

                    _log.LogInfo($"Rollback: etapa {id}");
                }
            }

            return rolledBack;
        }

        // ══════════════════════════════════════════════════════════
        //  CONSULTAS
        // ══════════════════════════════════════════════════════════

        public PipelineRunnerStatus GetStatus()
        {
            var states = _context.StepStates;
            return new PipelineRunnerStatus
            {
                SessionId = _context.SessionId,
                TotalSteps = _steps.Count,
                CompletedSteps = states.Values.Count(s => s.Status == StepStatus.Completed),
                PendingSteps = states.Values.Count(s => s.Status == StepStatus.Pending),
                FailedSteps = states.Values.Count(s => s.Status == StepStatus.Failed),
                CurrentStepId = GetNextPendingStep()?.StepId,
                IsComplete = states.Values.All(s =>
                    s.Status is StepStatus.Completed or StepStatus.Skipped),
                Progress = states.Count > 0
                    ? (double)states.Values.Count(s => s.Status == StepStatus.Completed)
                      / states.Count
                    : 0
            };
        }

        public PipelineContext GetContext() => _context;

        // ══════════════════════════════════════════════════════════
        //  HELPERS INTERNOS
        // ══════════════════════════════════════════════════════════

        private IProcessingStep GetNextPendingStep()
        {
            return _steps
                .Where(s =>
                {
                    var state = _context.StepStates.GetValueOrDefault(s.StepId);
                    return state?.Status is StepStatus.Pending or StepStatus.RolledBack;
                })
                .OrderBy(s => s.Order)
                .FirstOrDefault();
        }

        private List<string> GetDependentSteps(string stepId)
        {
            return _steps
                .Where(s => s.Dependencies.Contains(stepId))
                .Select(s => s.StepId)
                .ToList();
        }
    }

    public class PipelineRunnerStatus
    {
        public string SessionId { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int PendingSteps { get; set; }
        public int FailedSteps { get; set; }
        public string CurrentStepId { get; set; }
        public bool IsComplete { get; set; }
        public double Progress { get; set; }

        public override string ToString() =>
            $"Pipeline: {CompletedSteps}/{TotalSteps} " +
            $"({Progress:P0}) " +
            $"Atual: {CurrentStepId ?? "nenhuma"} " +
            $"{(IsComplete ? "✅ Concluído" : "🔄 Em andamento")}";
    }
}
```

---

## 8. Etapas Concretas

### 8.1 E01 — DetectRoomsStep

```csharp
namespace HidraulicoPlugin.Core.Pipeline.Steps
{
    public class DetectRoomsStep : ProcessingStepBase
    {
        private readonly IRoomService _roomService;

        public DetectRoomsStep(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public override string StepId => "E01";
        public override string Name => "DetectRooms";
        public override string Description => "Detecção de ambientes";
        public override int Order => 1;
        public override IReadOnlyList<string> Dependencies => Array.Empty<string>();

        protected override StepResult ExecuteCore(PipelineContext context)
        {
            if (context.RawRooms == null || context.RawRooms.Count == 0)
                return StepResult.Failure(StepId, "Nenhum dado bruto de ambientes fornecido");

            var result = _roomService.DetectRooms(context.RawRooms);

            if (!result.IsSuccessful)
                return StepResult.Failure(StepId,
                    $"Detecção falhou: {result.TotalErrors} erros");

            // Escrever no contexto
            context.DetectedRooms = result.Rooms;

            return StepResult.Success(StepId,
                $"{result.TotalConverted} ambientes detectados",
                new Dictionary<string, object>
                {
                    ["totalDetected"] = result.TotalConverted,
                    ["totalErrors"] = result.TotalErrors
                });
        }

        public override StepValidationResult Validate(PipelineContext context)
        {
            if (context.DetectedRooms == null || context.DetectedRooms.Count == 0)
                return StepValidationResult.Invalid("Nenhum ambiente detectado");

            return StepValidationResult.Valid();
        }

        public override void Rollback(PipelineContext context)
        {
            context.DetectedRooms.Clear();
        }
    }
}
```

### 8.2 E02 — ClassifyRoomsStep

```csharp
namespace HidraulicoPlugin.Core.Pipeline.Steps
{
    public class ClassifyRoomsStep : ProcessingStepBase
    {
        private readonly IRoomService _roomService;

        public ClassifyRoomsStep(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public override string StepId => "E02";
        public override string Name => "ClassifyRooms";
        public override string Description => "Classificação de ambientes";
        public override int Order => 2;
        public override IReadOnlyList<string> Dependencies =>
            new[] { "E01" };

        protected override StepResult ExecuteCore(PipelineContext context)
        {
            var classifyResult = _roomService.ClassifyAll(context.DetectedRooms);

            context.ClassifiedRooms = context.DetectedRooms; // já classificados in-place
            context.WetAreas = _roomService.GetWetAreas(context.ClassifiedRooms);

            return StepResult.Success(StepId,
                $"{context.WetAreas.Count} áreas molhadas de {context.ClassifiedRooms.Count} ambientes",
                new Dictionary<string, object>
                {
                    ["totalClassified"] = context.ClassifiedRooms.Count,
                    ["wetAreas"] = context.WetAreas.Count
                });
        }

        public override void Rollback(PipelineContext context)
        {
            context.ClassifiedRooms.Clear();
            context.WetAreas.Clear();
        }
    }
}
```

### 8.3 E04 — InsertEquipmentStep

```csharp
namespace HidraulicoPlugin.Core.Pipeline.Steps
{
    public class InsertEquipmentStep : ProcessingStepBase
    {
        private readonly IEquipmentService _equipService;

        public InsertEquipmentStep(IEquipmentService equipService)
        {
            _equipService = equipService;
        }

        public override string StepId => "E04";
        public override string Name => "InsertEquipment";
        public override string Description => "Inserção e posicionamento de equipamentos";
        public override int Order => 4;
        public override IReadOnlyList<string> Dependencies =>
            new[] { "E03" };

        protected override StepResult ExecuteCore(PipelineContext context)
        {
            var allEquipment = new List<EquipmentInfo>();
            var allPoints = new List<HydraulicPoint>();

            foreach (var room in context.WetAreas)
            {
                var processResult = _equipService.ProcessRoom(room);

                if (!processResult.IsSuccessful)
                    return StepResult.Failure(StepId,
                        $"Falha no ambiente '{room.Name}': " +
                        $"{processResult.Validation?.Issues?.Count ?? 0} problemas");

                allEquipment.AddRange(processResult.Equipment);
            }

            context.PositionedEquipment = allEquipment;

            return StepResult.Success(StepId,
                $"{allEquipment.Count} equipamentos posicionados",
                new Dictionary<string, object>
                {
                    ["totalEquipment"] = allEquipment.Count,
                    ["rooms"] = context.WetAreas.Count
                });
        }

        public override void Rollback(PipelineContext context)
        {
            context.PositionedEquipment.Clear();
            context.HydraulicPoints.Clear();
        }
    }
}
```

### 8.4 E07 — BuildColdWaterStep

```csharp
namespace HidraulicoPlugin.Core.Pipeline.Steps
{
    public class BuildColdWaterStep : ProcessingStepBase
    {
        private readonly INetworkService _networkService;

        public BuildColdWaterStep(INetworkService networkService)
        {
            _networkService = networkService;
        }

        public override string StepId => "E07";
        public override string Name => "BuildColdWater";
        public override string Description => "Geração da rede de água fria";
        public override int Order => 7;
        public override IReadOnlyList<string> Dependencies =>
            new[] { "E06" };

        protected override StepResult ExecuteCore(PipelineContext context)
        {
            var afPoints = context.HydraulicPoints
                .Where(p => p.System == HydraulicSystem.ColdWater)
                .ToList();

            var input = new ColdWaterNetworkInput
            {
                Points = afPoints,
                Equipment = context.PositionedEquipment,
                Rooms = context.WetAreas,
                Risers = context.Risers
                    .Where(r => r.System == HydraulicSystem.ColdWater)
                    .ToList(),
                ReservoirElevationM = context.ProjectConfig?.ReservoirElevationM ?? 9.0,
                WaterLevelM = context.ProjectConfig?.WaterLevelM ?? 1.0,
                BarrelPosition = context.ProjectConfig?.BarrelPosition
                    ?? new Point3D(5, 4, 9.5)
            };

            var result = _networkService.BuildColdWaterNetwork(input);

            if (!result.IsSuccessful)
                return StepResult.Failure(StepId,
                    $"Falha na rede AF: {result.Errors.Count} erros");

            context.ColdWaterNetwork = result.Network;

            return StepResult.Success(StepId,
                result.GetSummary(),
                new Dictionary<string, object>
                {
                    ["segments"] = result.Network.SegmentCount,
                    ["totalLengthM"] = result.Statistics.TotalPipeLengthM
                });
        }

        public override void Rollback(PipelineContext context)
        {
            context.ColdWaterNetwork = null;
        }
    }
}
```

### 8.5 E11 — SizeNetworksStep

```csharp
namespace HidraulicoPlugin.Core.Pipeline.Steps
{
    public class SizeNetworksStep : ProcessingStepBase
    {
        private readonly ISizingService _sizingService;

        public SizeNetworksStep(ISizingService sizingService)
        {
            _sizingService = sizingService;
        }

        public override string StepId => "E11";
        public override string Name => "SizeNetworks";
        public override string Description => "Dimensionamento hidráulico";
        public override int Order => 12;
        public override IReadOnlyList<string> Dependencies =>
            new[] { "E07", "E08", "E08b" };

        protected override StepResult ExecuteCore(PipelineContext context)
        {
            var sourcePressure = context.ProjectConfig?.ReservoirElevationM
                + (context.ProjectConfig?.WaterLevelM ?? 1.0)
                ?? 10.0;

            // AF
            if (context.ColdWaterNetwork != null)
            {
                var afResult = _sizingService.ProcessNetwork(
                    context.ColdWaterNetwork,
                    new SizingOptions { SourcePressureMca = sourcePressure });

                context.ColdWaterSizing = afResult.Sizing;
                context.PressureTraversal = afResult.PressureTraversal;
                context.CriticalPoint = afResult.CriticalPoint;
            }

            // ES
            if (context.SewerNetwork != null)
            {
                var esResult = _sizingService.ProcessNetwork(context.SewerNetwork);
                context.SewerSizing = esResult.Sizing;
            }

            // VE
            if (context.VentilationNetwork != null)
            {
                var veResult = _sizingService.ProcessNetwork(context.VentilationNetwork);
                context.VentilationSizing = veResult.Sizing;
            }

            var allSizing = context.GetAllSizingResults();
            var allPassed = allSizing.All(s => s.IsCompliant);

            return allPassed
                ? StepResult.Success(StepId,
                    $"Dimensionamento OK: {allSizing.Sum(s => s.TotalSegmentsSized)} trechos",
                    new Dictionary<string, object>
                    {
                        ["totalSegments"] = allSizing.Sum(s => s.TotalSegmentsSized),
                        ["criticalPoint"] = context.CriticalPoint?.PointId,
                        ["criticalPressure"] = context.CriticalPoint?.DynamicPressureMca
                    })
                : StepResult.Failure(StepId,
                    $"Dimensionamento com erros: " +
                    $"{allSizing.Sum(s => s.TotalSegmentsWithErrors)} trechos reprovados");
        }

        public override void Rollback(PipelineContext context)
        {
            context.ColdWaterSizing = null;
            context.SewerSizing = null;
            context.VentilationSizing = null;
            context.PressureTraversal = null;
            context.CriticalPoint = null;
        }
    }
}
```

---

## 9. Registro de Etapas (Factory)

```csharp
namespace HidraulicoPlugin.Core.Pipeline
{
    /// <summary>
    /// Fábrica que cria e registra todas as etapas do pipeline.
    /// Ponto de extensão: para adicionar uma etapa,
    /// crie a classe e registre aqui.
    /// </summary>
    public static class PipelineFactory
    {
        public static PipelineRunner Create(
            IRoomService roomService,
            IEquipmentService equipService,
            INetworkService networkService,
            ISizingService sizingService,
            IExportService exportService,
            ILogService logService)
        {
            var steps = new List<IProcessingStep>
            {
                new DetectRoomsStep(roomService),          // E01
                new ClassifyRoomsStep(roomService),        // E02
                new IdentifyEquipmentStep(equipService),   // E03
                new InsertEquipmentStep(equipService),     // E04
                new ValidateModelStep(roomService, equipService), // E05
                new BuildRisersStep(networkService),       // E06
                new BuildColdWaterStep(networkService),    // E07
                new BuildSewerStep(networkService),        // E08
                new BuildVentilationStep(networkService),  // E08b
                new OptimizeNetworksStep(networkService),  // E09
                // E10: ExportToRevit (via IDynamoIntegration, não neste factory)
                new SizeNetworksStep(sizingService),       // E11
                new GenerateExportStep(exportService),     // E12
            };

            return new PipelineRunner(steps, logService);
        }
    }
}
```

---

## 10. Exemplo de Execução Completo

```csharp
public class HydraulicPluginCommand
{
    private readonly PipelineRunner _runner;
    private readonly ILogService _log;

    public void Execute(List<RoomRawData> rawData)
    {
        // 1. Criar contexto
        var context = new PipelineContext
        {
            RawRooms = rawData,
            ProjectConfig = new ProjectConfig
            {
                ReservoirElevationM = 9.0,
                WaterLevelM = 1.0,
                BarrelPosition = new Point3D(5, 4, 9.5)
            }
        };

        // 2. Inicializar
        var status = _runner.Initialize(context);
        Console.WriteLine(status);
        // → "Pipeline: 0/12 (0%) Atual: E01 🔄 Em andamento"

        // 3. Executar até precisar de aprovação
        var results = _runner.ExecuteUntilApproval();
        // → Executa E01, para em WaitingApproval

        var lastResult = results.Last();
        Console.WriteLine(lastResult.Summary);
        // → "12 ambientes detectados"

        // 4. Aprovar
        _runner.Approve("E01", "Ambientes OK, pode prosseguir");

        // 5. Continuar
        results = _runner.ExecuteUntilApproval();
        // → Executa E02, para em WaitingApproval

        Console.WriteLine(results.Last().Summary);
        // → "5 áreas molhadas de 12 ambientes"

        // 6. Rejeitar e corrigir
        _runner.Reject("E02", "Lavabo não foi classificado como molhado");

        // Ajustar contexto...
        _runner.Retry("E02");
        // → Rollback E02, reexecuta com contexto atualizado

        // 7. Loop de execução
        while (!_runner.GetStatus().IsComplete)
        {
            results = _runner.ExecuteUntilApproval();
            if (results.Count == 0) break;

            var last = results.Last();
            if (!last.IsSuccessful)
            {
                Console.WriteLine($"❌ {last.StepId}: {last.ErrorMessage}");
                break;
            }

            var currentStep = _runner.GetStatus().CurrentStepId;
            if (currentStep != null)
            {
                // Mostrar resultado na UI e esperar aprovação
                Console.Write($"Aprovar {currentStep}? (s/n): ");
                var input = Console.ReadLine();

                if (input == "s")
                    _runner.Approve(currentStep);
                else
                    _runner.Reject(currentStep, "Rejeitado pelo usuário");
            }
        }

        // 8. Status final
        var finalStatus = _runner.GetStatus();
        Console.WriteLine(finalStatus);
        // → "Pipeline: 12/12 (100%) Atual: nenhuma ✅ Concluído"
    }
}
```

---

## 11. Extensibilidade

### Para adicionar uma nova etapa:

```csharp
// 1. Criar a classe
public class NewCustomStep : ProcessingStepBase
{
    public override string StepId => "EX1";
    public override string Name => "CustomStep";
    public override string Description => "Minha etapa customizada";
    public override int Order => 15; // após E12
    public override IReadOnlyList<string> Dependencies => new[] { "E11" };
    public override bool RequiresApproval => false; // automática

    protected override StepResult ExecuteCore(PipelineContext context)
    {
        // Ler dados do contexto
        var networks = context.GetAllNetworks();

        // Processar...
        var myResult = DoCustomProcessing(networks);

        // Salvar no store genérico
        context.Set("EX1:CustomResult", myResult);

        return StepResult.Success(StepId, "Processamento custom concluído");
    }

    public override void Rollback(PipelineContext context)
    {
        context.Set<object>("EX1:CustomResult", null);
    }
}

// 2. Registrar no factory (1 linha)
steps.Add(new NewCustomStep());
```

---

## 12. Resumo Visual

```
Padrão Pipeline / Chain of Responsibility
│
├── Interfaces
│   └── IProcessingStep
│       ├── StepId, Name, Description, Order
│       ├── Dependencies, IsMandatory, RequiresApproval
│       ├── CanExecute(ctx) → StepCanExecuteResult
│       ├── Execute(ctx) → StepResult
│       ├── Validate(ctx) → StepValidationResult
│       └── Rollback(ctx)
│
├── Classe Base
│   └── ProcessingStepBase
│       ├── CanExecute → verifica dependências
│       ├── Execute → try/catch + Stopwatch + chama ExecuteCore()
│       ├── Validate → default: Valid()
│       └── Rollback → default: no-op
│
├── Contexto
│   └── PipelineContext
│       ├── Dados tipados (DetectedRooms, ColdWaterNetwork, etc.)
│       ├── StepStates (estado de cada etapa)
│       ├── ExecutionHistory (log de execução)
│       └── Store genérico (Set/Get/Has)
│
├── Runner
│   └── PipelineRunner
│       ├── Initialize(ctx) → status
│       ├── ExecuteNext() → StepResult
│       ├── ExecuteStep(id) → StepResult
│       ├── ExecuteUntilApproval() → List<StepResult>
│       ├── Approve(id, comment)
│       ├── Reject(id, reason)
│       ├── Retry(id) → StepResult
│       ├── Rollback(id) → List<string> (cascata)
│       └── GetStatus() → PipelineRunnerStatus
│
├── Etapas Implementadas
│   ├── E01: DetectRoomsStep (IRoomService)
│   ├── E02: ClassifyRoomsStep (IRoomService)
│   ├── E04: InsertEquipmentStep (IEquipmentService)
│   ├── E07: BuildColdWaterStep (INetworkService)
│   └── E11: SizeNetworksStep (ISizingService)
│
├── Factory
│   └── PipelineFactory.Create(...) → PipelineRunner com 12 etapas
│
└── Extensibilidade
    └── 1. Criar classe herda ProcessingStepBase
        2. Implementar ExecuteCore()
        3. Registrar no PipelineFactory
```
