# Padrão de Confiabilidade — Classificação de Erros

> Especificação completa do sistema de classificação de erros por severidade (Critical, Warning, Info), com regras de bloqueio, integração com pipeline, logging e eventos, para uso no PluginCore.

---

## 1. Definição do Padrão

### 1.1 O que é

O padrão de classificação de erros define uma **taxonomia consistente** para todos os problemas que podem ocorrer durante a execução do pipeline. Cada erro recebe uma **severidade** que determina automaticamente o que acontece com a execução: parar, pausar para aprovação, ou continuar com aviso.

### 1.2 Por que é necessário

Sem classificação, o sistema trata tudo como fatal:

```
❌ ANTES:
  try { executarEtapa(); }
  catch (Exception ex) { pipeline.Stop(); }
  // → Um warning de área mínima para o pipeline inteiro

✅ DEPOIS:
  Erro: "Banheiro com 1.8m² (mínimo 2.0m²)"
  Severidade: Warning → pipeline pausa, usuário decide
  Ação: Approve (aceitar) ou Reject (corrigir)
```

### 1.3 Benefícios

| Benefício | Detalhe |
|-----------|---------|
| **Previsibilidade** | Cada tipo de erro tem ação definida — sem surpresas |
| **Continuidade** | Erros leves não travam o pipeline |
| **Controle** | Erros médios pedem decisão do usuário |
| **Segurança** | Erros críticos garantem que nada incorreto é gerado |
| **Debug** | Contexto rico em cada erro facilita diagnóstico |
| **Auditoria** | Histórico completo de erros por sessão |

---

## 2. Classificação — Enum

```csharp
namespace HidraulicoPlugin.Core.ErrorHandling
{
    /// <summary>
    /// Severidade do erro. Determina o comportamento do pipeline.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// CRÍTICO — Bloqueia execução imediatamente.
        /// O pipeline PARA. Requer correção obrigatória.
        /// Exemplos: dados de entrada ausentes, violação estrutural,
        /// cálculo impossível, divisão por zero.
        /// </summary>
        Critical = 1,

        /// <summary>
        /// AVISO — Permite continuidade com aprovação.
        /// O pipeline PAUSA em WaitingApproval.
        /// O usuário decide: aprovar (continua) ou rejeitar (para).
        /// Exemplos: área abaixo do mínimo, velocidade acima do ideal,
        /// DN não comercial necessitando arredondamento.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// INFORMATIVO — Apenas registra, não bloqueia.
        /// O pipeline CONTINUA normalmente.
        /// Exemplos: equipamento opcional não incluído,
        /// otimização sem efeito, arredondamento automático.
        /// </summary>
        Info = 3
    }
}
```

---

## 3. Semântica dos Níveis

### 3.1 Diagrama de decisão

```
Erro detectado
    │
    ├── Severidade?
    │
    ├── CRITICAL ──→ 🛑 STOP
    │                   │
    │                   ├── Pipeline.Status = Failed
    │                   ├── Step.Status = Failed
    │                   ├── Evento: StepFailedEvent
    │                   ├── Log: LogCritical()
    │                   └── Ação: OBRIGATÓRIO corrigir + Retry
    │
    ├── WARNING ───→ ⏸ PAUSE
    │                   │
    │                   ├── Pipeline.Status = Paused
    │                   ├── Step.Status = WaitingApproval
    │                   ├── Evento: ValidationWarning
    │                   ├── Log: LogWarning()
    │                   └── Ação: Usuário decide
    │                       ├── Approve → Completed (com ressalva)
    │                       └── Reject → Failed (corrigir + Retry)
    │
    └── INFO ──────→ ✅ CONTINUE
                        │
                        ├── Pipeline.Status = Running
                        ├── Step continua executando
                        ├── Evento: LogCreated
                        ├── Log: LogInfo()
                        └── Ação: Nenhuma (registrado para auditoria)
```

### 3.2 Tabela de regras

| Severidade | Pipeline | Step | Bloqueia? | Ação Requerida | Pode Ignorar? |
|-----------|----------|------|-----------|---------------|--------------|
| **Critical** | Failed | Failed | ✅ SIM | Corrigir + Retry | ❌ NÃO |
| **Warning** | Paused | WaitingApproval | ⚠️ PAUSA | Approve ou Reject | ✅ SIM (Approve) |
| **Info** | Running | Continua | ❌ NÃO | Nenhuma | N/A |

---

## 4. Estrutura do Erro — SystemError

