# Padrão de Observabilidade — Observer para Eventos e Logging

> Especificação completa do padrão Observer aplicado a eventos do pipeline, com EventBus centralizado, observers desacoplados para logging/progresso/debug, e isolamento de falhas, para uso no PluginCore.

---

## 1. Definição do Padrão

### 1.1 O que é Observer

O padrão **Observer** (GoF) define uma relação 1:N entre objetos: quando o **Subject** (publisher) muda de estado, todos os **Observers** (subscribers) são notificados automaticamente. Ninguém conhece ninguém — a comunicação é por contrato (interface + evento).

### 1.2 Por que usar neste sistema

O pipeline emite informações constantemente:

```
Pipeline executa E01 → precisa avisar:
  → ILogService: gravar log em disco
  → UI: atualizar barra de progresso
  → Debug: escrever no console
  → Auditoria: registrar timestamp
  → Métricas: acumular duração
```

Sem Observer, o PipelineRunner teria que conhecer e chamar cada consumidor diretamente — **acoplamento máximo**. Com Observer, o Runner emite um evento e **não sabe quantos nem quais observers existem**.

### 1.3 Benefícios

| Benefício | Detalhe |
|-----------|---------|
| **Desacoplamento** | Pipeline não conhece LogService, UI, Debug |
| **Extensibilidade** | Novo observer = 1 classe + 1 `Subscribe()` |
| **Isolamento** | Observer falhar NÃO quebra o pipeline |
| **Testabilidade** | Testar com mock observer que captura eventos |
| **Runtime** | Subscribe/unsubscribe em tempo de execução |
| **Múltiplos consumidores** | Mesmo evento → N observers simultâneos |

---

## 2. Arquitetura

```
╔═══════════════════════════════════════════════════════════════════════╗
║                          PUBLISHERS                                   ║
║                                                                       ║
║  PipelineRunner        SizingService       NetworkService             ║
║  ┌──────────┐          ┌──────────┐        ┌──────────┐             ║
║  │ Publish  │          │ Publish  │        │ Publish  │              ║
║  │ (event)  │          │ (event)  │        │ (event)  │              ║
║  └────┬─────┘          └────┬─────┘        └────┬─────┘             ║
║       │                     │                    │                    ║
║       └─────────────────────┼────────────────────┘                   ║
║                             │                                         ║
║                    ╔════════╧════════╗                                ║
║                    ║    EventBus     ║  ← BARRAMENTO CENTRAL          ║
║                    ║                 ║                                 ║
║                    ║ Publish(event)  ║                                ║
║                    ║ Subscribe<T>() ║                                 ║
║                    ║ Unsubscribe()  ║                                 ║
║                    ╚═══╤═══╤═══╤═══╝                                 ║
║                        │   │   │                                      ║
║              ┌─────────┘   │   └─────────┐                           ║
║              ▼             ▼             ▼                            ║
║   ┌────────────────┐ ┌──────────┐ ┌──────────────┐                  ║
║   │  LogObserver   │ │ Progress │ │   Debug      │                   ║
║   │                │ │ Observer │ │  Observer    │                    ║
║   │ → ILogService  │ │ → UI     │ │ → Console   │                   ║
║   └────────────────┘ └──────────┘ └──────────────┘                  ║
║                                                                       ║
║                          SUBSCRIBERS                                  ║
╚═══════════════════════════════════════════════════════════════════════╝
```

---

## 3. Eventos — Estrutura Base

```csharp
namespace HidraulicoPlugin.Core.Events
{
    /// <summary>
    /// Evento base do sistema. Todos os eventos herdam deste.
    /// Imutável após criação.
    /// </summary>
    public abstract class PipelineEvent
    {
        /// <summary>ID único do evento.</summary>
        public string EventId { get; }

        /// <summary>Tipo do evento.</summary>
        public abstract PipelineEventType EventType { get; }

        /// <summary>Timestamp UTC de criação.</summary>
        public DateTime Timestamp { get; }

        /// <summary>ID de correlação (sessão do pipeline).</summary>
        public string CorrelationId { get; set; }

        /// <summary>Origem do evento (nome do componente).</summary>
        public string Source { get; set; }

        protected PipelineEvent()
        {
            EventId = Guid.NewGuid().ToString("N")[..12];
            Timestamp = DateTime.UtcNow;
        }
    }
}
```

---

## 4. Enum de Tipos de Evento

