# Serviço de Domínio — IOrchestratorService (IOrquestradorService)

> Especificação completa da interface do orquestrador de pipeline, responsável por controlar a execução por etapas, gerenciar estado, validação humana, retry/rollback e integração entre todos os serviços do PluginCore.

---

## 1. Definição da Interface

### 1.1 O que é IOrchestratorService

`IOrchestratorService` é o **maestro do sistema**. Ele não executa lógica de negócio — ele coordena quem executa, quando executa, e o que fazer se falhar. É uma **state machine** que gerencia a progressão das etapas E01→E12, garantindo que dependências sejam respeitadas, validações humanas sejam aguardadas, e erros sejam tratados de forma previsível.

### 1.2 Papel no sistema

```
╔═══════════════════════════════════════════════════════════════╗
║                      UI / Command                             ║
║  [▶ Start]  [✅ Approve]  [↻ Retry]  [◀ Rollback]  [Status] ║
╚═══════════════════════╤═══════════════════════════════════════╝
                        │
              ╔═════════╧══════════╗
              ║ IOrchestratorSvc   ║  ← ESTE SERVIÇO
              ╠════════════════════╣
              ║ StartPipeline()    ║
              ║ ExecuteNextStep()  ║
              ║ ApproveStep()      ║
              ║ RetryStep()        ║
              ║ RollbackStep()     ║
              ╚═╤══╤══╤══╤══╤═════╝
                │  │  │  │  │
    ┌───────────┘  │  │  │  └───────────┐
    ▼              ▼  ▼  ▼              ▼
IRoomSvc    IEquipSvc  INetSvc  ISizingSvc  ILogSvc
(E01/E02)   (E03/E04)  (E06-09)  (E11)     (todos)
```

### 1.3 Por que é independente do Revit

```
O ORQUESTRADOR:
  - NÃO acessa API do Revit
  - NÃO cria elementos no modelo
  - NÃO executa scripts Dynamo diretamente

O QUE ELE FAZ:
  - Gerencia ESTADO (qual etapa, qual status)
  - Chama SERVIÇOS do Core (que são agnósticos)
  - Delega para ADAPTERS quando precisa do Revit

QUANDO PRECISA DO REVIT:
  Etapa E10 (ExportToRevit) chama IRevitAdapter (Infrastructure)
  O Orquestrador não sabe que é Revit — ele chama uma interface

RESULTADO:
  - Pode testar o pipeline INTEIRO sem Revit instalado
  - Pode simular execução com mocks
  - Pode rodar em ambiente de CI/CD
```

---

## 2. Responsabilidades

### 2.1 O que o serviço DEVE fazer

| Ação | Detalhe |
|------|---------|
| Gerenciar ordem de execução | E01 → E02 → E03 → ... → E12 |
| Verificar dependências | E04 só roda se E02 concluiu |
| Chamar serviços corretos | E01 → IRoomService.DetectRooms() |
| Aguardar aprovação humana | Pausar entre etapas para revisão |
| Registrar estado | Qual etapa, status, dados intermediários |
| Reexecutar etapas | Retry com mesmo input ou input corrigido |
| Rollback | Desfazer última etapa e voltar ao estado anterior |
| Logar tudo | Via ILogService: início, fim, erro, aprovação |
| Persistir progresso | Salvar estado para retomar após crash |
| Reportar status | Dashboard de progresso para UI |

### 2.2 O que NÃO DEVE fazer

| Proibição | Quem faz |
|-----------|---------|
| ❌ Lógica de classificação | IRoomService |
| ❌ Cálculos hidráulicos | ISizingService |
| ❌ Geração de redes | INetworkService |
| ❌ Posicionamento de equipamentos | IEquipmentService |
| ❌ Criar elementos no Revit | IRevitAdapter (Infrastructure) |
| ❌ Decisões de domínio | Serviços especializados |
| ❌ Manipulação de UI | Camada UI |

---

## 3. Conceitos-Chave

### 3.1 Pipeline

```
Pipeline = sequência ordenada de Steps com dependências.
É configurável: pode pular etapas opcionais, reordenar parcialmente.
Uma execução = 1 PipelineSession com CorrelationId único.
```