```csharp
namespace HidraulicoPlugin.Core.ErrorHandling
{
    /// <summary>
    /// Erro estruturado do sistema. Imutável após criação.
    /// Contém contexto suficiente para diagnóstico completo.
    /// </summary>
    public class SystemError
    {
        // ══════════════════════════════════════════════════════════
        //  IDENTIDADE
        // ══════════════════════════════════════════════════════════

        /// <summary>ID único do erro.</summary>
        public string ErrorId { get; }

        /// <summary>Código classificador (ex: "CALC_001", "VAL_012").</summary>
        public string Code { get; set; }

        /// <summary>Severidade.</summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>Categoria funcional.</summary>
        public ErrorCategory Category { get; set; }

        // ══════════════════════════════════════════════════════════
        //  MENSAGEM
        // ══════════════════════════════════════════════════════════

        /// <summary>Mensagem principal (curta, para UI).</summary>
        public string Message { get; set; }

        /// <summary>Descrição detalhada (para log/debug).</summary>
        public string Detail { get; set; }

        /// <summary>Sugestão de correção.</summary>
        public string Suggestion { get; set; }

        // ══════════════════════════════════════════════════════════
        //  CONTEXTO
        // ══════════════════════════════════════════════════════════

        /// <summary>Etapa do pipeline onde ocorreu.</summary>
        public string StepId { get; set; }

        /// <summary>Componente afetado (ex: "seg_af_003", "room_02").</summary>
        public string ComponentId { get; set; }

        /// <summary>Tipo do componente (ex: "PipeSegment", "Room").</summary>
        public string ComponentType { get; set; }

        /// <summary>Dados adicionais de contexto.</summary>
        public Dictionary<string, object> Context { get; set; } = new();

        // ══════════════════════════════════════════════════════════
        //  CONTROLE
        // ══════════════════════════════════════════════════════════

        /// <summary>Se o erro pode ser ignorado pelo usuário (Approve).</summary>
        public bool CanBeIgnored { get; set; }

        /// <summary>Se o erro é passível de retry automático.</summary>
        public bool IsRetryable { get; set; }

        /// <summary>Se o erro já foi resolvido/ignorado.</summary>
        public ErrorResolution Resolution { get; set; } = ErrorResolution.Unresolved;

        /// <summary>Comentário do usuário ao resolver.</summary>
        public string ResolutionComment { get; set; }

        // ══════════════════════════════════════════════════════════
        //  METADADOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Timestamp de criação.</summary>
        public DateTime Timestamp { get; }

        /// <summary>Exceção original (se houver).</summary>
        public Exception OriginalException { get; set; }

        /// <summary>CorrelationId da sessão.</summary>
        public string CorrelationId { get; set; }

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTOR
        // ══════════════════════════════════════════════════════════

        public SystemError(ErrorSeverity severity, string code, string message)
        {
            ErrorId = $"err_{Guid.NewGuid().ToString("N")[..8]}";
            Timestamp = DateTime.UtcNow;
            Severity = severity;
            Code = code;
            Message = message;

            // Defaults baseados na severidade
            CanBeIgnored = severity != ErrorSeverity.Critical;
            IsRetryable = severity == ErrorSeverity.Critical;
        }

        // ══════════════════════════════════════════════════════════
        //  DISPLAY
        // ══════════════════════════════════════════════════════════

        public string GetIcon() => Severity switch
        {
            ErrorSeverity.Critical => "🛑",
            ErrorSeverity.Warning => "⚠️",
            ErrorSeverity.Info => "ℹ️",
            _ => "❓"
        };

        public override string ToString() =>
            $"{GetIcon()} [{Code}] {Message}";

        public string ToDetailedString() =>
            $"{GetIcon()} [{Code}] ({Severity}) {Message}\n" +
            $"   Etapa: {StepId} | Componente: {ComponentId}\n" +
            $"   Detalhe: {Detail}\n" +
            $"   Sugestão: {Suggestion}";
    }
}
```

---

## 5. Enums de Suporte