```csharp
namespace HidraulicoPlugin.Core.Events
{
    /// <summary>
    /// Tipos de evento do sistema.
    /// </summary>
    public enum PipelineEventType
    {
        // ── Pipeline ────────────────────────────────────────────
        PipelineStarted = 100,
        PipelineCompleted = 101,
        PipelineFailed = 102,
        PipelineCancelled = 103,

        // ── Etapas ──────────────────────────────────────────────
        StepStarted = 200,
        StepCompleted = 201,
        StepFailed = 202,
        StepSkipped = 203,
        StepProgress = 204,
        StepApproved = 205,
        StepRejected = 206,
        StepRolledBack = 207,
        StepRetried = 208,

        // ── Validação ───────────────────────────────────────────
        ValidationPassed = 300,
        ValidationFailed = 301,
        ValidationWarning = 302,

        // ── Cálculo ─────────────────────────────────────────────
        CalculationStarted = 400,
        CalculationCompleted = 401,
        CalculationError = 402,

        // ── Integração ──────────────────────────────────────────
        DynamoExecutionStarted = 500,
        DynamoExecutionCompleted = 501,
        DynamoExecutionFailed = 502,

        // ── Log ─────────────────────────────────────────────────
        LogCreated = 600,

        // ── Dados ───────────────────────────────────────────────
        DataExported = 700,
        DataImported = 701
    }
}
```

---

## 5. Eventos Concretos

### 5.1 Eventos de Pipeline

```csharp
namespace HidraulicoPlugin.Core.Events
{
    public class PipelineStartedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.PipelineStarted;
        public string SessionId { get; set; }
        public int TotalSteps { get; set; }
        public string SessionName { get; set; }
    }

    public class PipelineCompletedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.PipelineCompleted;
        public string SessionId { get; set; }
        public int TotalStepsExecuted { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool IsSuccessful { get; set; }
    }

    public class PipelineFailedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.PipelineFailed;
        public string SessionId { get; set; }
        public string FailedStepId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
```

### 5.2 Eventos de Etapa

```csharp
namespace HidraulicoPlugin.Core.Events
{
    public class StepStartedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.StepStarted;
        public string StepId { get; set; }
        public string StepName { get; set; }
        public string Description { get; set; }
        public int AttemptNumber { get; set; }
        public int StepOrder { get; set; }
        public int TotalSteps { get; set; }
    }

    public class StepCompletedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.StepCompleted;
        public string StepId { get; set; }
        public string StepName { get; set; }
        public string Summary { get; set; }
        public TimeSpan Duration { get; set; }
        public bool RequiresApproval { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    public class StepFailedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.StepFailed;
        public string StepId { get; set; }
        public string StepName { get; set; }
        public string ErrorMessage { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
        public int AttemptNumber { get; set; }
        public bool CanRetry { get; set; }
    }

    public class StepProgressEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.StepProgress;
        public string StepId { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
        public string Detail { get; set; }
        public double Percent => Total > 0 ? (double)Current / Total * 100 : 0;
    }

    public class StepApprovedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.StepApproved;
        public string StepId { get; set; }
        public string Comment { get; set; }
    }

    public class StepRejectedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.StepRejected;
        public string StepId { get; set; }
        public string Reason { get; set; }
    }

    public class StepRolledBackEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.StepRolledBack;
        public string StepId { get; set; }
        public List<string> CascadedStepIds { get; set; } = new();
    }
}
```

### 5.3 Eventos de Validação

```csharp
namespace HidraulicoPlugin.Core.Events
{
    public class ValidationEvent : PipelineEvent
    {
        public override PipelineEventType EventType { get; }
        public string ComponentId { get; set; }
        public string ComponentType { get; set; }
        public string Rule { get; set; }
        public string Message { get; set; }
        public bool IsCritical { get; set; }

        public ValidationEvent(bool passed)
        {
            EventType = passed
                ? PipelineEventType.ValidationPassed
                : PipelineEventType.ValidationFailed;
        }
    }
}
```

### 5.4 Eventos de Cálculo

```csharp
namespace HidraulicoPlugin.Core.Events
{
    public class CalculationCompletedEvent : PipelineEvent
    {
        public override PipelineEventType EventType => PipelineEventType.CalculationCompleted;
        public string CalculationType { get; set; }
        public string SegmentId { get; set; }
        public Dictionary<string, double> Results { get; set; } = new();
    }
}
```

### 5.5 Eventos de Dynamo

```csharp
namespace HidraulicoPlugin.Core.Events
{
    public class DynamoExecutionEvent : PipelineEvent
    {
        public override PipelineEventType EventType { get; }
        public string ScriptId { get; set; }
        public string ExecutionId { get; set; }
        public TimeSpan? Duration { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccessful { get; set; }

        public DynamoExecutionEvent(PipelineEventType type)
        {
            EventType = type;
        }
    }
}
```

---

## 6. Observer — Interface

```csharp
namespace HidraulicoPlugin.Core.Events
{
    /// <summary>
    /// Observer que reage a eventos do pipeline.
    /// </summary>
    public interface IPipelineObserver
    {
        /// <summary>Nome do observer (para logging/debug).</summary>
        string Name { get; }

        /// <summary>
        /// Chamado quando um evento é publicado.
        /// Implementações devem ser:
        /// - Rápidas (não bloquear o pipeline)
        /// - Seguras (não lançar exceções)
        /// </summary>
        void OnEvent(PipelineEvent pipelineEvent);

        /// <summary>
        /// Filtra quais tipos de evento este observer quer receber.
        /// Retornar null = receber TODOS.
        /// </summary>
        IReadOnlyList<PipelineEventType> SubscribedEventTypes { get; }
    }
}
```