### 3.2 Step (Etapa)

```
Step = unidade atômica de execução.
Tem: ID, nome, estado, dependências, executor, dados de entrada/saída.
Uma etapa ou completa inteiramente ou falha inteiramente (atômica).
```

### 3.3 Estado (State Machine)

```
Cada Step tem um estado:

  ┌──────────────────────────────────────────────────────────┐
  │                                                          │
  │   Pending ──→ Running ──→ WaitingApproval ──→ Completed  │
  │     │           │                │                       │
  │     │           ▼                ▼                       │
  │     │         Failed ◄──── Rejected                      │
  │     │           │                                        │
  │     │           ▼                                        │
  │     │       RolledBack                                   │
  │     │           │                                        │
  │     └───────────┘  (retry volta para Pending)            │
  │                                                          │
  └──────────────────────────────────────────────────────────┘
```

### 3.4 Transições válidas

| De | Para | Gatilho |
|----|------|---------|
| Pending | Running | ExecuteStep / ExecuteNextStep |
| Running | WaitingApproval | Step concluiu, requer aprovação |
| Running | Completed | Step concluiu, sem aprovação necessária |
| Running | Failed | Step lançou exceção |
| WaitingApproval | Completed | ApproveStep |
| WaitingApproval | Rejected | RejectStep |
| Rejected | Pending | RetryStep |
| Failed | Pending | RetryStep |
| Failed | RolledBack | RollbackStep |
| Completed | RolledBack | RollbackStep |

---

## 4. Enums

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Estado de uma etapa do pipeline.
    /// </summary>
    public enum StepStatus
    {
        /// <summary>Aguardando execução.</summary>
        Pending = 0,

        /// <summary>Em execução.</summary>
        Running = 1,

        /// <summary>Concluída, aguardando aprovação humana.</summary>
        WaitingApproval = 2,

        /// <summary>Concluída e aprovada.</summary>
        Completed = 3,

        /// <summary>Falhou durante execução.</summary>
        Failed = 4,

        /// <summary>Rejeitada pelo usuário (após WaitingApproval).</summary>
        Rejected = 5,

        /// <summary>Desfeita (rollback aplicado).</summary>
        RolledBack = 6,

        /// <summary>Ignorada (pulada pelo usuário).</summary>
        Skipped = 7
    }

    /// <summary>
    /// Estado geral do pipeline.
    /// </summary>
    public enum PipelineStatus
    {
        /// <summary>Não iniciado.</summary>
        NotStarted = 0,

        /// <summary>Em execução (alguma etapa rodando).</summary>
        Running = 1,

        /// <summary>Pausado (aguardando aprovação humana).</summary>
        Paused = 2,

        /// <summary>Concluído com sucesso (todas as etapas).</summary>
        Completed = 3,

        /// <summary>Falhou (alguma etapa crítica falhou).</summary>
        Failed = 4,

        /// <summary>Cancelado pelo usuário.</summary>
        Cancelled = 5
    }
}
```

---

## 5. Modelo de Etapa (PipelineStep)

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Definição de uma etapa do pipeline.
    /// </summary>
    public class PipelineStep
    {
        // ── Identidade ──────────────────────────────────────────

        /// <summary>ID único da etapa (ex: "E01").</summary>
        public string Id { get; set; }

        /// <summary>Nome técnico (ex: "E01_DetectRooms").</summary>
        public string Name { get; set; }

        /// <summary>Descrição humana (ex: "Detecção de ambientes").</summary>
        public string Description { get; set; }

        /// <summary>Ordem de execução (1, 2, 3...).</summary>
        public int Order { get; set; }

        // ── Dependências ────────────────────────────────────────

        /// <summary>IDs das etapas que devem estar Completed antes.</summary>
        public List<string> DependsOn { get; set; } = new();

        /// <summary>Se a etapa é obrigatória ou pode ser pulada.</summary>
        public bool IsMandatory { get; set; } = true;

        /// <summary>Se requer aprovação humana antes de prosseguir.</summary>
        public bool RequiresApproval { get; set; } = true;

        // ── Estado ──────────────────────────────────────────────

        /// <summary>Estado atual da etapa.</summary>
        public StepStatus Status { get; set; } = StepStatus.Pending;

        /// <summary>Número de tentativas de execução.</summary>
        public int AttemptCount { get; set; }

        /// <summary>Máximo de tentativas permitidas.</summary>
        public int MaxAttempts { get; set; } = 3;

        // ── Tempo ───────────────────────────────────────────────

        /// <summary>Quando iniciou a execução.</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>Quando concluiu.</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>Duração.</summary>
        public TimeSpan? Duration =>
            StartedAt.HasValue && CompletedAt.HasValue
                ? CompletedAt.Value - StartedAt.Value
                : null;

        // ── Resultados ──────────────────────────────────────────

        /// <summary>Resumo do resultado (para exibição).</summary>
        public string ResultSummary { get; set; }

        /// <summary>Dados de saída (serializados).</summary>
        public object OutputData { get; set; }

        /// <summary>Mensagem de erro (se Failed).</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Detalhes da exceção (se Failed).</summary>
        public string ExceptionDetails { get; set; }

        // ── Metadados ───────────────────────────────────────────

        /// <summary>Se a etapa foi aprovada pelo usuário.</summary>
        public bool? UserApproved { get; set; }

        /// <summary>Comentário do usuário na aprovação/rejeição.</summary>
        public string UserComment { get; set; }

        /// <summary>Serviço que executa esta etapa.</summary>
        public string ExecutorService { get; set; }

        // ── Métodos auxiliares ───────────────────────────────────

        public bool CanExecute => Status is StepStatus.Pending or StepStatus.RolledBack;
        public bool CanApprove => Status == StepStatus.WaitingApproval;
        public bool CanReject => Status == StepStatus.WaitingApproval;
        public bool CanRetry => Status is StepStatus.Failed or StepStatus.Rejected
                                 && AttemptCount < MaxAttempts;
        public bool CanRollback => Status is StepStatus.Completed or StepStatus.Failed;
        public bool CanSkip => !IsMandatory && Status == StepStatus.Pending;
        public bool IsTerminal => Status is StepStatus.Completed or StepStatus.Skipped;
    }
}
```