```csharp
namespace HidraulicoPlugin.Core.ErrorHandling
{
    /// <summary>
    /// Categoria funcional do erro.
    /// </summary>
    public enum ErrorCategory
    {
        // ── Dados ───────────────────────────────────────────────
        /// <summary>Dados de entrada ausentes ou incompletos.</summary>
        MissingData = 100,

        /// <summary>Dados inválidos (formato, tipo, range).</summary>
        InvalidData = 101,

        /// <summary>Dados inconsistentes entre etapas.</summary>
        InconsistentData = 102,

        // ── Validação ───────────────────────────────────────────
        /// <summary>Violação de regra normativa (NBR).</summary>
        NormViolation = 200,

        /// <summary>Violação de restrição espacial.</summary>
        SpatialViolation = 201,

        /// <summary>Equipamento faltante ou incorreto.</summary>
        EquipmentViolation = 202,

        // ── Cálculo ─────────────────────────────────────────────
        /// <summary>Falha em cálculo hidráulico.</summary>
        CalculationError = 300,

        /// <summary>Resultado fora do range aceitável.</summary>
        OutOfRange = 301,

        /// <summary>DN não encontrado em tabela comercial.</summary>
        NoCommercialDN = 302,

        // ── Rede ────────────────────────────────────────────────
        /// <summary>Rede desconectada (pontos soltos).</summary>
        NetworkDisconnected = 400,

        /// <summary>Ciclo detectado na rede.</summary>
        NetworkCycle = 401,

        /// <summary>Roteamento impossível.</summary>
        RoutingFailed = 402,

        // ── Integração ──────────────────────────────────────────
        /// <summary>Falha de execução do Dynamo.</summary>
        DynamoError = 500,

        /// <summary>Dynamo não acessível.</summary>
        DynamoUnavailable = 501,

        /// <summary>JSON inválido na comunicação.</summary>
        JsonError = 502,

        // ── Sistema ─────────────────────────────────────────────
        /// <summary>Erro inesperado (exception não tratada).</summary>
        SystemError = 900,

        /// <summary>Timeout de operação.</summary>
        Timeout = 901
    }

    /// <summary>
    /// Estado de resolução do erro.
    /// </summary>
    public enum ErrorResolution
    {
        /// <summary>Não resolvido.</summary>
        Unresolved = 0,

        /// <summary>Corrigido (dados alterados + retry).</summary>
        Fixed = 1,

        /// <summary>Ignorado pelo usuário (approved).</summary>
        Ignored = 2,

        /// <summary>Retry automático resolveu.</summary>
        AutoResolved = 3,

        /// <summary>Rejeitado (usuário pediu correção).</summary>
        Rejected = 4
    }
}
```

---

## 6. ErrorCollector — Acumula Erros por Etapa

```csharp
namespace HidraulicoPlugin.Core.ErrorHandling
{
    /// <summary>
    /// Acumulador de erros para uma etapa do pipeline.
    /// Cada step cria um ErrorCollector, acumula erros durante execução,
    /// e no final avalia o resultado.
    /// </summary>
    public class ErrorCollector
    {
        private readonly List<SystemError> _errors = new();
        private readonly string _stepId;

        public ErrorCollector(string stepId)
        {
            _stepId = stepId;
        }

        // ══════════════════════════════════════════════════════════
        //  ACUMULAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>Adiciona um erro ao coletor.</summary>
        public void Add(SystemError error)
        {
            error.StepId ??= _stepId;
            _errors.Add(error);
        }

        /// <summary>Adiciona um erro crítico.</summary>
        public void AddCritical(string code, string message,
            string detail = null, string suggestion = null)
        {
            Add(new SystemError(ErrorSeverity.Critical, code, message)
            {
                Detail = detail,
                Suggestion = suggestion
            });
        }

        /// <summary>Adiciona um aviso.</summary>
        public void AddWarning(string code, string message,
            string detail = null, string suggestion = null)
        {
            Add(new SystemError(ErrorSeverity.Warning, code, message)
            {
                Detail = detail,
                Suggestion = suggestion
            });
        }

        /// <summary>Adiciona um informativo.</summary>
        public void AddInfo(string code, string message,
            string detail = null)
        {
            Add(new SystemError(ErrorSeverity.Info, code, message)
            {
                Detail = detail
            });
        }

        /// <summary>Adiciona erro com contexto de componente.</summary>
        public void AddForComponent(ErrorSeverity severity,
            string code, string message,
            string componentId, string componentType,
            Dictionary<string, object> context = null)
        {
            Add(new SystemError(severity, code, message)
            {
                ComponentId = componentId,
                ComponentType = componentType,
                Context = context ?? new()
            });
        }

        // ══════════════════════════════════════════════════════════
        //  CONSULTAS
        // ══════════════════════════════════════════════════════════

        /// <summary>Todos os erros acumulados.</summary>
        public IReadOnlyList<SystemError> All => _errors.AsReadOnly();

        /// <summary>Quantidade total.</summary>
        public int Count => _errors.Count;

        /// <summary>Tem erros?</summary>
        public bool HasErrors => _errors.Count > 0;

        /// <summary>Tem erros críticos?</summary>
        public bool HasCritical => _errors.Any(e =>
            e.Severity == ErrorSeverity.Critical);

        /// <summary>Tem warnings?</summary>
        public bool HasWarnings => _errors.Any(e =>
            e.Severity == ErrorSeverity.Warning);

        /// <summary>Contagem por severidade.</summary>
        public int CriticalCount => _errors.Count(e =>
            e.Severity == ErrorSeverity.Critical);
        public int WarningCount => _errors.Count(e =>
            e.Severity == ErrorSeverity.Warning);
        public int InfoCount => _errors.Count(e =>
            e.Severity == ErrorSeverity.Info);

        /// <summary>Filtra por severidade.</summary>
        public List<SystemError> GetBySeverity(ErrorSeverity severity) =>
            _errors.Where(e => e.Severity == severity).ToList();

        /// <summary>Filtra por categoria.</summary>
        public List<SystemError> GetByCategory(ErrorCategory category) =>
            _errors.Where(e => e.Category == category).ToList();

        /// <summary>Filtra por componente.</summary>
        public List<SystemError> GetByComponent(string componentId) =>
            _errors.Where(e => e.ComponentId == componentId).ToList();

        // ══════════════════════════════════════════════════════════
        //  AVALIAÇÃO — Qual ação tomar?
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Avalia o resultado baseado nos erros acumulados.
        /// Retorna a ação que o pipeline deve tomar.
        /// </summary>
        public ErrorEvaluation Evaluate()
        {
            if (HasCritical)
            {
                return new ErrorEvaluation
                {
                    Action = PipelineAction.Stop,
                    HighestSeverity = ErrorSeverity.Critical,
                    Summary = $"🛑 {CriticalCount} erro(s) crítico(s) — pipeline interrompido",
                    Errors = _errors,
                    RequiresUserAction = true,
                    CanRetry = _errors.Any(e =>
                        e.Severity == ErrorSeverity.Critical && e.IsRetryable)
                };
            }

            if (HasWarnings)
            {
                return new ErrorEvaluation
                {
                    Action = PipelineAction.PauseForApproval,
                    HighestSeverity = ErrorSeverity.Warning,
                    Summary = $"⚠️ {WarningCount} aviso(s) — aprovação necessária",
                    Errors = _errors,
                    RequiresUserAction = true,
                    CanRetry = false
                };
            }

            if (HasErrors) // Só Info
            {
                return new ErrorEvaluation
                {
                    Action = PipelineAction.Continue,
                    HighestSeverity = ErrorSeverity.Info,
                    Summary = $"ℹ️ {InfoCount} nota(s) informativa(s)",
                    Errors = _errors,
                    RequiresUserAction = false,
                    CanRetry = false
                };
            }

            return new ErrorEvaluation
            {
                Action = PipelineAction.Continue,
                HighestSeverity = null,
                Summary = "✅ Sem erros",
                Errors = new(),
                RequiresUserAction = false,
                CanRetry = false
            };
        }

        /// <summary>Limpa todos os erros.</summary>
        public void Clear() => _errors.Clear();

        /// <summary>Resumo para exibição.</summary>
        public string GetSummary() =>
            $"Erros: {CriticalCount}🛑 {WarningCount}⚠️ {InfoCount}ℹ️";
    }
}
```