---

## 7. EventBus — Barramento Central

```csharp
namespace HidraulicoPlugin.Core.Events
{
    /// <summary>
    /// Barramento central de eventos.
    /// Recebe eventos de publishers e distribui para observers.
    /// Thread-safe. Isolamento de falhas.
    /// </summary>
    public class EventBus : IDisposable
    {
        private readonly List<IPipelineObserver> _observers = new();
        private readonly object _lock = new();
        private readonly List<PipelineEvent> _eventHistory = new();
        private readonly int _maxHistorySize;
        private bool _isEnabled = true;

        public EventBus(int maxHistorySize = 1000)
        {
            _maxHistorySize = maxHistorySize;
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra um observer para receber eventos.
        /// </summary>
        public void Subscribe(IPipelineObserver observer)
        {
            lock (_lock)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
            }
        }

        /// <summary>
        /// Remove um observer.
        /// </summary>
        public void Unsubscribe(IPipelineObserver observer)
        {
            lock (_lock)
            {
                _observers.Remove(observer);
            }
        }

        /// <summary>
        /// Remove todos os observers.
        /// </summary>
        public void UnsubscribeAll()
        {
            lock (_lock)
            {
                _observers.Clear();
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PUBLICAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Publica um evento para todos os observers interessados.
        /// Cada observer é chamado em try/catch isolado.
        /// </summary>
        public void Publish(PipelineEvent pipelineEvent)
        {
            if (!_isEnabled) return;

            // Guardar no histórico
            lock (_lock)
            {
                _eventHistory.Add(pipelineEvent);
                if (_eventHistory.Count > _maxHistorySize)
                    _eventHistory.RemoveAt(0);
            }

            // Notificar observers
            List<IPipelineObserver> snapshot;
            lock (_lock)
            {
                snapshot = new List<IPipelineObserver>(_observers);
            }

            foreach (var observer in snapshot)
            {
                try
                {
                    // Filtrar por tipo
                    if (observer.SubscribedEventTypes != null &&
                        !observer.SubscribedEventTypes.Contains(pipelineEvent.EventType))
                        continue;

                    observer.OnEvent(pipelineEvent);
                }
                catch (Exception ex)
                {
                    // ⚠️ ISOLAMENTO: observer falhar NÃO quebra o pipeline
                    System.Diagnostics.Debug.WriteLine(
                        $"[EventBus] Observer '{observer.Name}' falhou: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Atalho: publica com correlationId e source.
        /// </summary>
        public void Publish(PipelineEvent pipelineEvent,
            string correlationId, string source)
        {
            pipelineEvent.CorrelationId = correlationId;
            pipelineEvent.Source = source;
            Publish(pipelineEvent);
        }

        // ══════════════════════════════════════════════════════════
        //  CONTROLE
        // ══════════════════════════════════════════════════════════

        /// <summary>Habilita/desabilita publicação.</summary>
        public void SetEnabled(bool enabled) => _isEnabled = enabled;

        /// <summary>Retorna se está habilitado.</summary>
        public bool IsEnabled => _isEnabled;

        // ══════════════════════════════════════════════════════════
        //  CONSULTAS
        // ══════════════════════════════════════════════════════════

        /// <summary>Retorna observers registrados.</summary>
        public List<string> GetObserverNames()
        {
            lock (_lock) { return _observers.Select(o => o.Name).ToList(); }
        }

        /// <summary>Retorna contagem de observers.</summary>
        public int ObserverCount
        {
            get { lock (_lock) { return _observers.Count; } }
        }

        /// <summary>Retorna histórico de eventos.</summary>
        public List<PipelineEvent> GetEventHistory()
        {
            lock (_lock) { return new List<PipelineEvent>(_eventHistory); }
        }

        /// <summary>Retorna eventos filtrados por tipo.</summary>
        public List<T> GetEvents<T>() where T : PipelineEvent
        {
            lock (_lock)
            {
                return _eventHistory.OfType<T>().ToList();
            }
        }

        /// <summary>Retorna estatísticas do bus.</summary>
        public EventBusStats GetStats()
        {
            lock (_lock)
            {
                return new EventBusStats
                {
                    TotalEventsPublished = _eventHistory.Count,
                    ObserverCount = _observers.Count,
                    EventsByType = _eventHistory
                        .GroupBy(e => e.EventType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    IsEnabled = _isEnabled
                };
            }
        }

        public void Dispose()
        {
            UnsubscribeAll();
            _eventHistory.Clear();
        }
    }

    public class EventBusStats
    {
        public int TotalEventsPublished { get; set; }
        public int ObserverCount { get; set; }
        public Dictionary<PipelineEventType, int> EventsByType { get; set; } = new();
        public bool IsEnabled { get; set; }

        public override string ToString() =>
            $"EventBus: {TotalEventsPublished} eventos, " +
            $"{ObserverCount} observers, " +
            $"{(IsEnabled ? "ativo" : "desabilitado")}";
    }
}
```