---

## 6. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Orquestrador do pipeline de execução.
    /// Coordena etapas E01→E12, gerencia estado, aguarda aprovação humana,
    /// controla retry/rollback e integra todos os serviços.
    /// Independente do Revit.
    /// </summary>
    public interface IOrchestratorService
    {
        // ══════════════════════════════════════════════════════════
        //  CICLO DE VIDA DO PIPELINE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Inicializa o pipeline com configuração padrão.
        /// Cria a sessão, define as etapas e suas dependências,
        /// registra CorrelationId no ILogService.
        /// </summary>
        /// <param name="config">Configuração (etapas opcionais, opções).</param>
        /// <returns>Sessão do pipeline criada.</returns>
        PipelineSession StartPipeline(PipelineConfig config = null);

        /// <summary>
        /// Cancela a execução do pipeline.
        /// </summary>
        /// <param name="reason">Motivo do cancelamento.</param>
        void CancelPipeline(string reason = null);

        /// <summary>
        /// Retoma um pipeline salvo (após crash ou pausa intencional).
        /// </summary>
        /// <param name="sessionId">ID da sessão a retomar.</param>
        PipelineSession ResumePipeline(string sessionId);

        // ══════════════════════════════════════════════════════════
        //  EXECUÇÃO DE ETAPAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa a próxima etapa pendente na sequência.
        /// Verifica dependências antes de executar.
        /// Se a etapa requer aprovação, pausa em WaitingApproval.
        /// </summary>
        /// <returns>Resultado da execução.</returns>
        StepExecutionResult ExecuteNextStep();

        /// <summary>
        /// Executa uma etapa específica por ID.
        /// Útil para reexecução ou execução fora de ordem.
        /// Verifica dependências antes de executar.
        /// </summary>
        /// <param name="stepId">ID da etapa (ex: "E01").</param>
        /// <returns>Resultado da execução.</returns>
        StepExecutionResult ExecuteStep(string stepId);

        /// <summary>
        /// Executa todas as etapas pendentes até encontrar uma que
        /// requer aprovação ou até o final.
        /// Modo "auto-pilot".
        /// </summary>
        /// <returns>Resultado com lista de etapas executadas.</returns>
        BatchStepExecutionResult ExecuteUntilApproval();

        /// <summary>
        /// Pula uma etapa opcional.
        /// Só funciona para etapas com IsMandatory = false.
        /// </summary>
        void SkipStep(string stepId);

        // ══════════════════════════════════════════════════════════
        //  APROVAÇÃO HUMANA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aprova uma etapa que está em WaitingApproval.
        /// Muda o estado para Completed e libera a próxima.
        /// </summary>
        /// <param name="stepId">ID da etapa.</param>
        /// <param name="comment">Comentário do usuário (opcional).</param>
        void ApproveStep(string stepId, string comment = null);

        /// <summary>
        /// Rejeita uma etapa que está em WaitingApproval.
        /// Muda o estado para Rejected. Requer RetryStep para re-executar.
        /// </summary>
        /// <param name="stepId">ID da etapa.</param>
        /// <param name="reason">Motivo da rejeição.</param>
        void RejectStep(string stepId, string reason);

        // ══════════════════════════════════════════════════════════
        //  RETRY E ROLLBACK
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Reexecuta uma etapa que falhou ou foi rejeitada.
        /// Respeita MaxAttempts.
        /// </summary>
        /// <param name="stepId">ID da etapa.</param>
        /// <returns>Resultado da nova tentativa.</returns>
        StepExecutionResult RetryStep(string stepId);

        /// <summary>
        /// Desfaz uma etapa (limpa output, volta para Pending).
        /// Também faz rollback de etapas dependentes (cascata).
        /// </summary>
        /// <param name="stepId">ID da etapa.</param>
        /// <returns>Lista de etapas afetadas pelo rollback.</returns>
        RollbackResult RollbackStep(string stepId);

        /// <summary>
        /// Desfaz todas as etapas a partir de uma específica (inclusive).
        /// </summary>
        RollbackResult RollbackFrom(string stepId);

        // ══════════════════════════════════════════════════════════
        //  CONSULTA DE ESTADO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o status geral do pipeline.
        /// </summary>
        PipelineStatusReport GetPipelineStatus();

        /// <summary>
        /// Retorna o estado da etapa atual (ou a próxima a executar).
        /// </summary>
        PipelineStep GetCurrentStep();

        /// <summary>
        /// Retorna a definição de uma etapa por ID.
        /// </summary>
        PipelineStep GetStep(string stepId);

        /// <summary>
        /// Retorna todas as etapas do pipeline.
        /// </summary>
        List<PipelineStep> GetAllSteps();

        /// <summary>
        /// Retorna as etapas que podem ser executadas agora
        /// (dependências satisfeitas + status Pending).
        /// </summary>
        List<PipelineStep> GetExecutableSteps();

        /// <summary>
        /// Retorna o progresso geral (0.0 a 1.0).
        /// </summary>
        double GetProgress();

        /// <summary>
        /// Retorna os dados de saída de uma etapa concluída.
        /// Usado para passar dados entre etapas.
        /// </summary>
        T GetStepOutput<T>(string stepId);

        // ══════════════════════════════════════════════════════════
        //  PERSISTÊNCIA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Salva o estado atual do pipeline em disco.
        /// Permite retomar após crash.
        /// </summary>
        void SaveState();

        /// <summary>
        /// Carrega o estado de uma sessão salva.
        /// </summary>
        PipelineSession LoadState(string sessionId);

        // ══════════════════════════════════════════════════════════
        //  EVENTOS (para UI)
        // ══════════════════════════════════════════════════════════

        /// <summary>Disparado quando uma etapa muda de estado.</summary>
        event EventHandler<StepStateChangedEventArgs> OnStepStateChanged;

        /// <summary>Disparado quando o pipeline muda de estado.</summary>
        event EventHandler<PipelineStateChangedEventArgs> OnPipelineStateChanged;

        /// <summary>Disparado quando há progresso em uma etapa.</summary>
        event EventHandler<StepProgressEventArgs> OnStepProgress;
    }
}
```

---

## 7. Definição das Etapas (Configuração)

```csharp
namespace HidraulicoPlugin.Core.Configuration
{
    /// <summary>
    /// Configuração do pipeline. Define quais etapas executar e opções.
    /// </summary>
    public class PipelineConfig
    {
        /// <summary>Nome da sessão.</summary>
        public string SessionName { get; set; } = "HydraulicPipeline";