---

## 7. ErrorEvaluation e PipelineAction

```csharp
namespace HidraulicoPlugin.Core.ErrorHandling
{
    /// <summary>
    /// Ação que o pipeline deve tomar.
    /// </summary>
    public enum PipelineAction
    {
        /// <summary>Continuar execução normalmente.</summary>
        Continue = 0,

        /// <summary>Pausar e aguardar aprovação do usuário.</summary>
        PauseForApproval = 1,

        /// <summary>Parar execução imediatamente.</summary>
        Stop = 2
    }

    /// <summary>
    /// Resultado da avaliação dos erros acumulados.
    /// Diz ao pipeline O QUE FAZER.
    /// </summary>
    public class ErrorEvaluation
    {
        /// <summary>Ação que o pipeline deve executar.</summary>
        public PipelineAction Action { get; set; }

        /// <summary>Maior severidade encontrada.</summary>
        public ErrorSeverity? HighestSeverity { get; set; }

        /// <summary>Resumo para exibição.</summary>
        public string Summary { get; set; }

        /// <summary>Lista de erros.</summary>
        public List<SystemError> Errors { get; set; } = new();

        /// <summary>Se o usuário precisa agir.</summary>
        public bool RequiresUserAction { get; set; }

        /// <summary>Se pode tentar novamente.</summary>
        public bool CanRetry { get; set; }

        /// <summary>Erros críticos (para display).</summary>
        public List<SystemError> CriticalErrors =>
            Errors.Where(e => e.Severity == ErrorSeverity.Critical).ToList();

        /// <summary>Warnings (para display).</summary>
        public List<SystemError> Warnings =>
            Errors.Where(e => e.Severity == ErrorSeverity.Warning).ToList();
    }
}
```

---

## 8. Fábrica de Erros — ErrorFactory