---

## 8. Observers Concretos

### 8.1 LogObserver — Persiste em disco via ILogService

```csharp
namespace HidraulicoPlugin.Core.Events.Observers
{
    /// <summary>
    /// Persiste eventos como logs estruturados via ILogService.
    /// </summary>
    public class LogObserver : IPipelineObserver
    {
        private readonly ILogService _logService;

        public string Name => "LogObserver";

        /// <summary>Recebe TODOS os eventos.</summary>
        public IReadOnlyList<PipelineEventType> SubscribedEventTypes => null;

        public LogObserver(ILogService logService)
        {
            _logService = logService;
        }

        public void OnEvent(PipelineEvent pipelineEvent)
        {
            switch (pipelineEvent)
            {
                case PipelineStartedEvent e:
                    _logService.LogInfo(
                        $"Pipeline iniciado: {e.SessionName}",
                        new { e.SessionId, e.TotalSteps });
                    break;

                case PipelineCompletedEvent e:
                    _logService.LogInfo(
                        $"Pipeline concluído: {e.TotalStepsExecuted} etapas em {e.TotalDuration.TotalSeconds:F1}s",
                        new { e.SessionId, e.IsSuccessful });
                    break;

                case StepStartedEvent e:
                    _logService.LogStepStart(e.StepName, e.Description,
                        new { e.StepId, e.AttemptNumber, progress = $"{e.StepOrder}/{e.TotalSteps}" });
                    break;

                case StepCompletedEvent e:
                    _logService.LogStepEnd(e.StepName, true,
                        new { e.Summary, e.Metrics }, e.Duration);
                    break;

                case StepFailedEvent e:
                    _logService.LogError(
                        $"Etapa {e.StepId} falhou: {e.ErrorMessage}",
                        new { e.ExceptionType, e.AttemptNumber, e.CanRetry });
                    break;

                case StepApprovedEvent e:
                    _logService.LogInfo(
                        $"Etapa {e.StepId} aprovada",
                        new { e.Comment });
                    break;

                case StepRejectedEvent e:
                    _logService.LogWarning(
                        $"Etapa {e.StepId} rejeitada: {e.Reason}");
                    break;

                case StepRolledBackEvent e:
                    _logService.LogWarning(
                        $"Rollback: {e.StepId} + cascata: [{string.Join(", ", e.CascadedStepIds)}]");
                    break;

                case ValidationEvent e:
                    _logService.LogValidation(
                        e.ComponentId, e.Rule,
                        e.EventType == PipelineEventType.ValidationPassed,
                        e.Message);
                    break;

                case CalculationCompletedEvent e:
                    _logService.LogCalculation(
                        e.CalculationType, e.SegmentId, e.Results);
                    break;

                case DynamoExecutionEvent e:
                    _logService.LogDynamoExecution(
                        e.ScriptId, e.IsSuccessful,
                        e.Duration ?? TimeSpan.Zero,
                        e.ErrorMessage);
                    break;

                default:
                    _logService.LogDebug(
                        $"Evento: {pipelineEvent.EventType}",
                        new { pipelineEvent.EventId, pipelineEvent.Source });
                    break;
            }
        }
    }
}
```

### 8.2 ProgressObserver — Atualiza UI