        /// <summary>Se deve pular etapas opcionais automaticamente.</summary>
        public bool SkipOptionalSteps { get; set; } = false;

        /// <summary>Se deve executar sem parar para aprovação (modo expert).</summary>
        public bool AutoApprove { get; set; } = false;

        /// <summary>Máximo de tentativas por etapa.</summary>
        public int DefaultMaxAttempts { get; set; } = 3;

        /// <summary>Dados iniciais (ex: lista de RoomRawData).</summary>
        public object InitialData { get; set; }

        /// <summary>Etapas para pular (ex: ["E09"] para pular otimização).</summary>
        public List<string> StepsToSkip { get; set; } = new();
    }

    /// <summary>
    /// Fábrica de etapas do pipeline. Define a sequência padrão.
    /// </summary>
    public static class PipelineStepFactory
    {
        public static List<PipelineStep> CreateDefaultPipeline()
        {
            return new List<PipelineStep>
            {
                new PipelineStep
                {
                    Id = "E01", Order = 1,
                    Name = "E01_DetectRooms",
                    Description = "Detecção de ambientes",
                    DependsOn = new(),
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "IRoomService"
                },
                new PipelineStep
                {
                    Id = "E02", Order = 2,
                    Name = "E02_ClassifyRooms",
                    Description = "Classificação de ambientes",
                    DependsOn = new() { "E01" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "IRoomService"
                },
                new PipelineStep
                {
                    Id = "E03", Order = 3,
                    Name = "E03_IdentifyEquipment",
                    Description = "Identificação de equipamentos por ambiente",
                    DependsOn = new() { "E02" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "IEquipmentService"
                },
                new PipelineStep
                {
                    Id = "E04", Order = 4,
                    Name = "E04_InsertEquipment",
                    Description = "Inserção e posicionamento de equipamentos",
                    DependsOn = new() { "E03" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "IEquipmentService"
                },
                new PipelineStep
                {
                    Id = "E05", Order = 5,
                    Name = "E05_ValidateModel",
                    Description = "Validação geral do modelo",
                    DependsOn = new() { "E04" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "IValidationService"
                },
                new PipelineStep
                {
                    Id = "E06", Order = 6,
                    Name = "E06_BuildRisers",
                    Description = "Geração de prumadas",
                    DependsOn = new() { "E05" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "INetworkService"
                },
                new PipelineStep
                {
                    Id = "E07", Order = 7,
                    Name = "E07_BuildColdWater",
                    Description = "Geração da rede de água fria",
                    DependsOn = new() { "E06" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "INetworkService"
                },
                new PipelineStep
                {
                    Id = "E08", Order = 8,
                    Name = "E08_BuildSewer",
                    Description = "Geração da rede de esgoto",
                    DependsOn = new() { "E06" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "INetworkService"
                },
                new PipelineStep
                {
                    Id = "E08b", Order = 9,
                    Name = "E08b_BuildVentilation",
                    Description = "Geração da rede de ventilação",
                    DependsOn = new() { "E08" },
                    IsMandatory = true,
                    RequiresApproval = false,
                    ExecutorService = "INetworkService"
                },
                new PipelineStep
                {
                    Id = "E09", Order = 10,
                    Name = "E09_OptimizeNetworks",
                    Description = "Otimização das redes",
                    DependsOn = new() { "E07", "E08", "E08b" },
                    IsMandatory = false,
                    RequiresApproval = false,
                    ExecutorService = "INetworkService"
                },
                new PipelineStep
                {
                    Id = "E10", Order = 11,
                    Name = "E10_ExportToRevit",
                    Description = "Criação de elementos no Revit",
                    DependsOn = new() { "E09" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "IRevitAdapter"
                },
                new PipelineStep
                {
                    Id = "E11", Order = 12,
                    Name = "E11_SizeNetworks",
                    Description = "Dimensionamento hidráulico",
                    DependsOn = new() { "E07", "E08", "E08b" },
                    IsMandatory = true,
                    RequiresApproval = true,
                    ExecutorService = "ISizingService"
                },
                new PipelineStep
                {
                    Id = "E12", Order = 13,
                    Name = "E12_GenerateTables",
                    Description = "Geração de tabelas e relatórios",
                    DependsOn = new() { "E11" },
                    IsMandatory = false,
                    RequiresApproval = false,
                    ExecutorService = "IReportService"
                }
            };
        }
    }
}
```

### 7.1 Grafo de dependências

```
E01 ──→ E02 ──→ E03 ──→ E04 ──→ E05 ──→ E06 ──┬──→ E07 ──┐
                                                 │          │
                                                 ├──→ E08 ──┼──→ E09 ──→ E10
                                                 │    │     │
                                                 │    ▼     │
                                                 │   E08b ──┘
                                                 │
                                                 └──→ E07 ─┐
                                                      E08  ├──→ E11 ──→ E12
                                                      E08b─┘
```

**Nota:** E07 e E08 podem rodar em paralelo (ambos dependem de E06).
E09 depende de E07 + E08 + E08b (todas as redes prontas).
E11 pode rodar independente de E10 (dimensionamento ≠ criação no Revit).

---

## 8. DTOs de Resultado

### 8.1 PipelineSession

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Sessão de execução do pipeline.
    /// </summary>
    public class PipelineSession
    {
        public string SessionId { get; set; }
        public string CorrelationId { get; set; }
        public string SessionName { get; set; }
        public PipelineStatus Status { get; set; }
        public List<PipelineStep> Steps { get; set; } = new();
        public PipelineConfig Config { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan Elapsed => (CompletedAt ?? DateTime.UtcNow) - CreatedAt;

        /// <summary>Dados compartilhados entre etapas.</summary>
        public Dictionary<string, object> SharedData { get; set; } = new();
    }
}
```

### 8.2 StepExecutionResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da execução de uma etapa.
    /// </summary>
    public class StepExecutionResult
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public StepStatus Status { get; set; }
        public bool IsSuccessful { get; set; }

        /// <summary>Resumo para exibição na UI.</summary>
        public string Summary { get; set; }

        /// <summary>Dados de saída da etapa.</summary>
        public object OutputData { get; set; }

        /// <summary>Mensagem de erro (se falhou).</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Se a etapa requer aprovação humana.</summary>
        public bool RequiresApproval { get; set; }

        /// <summary>Número da tentativa.</summary>
        public int AttemptNumber { get; set; }

        /// <summary>Tempo de execução.</summary>
        public TimeSpan Duration { get; set; }
    }

    public class BatchStepExecutionResult
    {
        public List<StepExecutionResult> Results { get; set; } = new();
        public string StoppedAtStepId { get; set; }
        public string StopReason { get; set; }
        public int TotalExecuted => Results.Count;
        public int TotalSuccessful => Results.Count(r => r.IsSuccessful);
        public TimeSpan TotalDuration { get; set; }
    }
}
```

### 8.3 RollbackResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class RollbackResult
    {
        /// <summary>Etapas que sofreram rollback (em cascata).</summary>
        public List<string> RolledBackStepIds { get; set; } = new();

        /// <summary>Novo step atual após rollback.</summary>
        public string NewCurrentStepId { get; set; }

        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
    }
}
```

### 8.4 PipelineStatusReport

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Relatório completo do status do pipeline (para UI/dashboard).
    /// </summary>
    public class PipelineStatusReport
    {
        public string SessionId { get; set; }
        public PipelineStatus Status { get; set; }
        public double Progress { get; set; }
        public string CurrentStepId { get; set; }
        public string CurrentStepDescription { get; set; }
        public TimeSpan Elapsed { get; set; }

        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int PendingSteps { get; set; }
        public int FailedSteps { get; set; }
        public int SkippedSteps { get; set; }

        public List<StepStatusSummary> StepSummaries { get; set; } = new();
    }

    public class StepStatusSummary
    {
        public string StepId { get; set; }
        public string Description { get; set; }
        public StepStatus Status { get; set; }
        public TimeSpan? Duration { get; set; }
        public string ResultSummary { get; set; }
        public int AttemptCount { get; set; }
    }
}
```

### 8.5 Eventos (para UI)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class StepStateChangedEventArgs : EventArgs
    {
        public string StepId { get; set; }
        public StepStatus OldStatus { get; set; }
        public StepStatus NewStatus { get; set; }
        public string Message { get; set; }
    }

    public class PipelineStateChangedEventArgs : EventArgs
    {
        public PipelineStatus OldStatus { get; set; }
        public PipelineStatus NewStatus { get; set; }
        public string Reason { get; set; }
    }

    public class StepProgressEventArgs : EventArgs
    {
        public string StepId { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
        public string Detail { get; set; }
        public double Percent => Total > 0 ? (double)Current / Total * 100 : 0;
    }
}
```

---

## 9. Integração com Serviços

```csharp
/// <summary>
/// Mapa de execução: qual serviço e qual método cada etapa chama.
/// Usado internamente pela implementação do Orquestrador.
/// </summary>
public static class StepExecutorMap
{
    /*
    ┌──────┬────────────────────────────────────────────────────────────┐
    │ Step │ Execução                                                  │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E01  │ _roomService.DetectRooms(rawData)                         │
    │      │   Input:  List<RoomRawData> (do InitialData)              │
    │      │   Output: RoomDetectionResult → salva em SharedData       │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E02  │ _roomService.ClassifyAll(rooms)                           │
    │      │   Input:  SharedData["rooms"] de E01                      │
    │      │   Output: BatchClassificationResult                       │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E03  │ _equipService.GetRequiredEquipmentAll(wetAreas)           │
    │      │   Input:  SharedData["wetAreas"] de E02                   │
    │      │   Output: BatchEquipmentRequirementResult                 │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E04  │ _equipService.ProcessAll(wetAreas)                        │
    │      │   Input:  SharedData["wetAreas"] + requirements de E03    │
    │      │   Output: BatchEquipmentProcessingResult                  │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E05  │ _roomService.ValidateAll() + _equipService.ValidateAll()  │
    │      │   Input:  tudo de E01-E04                                 │
    │      │   Output: consolidação de validações                      │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E06  │ _networkService.BuildAllRisers(rooms)                     │
    │      │   Input:  SharedData["rooms"]                             │
    │      │   Output: BatchRiserGenerationResult                      │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E07  │ _networkService.BuildColdWaterNetwork(input)              │
    │      │   Input:  points AF + equipment + risers de E06           │
    │      │   Output: NetworkBuildResult (AF)                         │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E08  │ _networkService.BuildSewerNetwork(input)                  │
    │      │   Input:  points ES + equipment + risers de E06           │
    │      │   Output: NetworkBuildResult (ES)                         │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E08b │ _networkService.BuildVentilationNetwork(input)            │
    │      │   Input:  rede ES de E08 + points VE                      │
    │      │   Output: NetworkBuildResult (VE)                         │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E09  │ _networkService.OptimizeNetwork(network) × 3 sistemas     │
    │      │   Input:  redes AF/ES/VE de E07/E08/E08b                  │
    │      │   Output: NetworkOptimizationResult × 3                   │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E10  │ _revitAdapter.CreateMEPElements(exportData)               │
    │      │   Input:  NetworkExportData de E09                        │
    │      │   Output: RevitCreationResult (ElementIds criados)        │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E11  │ _sizingService.ProcessNetwork(network) × 3 sistemas       │
    │      │   Input:  redes AF/ES/VE                                  │
    │      │   Output: SizingProcessingResult × 3                      │
    ├──────┼────────────────────────────────────────────────────────────┤
    │ E12  │ _reportService.GenerateTables(sizingResults)              │
    │      │   Input:  resultados de E11                               │
    │      │   Output: ReportGenerationResult                          │
    └──────┴────────────────────────────────────────────────────────────┘
    */
}
```

---

## 10. Exemplo de Uso (UI → Orquestrador)

```csharp
public class MainCommand
{
    private readonly IOrchestratorService _orchestrator;

    public void OnStartButtonClick(List<RoomRawData> rawData)
    {
        // 1. Iniciar pipeline
        var session = _orchestrator.StartPipeline(new PipelineConfig
        {
            SessionName = "Residencial_Lote1",
            InitialData = rawData,
            AutoApprove = false
        });

        // 2. Executar até precisar de aprovação
        var batch = _orchestrator.ExecuteUntilApproval();
        // → Executa E01 (DetectRooms) → para em WaitingApproval

        ShowStepResultToUser(batch.Results.Last());
    }

    public void OnApproveButtonClick()
    {
        // 3. Usuário aprova a etapa atual
        var current = _orchestrator.GetCurrentStep();
        _orchestrator.ApproveStep(current.Id, "Ambientes OK");

        // 4. Executar próximas etapas
        var batch = _orchestrator.ExecuteUntilApproval();
        // → Executa E02 (ClassifyRooms) → para em WaitingApproval

        ShowStepResultToUser(batch.Results.Last());
    }

    public void OnRejectButtonClick(string reason)
    {
        var current = _orchestrator.GetCurrentStep();
        _orchestrator.RejectStep(current.Id, reason);

        // Retry com ajustes
        var result = _orchestrator.RetryStep(current.Id);
        ShowStepResultToUser(result);
    }

    public void OnRollbackButtonClick()
    {
        var current = _orchestrator.GetCurrentStep();
        var result = _orchestrator.RollbackStep(current.Id);
        // → Rollback em cascata de etapas dependentes

        UpdateDashboard();
    }

    public void OnStatusUpdate()
    {
        var report = _orchestrator.GetPipelineStatus();
        // report.Progress = 0.38 (38%)
        // report.CurrentStepId = "E05"
        // report.CompletedSteps = 4
        // report.PendingSteps = 9

        UpdateProgressBar(report.Progress);
        UpdateStepList(report.StepSummaries);
    }

    private void ShowStepResultToUser(StepExecutionResult result)
    {
        Console.WriteLine($"[{result.StepId}] {result.Summary}");
        Console.WriteLine($"  Status: {result.Status}");
        Console.WriteLine($"  Tempo: {result.Duration.TotalSeconds:F1}s");

        if (result.RequiresApproval)
            Console.WriteLine("  ⏸ Aguardando aprovação...");
    }
}
```

---

## 11. Resumo Visual

```
IOrchestratorService
│
├── Ciclo de Vida
│   ├── StartPipeline(config) → PipelineSession
│   ├── CancelPipeline(reason)
│   └── ResumePipeline(sessionId) → PipelineSession
│
├── Execução
│   ├── ExecuteNextStep() → StepExecutionResult
│   ├── ExecuteStep(stepId) → StepExecutionResult
│   ├── ExecuteUntilApproval() → BatchStepExecutionResult
│   └── SkipStep(stepId)
│
├── Aprovação Humana
│   ├── ApproveStep(stepId, comment)
│   └── RejectStep(stepId, reason)
│
├── Retry / Rollback
│   ├── RetryStep(stepId) → StepExecutionResult
│   ├── RollbackStep(stepId) → RollbackResult
│   └── RollbackFrom(stepId) → RollbackResult
│
├── Consulta
│   ├── GetPipelineStatus() → PipelineStatusReport
│   ├── GetCurrentStep() → PipelineStep
│   ├── GetStep(stepId) → PipelineStep
│   ├── GetAllSteps() → List<PipelineStep>
│   ├── GetExecutableSteps() → List<PipelineStep>
│   ├── GetProgress() → double (0.0 → 1.0)
│   └── GetStepOutput<T>(stepId) → T
│
├── Persistência
│   ├── SaveState()
│   └── LoadState(sessionId) → PipelineSession
│
├── Eventos
│   ├── OnStepStateChanged
│   ├── OnPipelineStateChanged
│   └── OnStepProgress
│
├── Configuração
│   ├── PipelineConfig (auto-approve, skip, max attempts)
│   └── PipelineStepFactory (13 etapas padrão E01→E12)
│
└── State Machine
    ├── StepStatus (8 estados)
    ├── PipelineStatus (6 estados)
    └── 10 transições válidas
```