```csharp
namespace HidraulicoPlugin.Core.ErrorHandling
{
    /// <summary>
    /// Fábrica de erros comuns do sistema.
    /// Garante consistência de código, mensagem e severidade.
    /// </summary>
    public static class ErrorFactory
    {
        // ══════════════════════════════════════════════════════════
        //  DADOS
        // ══════════════════════════════════════════════════════════

        public static SystemError MissingRooms() =>
            new(ErrorSeverity.Critical, "DAT_001",
                "Nenhum dado de ambiente fornecido")
            {
                Category = ErrorCategory.MissingData,
                Suggestion = "Verifique se o modelo Revit contém Rooms definidos"
            };

        public static SystemError EmptyRoom(string roomId, string roomName) =>
            new(ErrorSeverity.Critical, "DAT_002",
                $"Ambiente '{roomName}' sem área definida")
            {
                Category = ErrorCategory.InvalidData,
                ComponentId = roomId,
                ComponentType = "Room",
                Suggestion = "Defina limites (boundaries) para o Room no Revit"
            };

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO NORMATIVA
        // ══════════════════════════════════════════════════════════

        public static SystemError AreaBelowMinimum(string roomId,
            string roomName, double area, double minimum) =>
            new(ErrorSeverity.Warning, "VAL_001",
                $"Ambiente '{roomName}' com área {area:F1}m² abaixo do mínimo {minimum:F1}m²")
            {
                Category = ErrorCategory.NormViolation,
                ComponentId = roomId,
                ComponentType = "Room",
                Context = new() { ["area"] = area, ["minimum"] = minimum },
                Suggestion = "Verifique se a classificação do ambiente está correta"
            };

        public static SystemError MissingEquipment(string roomId,
            string roomName, string equipmentName) =>
            new(ErrorSeverity.Warning, "VAL_002",
                $"Equipamento obrigatório '{equipmentName}' ausente em '{roomName}'")
            {
                Category = ErrorCategory.EquipmentViolation,
                ComponentId = roomId,
                ComponentType = "Room",
                Suggestion = $"Adicione {equipmentName} ao ambiente"
            };

        public static SystemError VelocityAboveLimit(string segmentId,
            double velocity, double limit) =>
            new(ErrorSeverity.Warning, "VAL_003",
                $"Velocidade {velocity:F2} m/s acima do limite {limit:F2} m/s")
            {
                Category = ErrorCategory.NormViolation,
                ComponentId = segmentId,
                ComponentType = "PipeSegment",
                Context = new() { ["velocity"] = velocity, ["limit"] = limit },
                Suggestion = "Aumente o DN do trecho"
            };

        public static SystemError PressureBelowMinimum(string pointId,
            double pressure, double minimum) =>
            new(ErrorSeverity.Critical, "VAL_004",
                $"Pressão {pressure:F2} mca abaixo do mínimo {minimum:F2} mca")
            {
                Category = ErrorCategory.NormViolation,
                ComponentId = pointId,
                ComponentType = "HydraulicPoint",
                Context = new() { ["pressure"] = pressure, ["minimum"] = minimum },
                Suggestion = "Revisite o caminho crítico — considere aumentar pressão de alimentação"
            };

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO
        // ══════════════════════════════════════════════════════════

        public static SystemError DivisionByZero(string context) =>
            new(ErrorSeverity.Critical, "CALC_001",
                $"Divisão por zero em {context}")
            {
                Category = ErrorCategory.CalculationError,
                Suggestion = "Verifique se os dados de entrada estão preenchidos"
            };

        public static SystemError NoCommercialDN(double theoreticalDN) =>
            new(ErrorSeverity.Warning, "CALC_002",
                $"DN teórico {theoreticalDN:F1}mm não tem equivalente comercial exato")
            {
                Category = ErrorCategory.NoCommercialDN,
                Context = new() { ["theoreticalDN"] = theoreticalDN },
                Suggestion = "Será usado o DN comercial imediatamente superior"
            };

        public static SystemError DNAutoRoundedUp(double theoretical, int commercial) =>
            new(ErrorSeverity.Info, "CALC_003",
                $"DN arredondado: {theoretical:F1}mm → {commercial}mm (comercial)")
            {
                Category = ErrorCategory.NoCommercialDN,
                Context = new()
                {
                    ["theoreticalDN"] = theoretical,
                    ["commercialDN"] = commercial
                }
            };

        // ══════════════════════════════════════════════════════════
        //  REDE
        // ══════════════════════════════════════════════════════════

        public static SystemError NetworkDisconnected(string networkSystem,
            List<string> disconnectedIds) =>
            new(ErrorSeverity.Critical, "NET_001",
                $"Rede {networkSystem} desconectada: {disconnectedIds.Count} pontos soltos")
            {
                Category = ErrorCategory.NetworkDisconnected,
                Context = new() { ["disconnectedIds"] = disconnectedIds },
                Suggestion = "Verifique se todos os pontos estão conectados à rede"
            };

        public static SystemError NetworkCycleDetected(string networkSystem) =>
            new(ErrorSeverity.Critical, "NET_002",
                $"Ciclo detectado na rede {networkSystem}")
            {
                Category = ErrorCategory.NetworkCycle,
                Suggestion = "Redes hidráulicas devem ser acíclicas (árvore)"
            };

        // ══════════════════════════════════════════════════════════
        //  DYNAMO
        // ══════════════════════════════════════════════════════════

        public static SystemError DynamoTimeout(string scriptId, int timeoutSec) =>
            new(ErrorSeverity.Critical, "DYN_001",
                $"Script '{scriptId}' excedeu timeout de {timeoutSec}s")
            {
                Category = ErrorCategory.Timeout,
                IsRetryable = true,
                Suggestion = "Tente novamente — pode ser lentidão temporária"
            };

        public static SystemError DynamoScriptError(string scriptId, string error) =>
            new(ErrorSeverity.Critical, "DYN_002",
                $"Erro no script Dynamo '{scriptId}': {error}")
            {
                Category = ErrorCategory.DynamoError,
                IsRetryable = false,
                Suggestion = "Verifique o script .dyn e os dados de entrada"
            };

        // ══════════════════════════════════════════════════════════
        //  SISTEMA
        // ══════════════════════════════════════════════════════════

        public static SystemError UnexpectedException(Exception ex, string stepId = null) =>
            new(ErrorSeverity.Critical, "SYS_001",
                $"Erro inesperado: {ex.Message}")
            {
                Category = ErrorCategory.SystemError,
                OriginalException = ex,
                StepId = stepId,
                Detail = ex.StackTrace,
                Suggestion = "Verifique os logs para detalhes"
            };

        public static SystemError OptionalEquipmentSkipped(string roomName,
            string equipmentName) =>
            new(ErrorSeverity.Info, "INFO_001",
                $"Equipamento opcional '{equipmentName}' não incluído em '{roomName}'")
            {
                Category = ErrorCategory.EquipmentViolation,
            };
    }
}
```