```csharp
namespace HidraulicoPlugin.Core.Events.Observers
{
    /// <summary>
    /// Monitora progresso e expõe estado para a UI.
    /// Não depende de nenhum framework de UI específico.
    /// </summary>
    public class ProgressObserver : IPipelineObserver
    {
        public string Name => "ProgressObserver";

        public IReadOnlyList<PipelineEventType> SubscribedEventTypes => new[]
        {
            PipelineEventType.PipelineStarted,
            PipelineEventType.PipelineCompleted,
            PipelineEventType.PipelineFailed,
            PipelineEventType.StepStarted,
            PipelineEventType.StepCompleted,
            PipelineEventType.StepFailed,
            PipelineEventType.StepProgress,
            PipelineEventType.StepApproved,
            PipelineEventType.StepRejected
        };

        // ── Estado exposto para UI ──────────────────────────────

        public string CurrentStepId { get; private set; }
        public string CurrentStepName { get; private set; }
        public string CurrentDescription { get; private set; }
        public int CompletedSteps { get; private set; }
        public int TotalSteps { get; private set; }
        public double OverallProgress => TotalSteps > 0
            ? (double)CompletedSteps / TotalSteps : 0;
        public double StepProgress { get; private set; }
        public string StepDetail { get; private set; }
        public bool IsWaitingApproval { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsComplete { get; private set; }
        public string LastError { get; private set; }
        public TimeSpan LastStepDuration { get; private set; }
        public List<StepProgressEntry> History { get; } = new();

        // ── Callback para UI ────────────────────────────────────

        /// <summary>
        /// Callback invocado quando o estado muda.
        /// A UI registra um delegate aqui para receber updates.
        /// </summary>
        public Action<ProgressObserver> OnProgressChanged { get; set; }

        public void OnEvent(PipelineEvent pipelineEvent)
        {
            switch (pipelineEvent)
            {
                case PipelineStartedEvent e:
                    TotalSteps = e.TotalSteps;
                    CompletedSteps = 0;
                    IsRunning = true;
                    IsComplete = false;
                    break;

                case PipelineCompletedEvent e:
                    IsRunning = false;
                    IsComplete = true;
                    break;

                case PipelineFailedEvent e:
                    IsRunning = false;
                    LastError = e.ErrorMessage;
                    break;

                case StepStartedEvent e:
                    CurrentStepId = e.StepId;
                    CurrentStepName = e.StepName;
                    CurrentDescription = e.Description;
                    StepProgress = 0;
                    StepDetail = "";
                    IsWaitingApproval = false;
                    break;

                case StepCompletedEvent e:
                    StepProgress = 100;
                    LastStepDuration = e.Duration;
                    if (!e.RequiresApproval)
                        CompletedSteps++;
                    IsWaitingApproval = e.RequiresApproval;
                    History.Add(new StepProgressEntry
                    {
                        StepId = e.StepId,
                        Summary = e.Summary,
                        Duration = e.Duration,
                        IsSuccessful = true
                    });
                    break;

                case StepFailedEvent e:
                    StepProgress = 0;
                    LastError = e.ErrorMessage;
                    IsWaitingApproval = false;
                    History.Add(new StepProgressEntry
                    {
                        StepId = e.StepId,
                        Summary = e.ErrorMessage,
                        IsSuccessful = false
                    });
                    break;

                case StepProgressEvent e:
                    StepProgress = e.Percent;
                    StepDetail = e.Detail;
                    break;

                case StepApprovedEvent e:
                    CompletedSteps++;
                    IsWaitingApproval = false;
                    break;

                case StepRejectedEvent e:
                    IsWaitingApproval = false;
                    break;
            }

            // Notificar UI
            OnProgressChanged?.Invoke(this);
        }

        public string GetStatusLine() =>
            IsComplete ? $"✅ Concluído: {CompletedSteps}/{TotalSteps} etapas" :
            IsWaitingApproval ? $"⏸ Aguardando aprovação: {CurrentStepName}" :
            IsRunning ? $"🔄 {CurrentStepName} ({OverallProgress:P0}) — {StepDetail}" :
            $"⚪ Não iniciado";
    }

    public class StepProgressEntry
    {
        public string StepId { get; set; }
        public string Summary { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
```

### 8.3 DebugObserver — Console/Diagnóstico

```csharp
namespace HidraulicoPlugin.Core.Events.Observers
{
    /// <summary>
    /// Escreve TODOS os eventos no console para debug.
    /// Usar apenas em desenvolvimento.
    /// </summary>
    public class DebugObserver : IPipelineObserver
    {
        public string Name => "DebugObserver";
        public IReadOnlyList<PipelineEventType> SubscribedEventTypes => null;

        private readonly bool _verbose;

        public DebugObserver(bool verbose = false)
        {
            _verbose = verbose;
        }

        public void OnEvent(PipelineEvent pipelineEvent)
        {
            var icon = pipelineEvent.EventType switch
            {
                PipelineEventType.PipelineStarted => "🚀",
                PipelineEventType.PipelineCompleted => "🏁",
                PipelineEventType.PipelineFailed => "💥",
                PipelineEventType.StepStarted => "▶️",
                PipelineEventType.StepCompleted => "✅",
                PipelineEventType.StepFailed => "❌",
                PipelineEventType.StepProgress => "📊",
                PipelineEventType.StepApproved => "👍",
                PipelineEventType.StepRejected => "👎",
                PipelineEventType.StepRolledBack => "⏪",
                PipelineEventType.ValidationPassed => "✔️",
                PipelineEventType.ValidationFailed => "⚠️",
                PipelineEventType.CalculationCompleted => "🔢",
                PipelineEventType.DynamoExecutionStarted => "⚙️",
                _ => "📌"
            };

            var time = pipelineEvent.Timestamp.ToString("HH:mm:ss.fff");
            var msg = FormatEvent(pipelineEvent);

            System.Diagnostics.Debug.WriteLine(
                $"{icon} [{time}] [{pipelineEvent.EventType}] {msg}");

            if (_verbose)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"   ID: {pipelineEvent.EventId} | " +
                    $"Corr: {pipelineEvent.CorrelationId} | " +
                    $"Src: {pipelineEvent.Source}");
            }
        }

        private string FormatEvent(PipelineEvent e) => e switch
        {
            StepStartedEvent s => $"{s.StepId}: {s.Description} (tentativa {s.AttemptNumber})",
            StepCompletedEvent s => $"{s.StepId}: {s.Summary} ({s.Duration.TotalSeconds:F1}s)",
            StepFailedEvent s => $"{s.StepId}: ERRO — {s.ErrorMessage}",
            StepProgressEvent s => $"{s.StepId}: {s.Current}/{s.Total} — {s.Detail}",
            PipelineStartedEvent s => $"Sessão {s.SessionId}: {s.TotalSteps} etapas",
            PipelineCompletedEvent s => $"Concluído em {s.TotalDuration.TotalSeconds:F1}s",
            _ => e.EventType.ToString()
        };
    }
}
```