---

## 9. Integração com Pipeline

### 9.1 Step com ErrorCollector

```csharp
namespace HidraulicoPlugin.Core.Pipeline.Steps
{
    /// <summary>
    /// Etapa que usa ErrorCollector para acumular e avaliar erros.
    /// </summary>
    public class ClassifyRoomsStep : ProcessingStepBase
    {
        private readonly IRoomService _roomService;

        public override string StepId => "E02";
        public override string Name => "ClassifyRooms";
        public override string Description => "Classificação de ambientes";
        public override int Order => 2;
        public override IReadOnlyList<string> Dependencies => new[] { "E01" };

        public ClassifyRoomsStep(IRoomService roomService)
        {
            _roomService = roomService;
        }

        protected override StepResult ExecuteCore(PipelineContext context)
        {
            var errors = new ErrorCollector(StepId);

            foreach (var room in context.DetectedRooms)
            {
                // Classificar
                var classification = _roomService.Classify(room);
                room.RoomType = classification.RoomType;

                // Validar
                if (room.AreaSqM <= 0)
                {
                    errors.AddCritical("VAL_010",
                        $"Ambiente '{room.Name}' com área zero",
                        suggestion: "Defina boundaries no Revit");
                    continue; // Não processar mais este room
                }

                if (room.AreaSqM < GetMinimumArea(room.RoomType))
                {
                    errors.Add(ErrorFactory.AreaBelowMinimum(
                        room.Id, room.Name, room.AreaSqM,
                        GetMinimumArea(room.RoomType)));
                }

                if (classification.Confidence < 0.7)
                {
                    errors.AddWarning("VAL_011",
                        $"Classificação de '{room.Name}' com baixa confiança ({classification.Confidence:P0})",
                        suggestion: "Verifique se o nome do Room está correto");
                }
            }

            // Separar wet areas
            context.ClassifiedRooms = context.DetectedRooms;
            context.WetAreas = context.DetectedRooms
                .Where(r => IsWetArea(r.RoomType))
                .ToList();

            if (context.WetAreas.Count == 0)
            {
                errors.AddCritical("VAL_012",
                    "Nenhuma área molhada encontrada",
                    suggestion: "O modelo precisa ter pelo menos 1 banheiro/cozinha/lavanderia");
            }

            // 🎯 AVALIAR — O ErrorCollector decide a ação
            var evaluation = errors.Evaluate();

            // Armazenar erros no contexto para UI
            context.Set($"{StepId}:errors", errors.All.ToList());
            context.Set($"{StepId}:evaluation", evaluation);

            switch (evaluation.Action)
            {
                case PipelineAction.Stop:
                    return StepResult.Failure(StepId, evaluation.Summary);

                case PipelineAction.PauseForApproval:
                    // Retorna sucesso, mas RequiresApproval = true
                    // → PipelineRunner vai pausar em WaitingApproval
                    return StepResult.Success(StepId,
                        $"{context.WetAreas.Count} áreas molhadas. {evaluation.Summary}",
                        new Dictionary<string, object>
                        {
                            ["wetAreas"] = context.WetAreas.Count,
                            ["warnings"] = errors.WarningCount
                        });

                default: // Continue
                    return StepResult.Success(StepId,
                        $"{context.WetAreas.Count} áreas molhadas classificadas",
                        new Dictionary<string, object>
                        {
                            ["wetAreas"] = context.WetAreas.Count,
                            ["infos"] = errors.InfoCount
                        });
            }
        }

        private double GetMinimumArea(RoomType type) => type switch
        {
            RoomType.Bathroom => 2.0,
            RoomType.Lavatory => 1.2,
            RoomType.Kitchen => 4.0,
            RoomType.Laundry => 2.5,
            _ => 0
        };

        private bool IsWetArea(RoomType type) =>
            type is RoomType.Bathroom or RoomType.Lavatory
                or RoomType.Kitchen or RoomType.Laundry
                or RoomType.MasterBathroom;
    }
}
```