### 8.4 MetricsObserver — Acumula estatísticas

```csharp
namespace HidraulicoPlugin.Core.Events.Observers
{
    /// <summary>
    /// Acumula métricas de execução para análise e dashboard.
    /// </summary>
    public class MetricsObserver : IPipelineObserver
    {
        public string Name => "MetricsObserver";

        public IReadOnlyList<PipelineEventType> SubscribedEventTypes => new[]
        {
            PipelineEventType.StepCompleted,
            PipelineEventType.StepFailed,
            PipelineEventType.CalculationCompleted,
            PipelineEventType.ValidationPassed,
            PipelineEventType.ValidationFailed
        };

        // ── Métricas acumuladas ─────────────────────────────────

        public int TotalStepExecutions { get; private set; }
        public int TotalStepFailures { get; private set; }
        public int TotalValidations { get; private set; }
        public int TotalValidationFailures { get; private set; }
        public int TotalCalculations { get; private set; }
        public TimeSpan TotalExecutionTime { get; private set; }
        public Dictionary<string, TimeSpan> StepDurations { get; } = new();
        public Dictionary<string, int> StepRetries { get; } = new();

        public double SuccessRate => TotalStepExecutions > 0
            ? (double)(TotalStepExecutions - TotalStepFailures) / TotalStepExecutions
            : 0;

        public void OnEvent(PipelineEvent pipelineEvent)
        {
            switch (pipelineEvent)
            {
                case StepCompletedEvent e:
                    TotalStepExecutions++;
                    TotalExecutionTime += e.Duration;
                    StepDurations[e.StepId] = e.Duration;
                    break;

                case StepFailedEvent e:
                    TotalStepExecutions++;
                    TotalStepFailures++;
                    StepRetries[e.StepId] = e.AttemptNumber;
                    break;

                case ValidationEvent e:
                    TotalValidations++;
                    if (e.EventType == PipelineEventType.ValidationFailed)
                        TotalValidationFailures++;
                    break;

                case CalculationCompletedEvent:
                    TotalCalculations++;
                    break;
            }
        }

        public MetricsReport GetReport() => new()
        {
            TotalSteps = TotalStepExecutions,
            Failures = TotalStepFailures,
            SuccessRate = SuccessRate,
            TotalTime = TotalExecutionTime,
            SlowestStep = StepDurations.OrderByDescending(kv => kv.Value)
                .FirstOrDefault().Key,
            TotalValidations = TotalValidations,
            ValidationFailures = TotalValidationFailures,
            TotalCalculations = TotalCalculations
        };
    }

    public class MetricsReport
    {
        public int TotalSteps { get; set; }
        public int Failures { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan TotalTime { get; set; }
        public string SlowestStep { get; set; }
        public int TotalValidations { get; set; }
        public int ValidationFailures { get; set; }
        public int TotalCalculations { get; set; }

        public override string ToString() =>
            $"Métricas: {TotalSteps} etapas ({SuccessRate:P0} sucesso), " +
            $"{TotalTime.TotalSeconds:F1}s total, " +
            $"mais lenta: {SlowestStep}, " +
            $"{TotalValidations} validações ({ValidationFailures} falhas), " +
            $"{TotalCalculations} cálculos";
    }
}
```

---

## 9. Integração com PipelineRunner