### 9.2 PipelineRunner reagindo à avaliação

```csharp
// No PipelineRunner, após ExecuteStep:
private StepResult ExecuteStep(IProcessingStep step)
{
    var result = step.Execute(_context);

    // Verificar se há avaliação de erros no contexto
    var evaluation = _context.Get<ErrorEvaluation>($"{step.StepId}:evaluation");

    if (evaluation != null)
    {
        switch (evaluation.Action)
        {
            case PipelineAction.Stop:
                state.Status = StepStatus.Failed;
                _eventBus.Publish(new StepFailedEvent
                {
                    StepId = step.StepId,
                    ErrorMessage = evaluation.Summary
                });
                break;

            case PipelineAction.PauseForApproval:
                // Forçar WaitingApproval mesmo se step não requer
                state.Status = StepStatus.WaitingApproval;
                _eventBus.Publish(new StepCompletedEvent
                {
                    StepId = step.StepId,
                    Summary = evaluation.Summary,
                    RequiresApproval = true
                });
                break;
        }
    }

    return result;
}
```

---

## 10. Integração com Observer

```csharp
namespace HidraulicoPlugin.Core.Events
{
    /// <summary>
    /// Evento emitido quando um erro é registrado.
    /// </summary>
    public class ErrorOccurredEvent : PipelineEvent
    {
        public override PipelineEventType EventType =>
            PipelineEventType.ValidationFailed;

        public SystemError Error { get; set; }

        public ErrorOccurredEvent(SystemError error)
        {
            Error = error;
            Source = error.StepId;
            CorrelationId = error.CorrelationId;
        }
    }

    /// <summary>
    /// Evento emitido quando uma avaliação de erros é concluída.
    /// </summary>
    public class ErrorEvaluationEvent : PipelineEvent
    {
        public override PipelineEventType EventType =>
            PipelineEventType.ValidationWarning;

        public ErrorEvaluation Evaluation { get; set; }
        public string StepId { get; set; }
    }
}

// No ErrorCollector, emitir eventos ao adicionar:
public class ErrorCollector
{
    private readonly EventBus _eventBus; // opcional

    public ErrorCollector(string stepId, EventBus eventBus = null)
    {
        _stepId = stepId;
        _eventBus = eventBus;
    }

    public void Add(SystemError error)
    {
        error.StepId ??= _stepId;
        _errors.Add(error);

        // Emitir evento
        _eventBus?.Publish(new ErrorOccurredEvent(error));
    }
}
```

---

## 11. Catálogo de Códigos de Erro

```
┌──────────┬────────────┬──────────────────────────────────────────────┬──────────┐
│ Código   │ Severidade │ Mensagem                                     │ Categoria│
├──────────┼────────────┼──────────────────────────────────────────────┼──────────┤
│ DAT_001  │ 🛑 Critical│ Nenhum dado de ambiente fornecido            │ Missing  │
│ DAT_002  │ 🛑 Critical│ Ambiente sem área definida                   │ Invalid  │
│ VAL_001  │ ⚠️ Warning │ Área abaixo do mínimo                        │ Norm     │
│ VAL_002  │ ⚠️ Warning │ Equipamento obrigatório ausente              │ Equipment│
│ VAL_003  │ ⚠️ Warning │ Velocidade acima do limite                   │ Norm     │
│ VAL_004  │ 🛑 Critical│ Pressão abaixo do mínimo (ponto crítico)    │ Norm     │
│ CALC_001 │ 🛑 Critical│ Divisão por zero                             │ Calc     │
│ CALC_002 │ ⚠️ Warning │ DN teórico sem equivalente comercial         │ NoDN     │
│ CALC_003 │ ℹ️ Info    │ DN arredondado automaticamente               │ NoDN     │
│ NET_001  │ 🛑 Critical│ Rede desconectada                            │ Network  │
│ NET_002  │ 🛑 Critical│ Ciclo detectado na rede                      │ Network  │
│ DYN_001  │ 🛑 Critical│ Script Dynamo timeout (retryable)            │ Timeout  │
│ DYN_002  │ 🛑 Critical│ Erro no script Dynamo                        │ Dynamo   │
│ SYS_001  │ 🛑 Critical│ Exceção inesperada                           │ System   │
│ INFO_001 │ ℹ️ Info    │ Equipamento opcional não incluído             │ Equipment│
└──────────┴────────────┴──────────────────────────────────────────────┴──────────┘
```