```csharp
namespace HidraulicoPlugin.Core.Pipeline
{
    /// <summary>
    /// PipelineRunner com suporte a EventBus.
    /// Emite eventos em cada transição de estado.
    /// </summary>
    public class PipelineRunner
    {
        private readonly List<IProcessingStep> _steps;
        private readonly EventBus _eventBus;
        private PipelineContext _context;

        public PipelineRunner(List<IProcessingStep> steps, EventBus eventBus)
        {
            _steps = steps.OrderBy(s => s.Order).ToList();
            _eventBus = eventBus;
        }

        public PipelineRunnerStatus Initialize(PipelineContext context)
        {
            _context = context;

            // Inicializar etapas...
            foreach (var step in _steps)
                _context.StepStates[step.StepId] = new StepState
                    { StepId = step.StepId, Status = StepStatus.Pending };

            _context.SessionId = $"sess_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            // 🔔 EVENTO: Pipeline iniciado
            _eventBus.Publish(new PipelineStartedEvent
            {
                SessionId = _context.SessionId,
                TotalSteps = _steps.Count,
                SessionName = "HydraulicPipeline"
            }, _context.CorrelationId, "PipelineRunner");

            return GetStatus();
        }

        private StepResult ExecuteStep(IProcessingStep step)
        {
            var state = _context.StepStates[step.StepId];
            state.Status = StepStatus.Running;
            state.AttemptCount++;

            // 🔔 EVENTO: Etapa iniciou
            _eventBus.Publish(new StepStartedEvent
            {
                StepId = step.StepId,
                StepName = step.Name,
                Description = step.Description,
                AttemptNumber = state.AttemptCount,
                StepOrder = step.Order,
                TotalSteps = _steps.Count
            }, _context.CorrelationId, step.Name);

            // Executar
            var result = step.Execute(_context);

            if (result.IsSuccessful)
            {
                state.Status = step.RequiresApproval
                    ? StepStatus.WaitingApproval
                    : StepStatus.Completed;

                // 🔔 EVENTO: Etapa concluiu
                _eventBus.Publish(new StepCompletedEvent
                {
                    StepId = step.StepId,
                    StepName = step.Name,
                    Summary = result.Summary,
                    Duration = result.Duration,
                    RequiresApproval = step.RequiresApproval,
                    Metrics = result.Metrics
                }, _context.CorrelationId, step.Name);
            }
            else
            {
                state.Status = StepStatus.Failed;
                state.ErrorMessage = result.ErrorMessage;

                // 🔔 EVENTO: Etapa falhou
                _eventBus.Publish(new StepFailedEvent
                {
                    StepId = step.StepId,
                    StepName = step.Name,
                    ErrorMessage = result.ErrorMessage,
                    ExceptionType = result.Exception?.GetType().Name,
                    StackTrace = result.Exception?.StackTrace,
                    AttemptNumber = state.AttemptCount,
                    CanRetry = state.AttemptCount < step.MaxAttempts
                }, _context.CorrelationId, step.Name);
            }

            return result;
        }

        public void Approve(string stepId, string comment = null)
        {
            var state = _context.StepStates[stepId];
            state.Status = StepStatus.Completed;
            state.UserApproved = true;

            // 🔔 EVENTO: Aprovado
            _eventBus.Publish(new StepApprovedEvent
            {
                StepId = stepId,
                Comment = comment
            }, _context.CorrelationId, "User");
        }

        public void Reject(string stepId, string reason)
        {
            var state = _context.StepStates[stepId];
            state.Status = StepStatus.Rejected;

            // 🔔 EVENTO: Rejeitado
            _eventBus.Publish(new StepRejectedEvent
            {
                StepId = stepId,
                Reason = reason
            }, _context.CorrelationId, "User");
        }

        public List<string> Rollback(string stepId)
        {
            var rolledBack = new List<string> { stepId };

            // Rollback lógica...
            foreach (var id in rolledBack)
                _context.StepStates[id].Status = StepStatus.RolledBack;

            // 🔔 EVENTO: Rollback
            _eventBus.Publish(new StepRolledBackEvent
            {
                StepId = stepId,
                CascadedStepIds = rolledBack.Skip(1).ToList()
            }, _context.CorrelationId, "PipelineRunner");

            return rolledBack;
        }

        // ... demais métodos (ExecuteNext, etc.) omitidos por brevidade
        public PipelineRunnerStatus GetStatus() => new();
    }
}
```

---

## 10. Emissão de Progresso dentro de uma Etapa

```csharp
namespace HidraulicoPlugin.Core.Pipeline.Steps
{
    /// <summary>
    /// Etapa que emite progresso granular via EventBus.
    /// </summary>
    public class BuildColdWaterStep : ProcessingStepBase
    {
        private readonly INetworkService _networkService;
        private readonly EventBus _eventBus;

        public BuildColdWaterStep(INetworkService networkService, EventBus eventBus)
        {
            _networkService = networkService;
            _eventBus = eventBus;
        }

        public override string StepId => "E07";
        public override string Name => "BuildColdWater";
        public override string Description => "Geração da rede de água fria";
        public override int Order => 7;

        protected override StepResult ExecuteCore(PipelineContext context)
        {
            var rooms = context.WetAreas;
            int total = rooms.Count;

            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];

                // 🔔 Progresso: item por item
                _eventBus.Publish(new StepProgressEvent
                {
                    StepId = StepId,
                    Current = i + 1,
                    Total = total,
                    Detail = $"Processando: {room.Name}"
                }, context.CorrelationId, Name);

                // ... lógica de rede para o ambiente
            }

            context.ColdWaterNetwork = /* resultado */;

            return StepResult.Success(StepId,
                $"Rede AF: {total} ambientes processados");
        }
    }
}
```

---

## 11. Setup e Exemplo de Uso

```csharp
public class Application
{
    public void Start(List<RoomRawData> rawData)
    {
        // 1. Criar EventBus
        var eventBus = new EventBus(maxHistorySize: 5000);

        // 2. Criar serviços
        var logService = new FileLogService("logs/");
        var roomService = new RoomService();
        // ...

        // 3. Criar e registrar observers
        var logObserver = new LogObserver(logService);
        var progressObserver = new ProgressObserver();
        var debugObserver = new DebugObserver(verbose: true);
        var metricsObserver = new MetricsObserver();

        eventBus.Subscribe(logObserver);
        eventBus.Subscribe(progressObserver);
        eventBus.Subscribe(debugObserver);
        eventBus.Subscribe(metricsObserver);

        // 4. Bind da UI ao ProgressObserver
        progressObserver.OnProgressChanged = (obs) =>
        {
            Console.Clear();
            Console.WriteLine(obs.GetStatusLine());
            Console.WriteLine($"Progresso geral: {obs.OverallProgress:P0}");
            Console.WriteLine($"Etapa: {obs.StepProgress:F0}% — {obs.StepDetail}");
        };

        // 5. Criar pipeline com EventBus
        var steps = new List<IProcessingStep>
        {
            new DetectRoomsStep(roomService),
            new ClassifyRoomsStep(roomService),
            // ... demais etapas
        };

        var runner = new PipelineRunner(steps, eventBus);

        // 6. Executar
        var context = new PipelineContext { RawRooms = rawData };
        runner.Initialize(context);

        // Console do DebugObserver:
        // 🚀 [14:32:01.123] [PipelineStarted] Sessão sess_20260319_143201: 12 etapas
        // ▶️ [14:32:01.125] [StepStarted] E01: Detecção de ambientes (tentativa 1)
        // 📊 [14:32:01.200] [StepProgress] E01: 3/12 — Processando: Banheiro Suíte
        // ✅ [14:32:01.450] [StepCompleted] E01: 12 ambientes detectados (0.3s)

        // 7. Métricas finais
        var report = metricsObserver.GetReport();
        Console.WriteLine(report);
        // → "Métricas: 12 etapas (100% sucesso), 4.2s total,
        //    mais lenta: E07, 45 validações (2 falhas), 38 cálculos"

        // 8. Histórico de eventos
        var history = eventBus.GetEvents<StepCompletedEvent>();
        // → lista de todas as etapas concluídas com métricas

        // 9. Cleanup
        eventBus.Dispose();
    }
}
```

---

## 12. Extensibilidade

### Para adicionar um novo Observer:

```csharp
// 1. Criar a classe (ex: enviar notificações por email)
public class EmailNotificationObserver : IPipelineObserver
{
    public string Name => "EmailObserver";

    // Só recebe falhas críticas
    public IReadOnlyList<PipelineEventType> SubscribedEventTypes => new[]
    {
        PipelineEventType.PipelineFailed,
        PipelineEventType.StepFailed
    };

    public void OnEvent(PipelineEvent pipelineEvent)
    {
        if (pipelineEvent is StepFailedEvent e && !e.CanRetry)
            SendEmail($"Pipeline falhou em {e.StepId}: {e.ErrorMessage}");
    }
}

// 2. Registrar (1 linha)
eventBus.Subscribe(new EmailNotificationObserver());
```

### Para adicionar um novo tipo de evento:

```csharp
// 1. Criar o evento
public class CostEstimateEvent : PipelineEvent
{
    public override PipelineEventType EventType => PipelineEventType.DataExported;
    public double EstimatedCostBRL { get; set; }
    public int TotalItems { get; set; }
}

// 2. Publicar onde necessário
_eventBus.Publish(new CostEstimateEvent
{
    EstimatedCostBRL = bom.TotalCost,
    TotalItems = bom.TotalItems
});

// 3. Observers existentes tratam automaticamente (default case no LogObserver)
```

---

## 13. Resumo Visual

```
Observer Pattern — Eventos e Logging
│
├── PipelineEvent (Classe base)
│   ├── EventId, Timestamp, EventType
│   ├── CorrelationId, Source
│   └── 15 eventos concretos
│
├── PipelineEventType (Enum)
│   ├── Pipeline: Started, Completed, Failed, Cancelled
│   ├── Step: Started, Completed, Failed, Skipped, Progress, Approved, Rejected, RolledBack
│   ├── Validation: Passed, Failed, Warning
│   ├── Calculation: Started, Completed, Error
│   ├── Dynamo: Started, Completed, Failed
│   └── Data: Exported, Imported
│
├── IPipelineObserver (Interface)
│   ├── Name
│   ├── OnEvent(PipelineEvent)
│   └── SubscribedEventTypes (filtro, null = todos)
│
├── EventBus (Barramento central)
│   ├── Subscribe(observer)
│   ├── Unsubscribe(observer)
│   ├── Publish(event) — try/catch por observer
│   ├── GetEventHistory() → List<PipelineEvent>
│   ├── GetEvents<T>() → filtro por tipo
│   ├── GetStats() → EventBusStats
│   └── Thread-safe (lock)
│
├── Observers Concretos (4)
│   ├── LogObserver → ILogService (persiste em disco)
│   ├── ProgressObserver → UI (estado + callback)
│   ├── DebugObserver → Console (com ícones)
│   └── MetricsObserver → Estatísticas acumuladas
│
├── Isolamento de falhas
│   └── Observer lançar exceção → catch + Debug.WriteLine
│       Pipeline NUNCA para por causa de um observer
│
└── Extensibilidade
    ├── Novo observer = 1 classe + 1 Subscribe()
    └── Novo evento = 1 classe herda PipelineEvent
```