---

## 12. Boas Práticas

### 12.1 Quando usar cada severidade

```
🛑 CRITICAL — Use apenas quando:
  ✓ É impossível continuar sem correção
  ✓ Dados de entrada faltam completamente
  ✓ Resultado seria estruturalmente incorreto
  ✓ Violação de segurança hidráulica (pressão negativa)

⚠️ WARNING — Use quando:
  ✓ Resultado é válido mas não ideal
  ✓ Valor está fora do range recomendado (mas não proibido)
  ✓ Classificação automática tem baixa confiança
  ✓ Arredondamento pode impactar resultado

ℹ️ INFO — Use quando:
  ✓ Ação automática foi tomada (arredondamento, skip de opcional)
  ✓ Informação útil para auditoria
  ✓ Nenhum impacto na execução
```

### 12.2 Mensagens boas vs. ruins

```
❌ "Erro de validação"
✅ "Banheiro 'BWC Suíte' com 1.8m² — mínimo recomendado: 2.0m²"

❌ "Cálculo falhou"
✅ "Velocidade 3.2 m/s no trecho seg_af_003 (DN 20) — limite: 3.0 m/s"

❌ "Dados inválidos"
✅ "Room 'Sala TV' não possui boundaries definidos no modelo Revit"
```

### 12.3 Contexto rico

```csharp
// ❌ Sem contexto:
errors.AddWarning("VAL_003", "Velocidade alta");

// ✅ Com contexto:
errors.AddForComponent(
    ErrorSeverity.Warning,
    "VAL_003",
    $"Velocidade {v:F2} m/s acima de {limit:F2} m/s",
    componentId: segment.Id,
    componentType: "PipeSegment",
    context: new()
    {
        ["velocity"] = v,
        ["limit"] = limit,
        ["dn"] = segment.DiameterMm,
        ["flow"] = segment.FlowLps,
        ["suggestion"] = $"Aumente para DN {nextDN}mm"
    });
```

---

## 13. Resumo Visual

```
Error Classification Pattern
│
├── ErrorSeverity (Enum)
│   ├── Critical → 🛑 STOP pipeline (Failed)
│   ├── Warning  → ⏸ PAUSE (WaitingApproval)
│   └── Info     → ✅ CONTINUE (registra apenas)
│
├── SystemError (Classe)
│   ├── ErrorId, Code, Severity, Category
│   ├── Message, Detail, Suggestion
│   ├── StepId, ComponentId, ComponentType, Context
│   ├── CanBeIgnored, IsRetryable, Resolution
│   └── Timestamp, OriginalException
│
├── ErrorCategory (Enum — 14 categorias)
│   ├── Data: Missing, Invalid, Inconsistent
│   ├── Validation: Norm, Spatial, Equipment
│   ├── Calculation: Error, OutOfRange, NoCommercialDN
│   ├── Network: Disconnected, Cycle, RoutingFailed
│   ├── Integration: DynamoError, DynamoUnavailable, Json
│   └── System: SystemError, Timeout
│
├── ErrorCollector (Acumulador por etapa)
│   ├── Add/AddCritical/AddWarning/AddInfo
│   ├── HasCritical, HasWarnings, Count
│   ├── GetBySeverity, GetByComponent
│   └── Evaluate() → ErrorEvaluation
│           ├── Action: Stop | PauseForApproval | Continue
│           └── Determina o que o pipeline faz
│
├── ErrorFactory (Erros pré-definidos — 15 tipos)
│   ├── MissingRooms, EmptyRoom
│   ├── AreaBelowMinimum, MissingEquipment, VelocityAboveLimit
│   ├── PressureBelowMinimum, DivisionByZero, NoCommercialDN
│   ├── NetworkDisconnected, NetworkCycleDetected
│   ├── DynamoTimeout, DynamoScriptError
│   └── UnexpectedException
│
├── ErrorResolution (Enum)
│   ├── Unresolved, Fixed, Ignored, AutoResolved, Rejected
│
└── Integração
    ├── Pipeline: ErrorCollector no ExecuteCore() → Evaluate()
    ├── Observer: ErrorOccurredEvent, ErrorEvaluationEvent
    └── Log: ILogService via severidade
```
