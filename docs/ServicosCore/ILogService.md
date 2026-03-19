# Serviço de Infraestrutura — ILogService

> Especificação completa da interface de logging estruturado do PluginCore, com correlação por execução, rastreabilidade por etapa do pipeline, e persistência em JSON, totalmente agnóstica ao Revit.

---

## 1. Definição da Interface

### 1.1 O que é ILogService

`ILogService` é a **interface de observabilidade** do sistema. Todo componente do PluginCore registra eventos através dela — nunca via `Console.WriteLine` ou `Debug.Print`. Ela garante que cada log tenha contexto suficiente para reconstruir a execução completa do pipeline.

### 1.2 Papel no sistema

```
╔══════════════════════════════════════════════════════════╗
║                    TODOS OS SERVIÇOS                     ║
║  IRoomService  IEquipmentService  INetworkService  ...   ║
╚═══════════════════════╤══════════════════════════════════╝
                        │
                        ▼
              ╔═════════════════╗
              ║   ILogService   ║  ← ESTA INTERFACE
              ╠═════════════════╣
              ║ LogInfo()       ║
              ║ LogWarning()    ║
              ║ LogError()      ║
              ║ LogStepStart()  ║
              ║ LogStepEnd()    ║
              ║ LogValidation() ║
              ╚═══════╤═════════╝
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
     logs/*.json   Console    UI Panel
     (arquivo)     (debug)    (feedback)
```

### 1.3 Por que é independente do Revit

```
O QUE NÃO FAZEMOS:
  ❌ TaskDialog.Show()            → depende do Revit UI
  ❌ Application.WriteJournalComment() → depende do Revit API
  ❌ System.Diagnostics.Trace    → sem estrutura, sem contexto

O QUE FAZEMOS:
  ✅ ILogService.LogInfo()        → agnóstico, testável
  ✅ JSON estruturado             → analisável por qualquer ferramenta
  ✅ Injeção de dependência       → mock em testes, arquivo em produção

A IMPLEMENTAÇÃO (não a interface) decide para onde vai:
  - FileLogService      → escreve em logs/*.json
  - ConsoleLogService   → escreve no console (debug)
  - RevitLogService     → escreve no Revit journal (produção)
  - CompositeLogService → escreve em múltiplos destinos
```

---

## 2. Princípios de Logging

| Princípio | Regra |
|-----------|-------|
| **Estruturado** | Sempre JSON. Nunca texto livre sem contexto |
| **Contextualizado** | Todo log tem: timestamp, level, step, correlationId |
| **Correlacionado** | Uma execução do pipeline = 1 correlationId |
| **Leve** | Sem overhead perceptível. Serialização lazy |
| **Não-redundante** | 1 evento = 1 log. Não duplicar entre chamadas |
| **Seguro** | Nunca lançar exceção por falha de logging |
| **Auditável** | Logs de validação são permanentes (não podem ser apagados) |
| **Privado** | Não logar dados pessoais ou caminhos sensíveis |

---

## 3. Níveis de Log

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Nível de severidade do log.
    /// Ordenado por severidade crescente.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Informação detalhada para debug.
        /// Desativado em produção.
        /// Ex: "Convertendo Room 'Banheiro' (ElementId: 123456)"
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Evento normal do fluxo.
        /// Ex: "Etapa E01 concluída: 12 ambientes detectados"
        /// </summary>
        Info = 1,

        /// <summary>
        /// Situação inesperada mas não impeditiva.
        /// Ex: "Ambiente 'Sala' sem classificação — marcado como Unclassified"
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Erro recuperável.
        /// Ex: "Falha ao posicionar lavatório no Banheiro 02 — posição ajustada"
        /// </summary>
        Error = 3,

        /// <summary>
        /// Falha irrecuperável que interrompe a execução.
        /// Ex: "Nenhum ambiente detectado — pipeline abortado"
        /// </summary>
        Critical = 4
    }
}
```

---

## 4. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Interface de logging estruturado do sistema.
    /// Usada por todos os serviços do Core.
    /// Implementação decide o destino (arquivo, console, UI, Revit journal).
    /// </summary>
    public interface ILogService
    {
        // ══════════════════════════════════════════════════════════
        //  LOGS GERAIS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra mensagem de debug (desativada em produção).
        /// </summary>
        /// <param name="message">Mensagem descritiva.</param>
        /// <param name="data">Dados adicionais (serializados como JSON).</param>
        void LogDebug(string message, object data = null);

        /// <summary>
        /// Registra evento informativo do fluxo normal.
        /// </summary>
        void LogInfo(string message, object data = null);

        /// <summary>
        /// Registra situação inesperada mas não impeditiva.
        /// </summary>
        void LogWarning(string message, object data = null);

        /// <summary>
        /// Registra erro recuperável.
        /// </summary>
        /// <param name="message">Descrição do erro.</param>
        /// <param name="exception">Exceção capturada (opcional).</param>
        /// <param name="data">Dados adicionais.</param>
        void LogError(string message, Exception exception = null, object data = null);

        /// <summary>
        /// Registra falha irrecuperável.
        /// </summary>
        void LogCritical(string message, Exception exception = null, object data = null);

        /// <summary>
        /// Registra log genérico com nível explícito.
        /// </summary>
        void Log(LogLevel level, string message, object data = null,
            Exception exception = null);

        // ══════════════════════════════════════════════════════════
        //  LOGS DE PIPELINE (POR ETAPA)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra início de uma etapa do pipeline.
        /// Abre um escopo lógico para correlação.
        /// </summary>
        /// <param name="step">Identificador da etapa (ex: "E01_DetectRooms").</param>
        /// <param name="description">Descrição humana (ex: "Detecção de ambientes").</param>
        /// <param name="inputSummary">Resumo dos dados de entrada.</param>
        void LogStepStart(string step, string description, object inputSummary = null);

        /// <summary>
        /// Registra fim de uma etapa com resultado.
        /// Fecha o escopo lógico.
        /// </summary>
        /// <param name="step">Identificador da etapa.</param>
        /// <param name="success">Se a etapa foi bem-sucedida.</param>
        /// <param name="outputSummary">Resumo dos dados de saída.</param>
        /// <param name="duration">Tempo de execução.</param>
        void LogStepEnd(string step, bool success, object outputSummary = null,
            TimeSpan? duration = null);

        /// <summary>
        /// Registra progresso intermediário de uma etapa.
        /// Ex: "Processando ambiente 5 de 12..."
        /// </summary>
        void LogStepProgress(string step, int current, int total,
            string detail = null);

        // ══════════════════════════════════════════════════════════
        //  LOGS DE VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra resultado de validação de um componente.
        /// </summary>
        /// <param name="componentType">Tipo do componente (Room, Equipment, Segment).</param>
        /// <param name="componentId">ID do componente.</param>
        /// <param name="isValid">Se passou na validação.</param>
        /// <param name="issues">Lista de problemas encontrados.</param>
        void LogValidation(string componentType, string componentId,
            bool isValid, List<LogValidationIssue> issues = null);

        /// <summary>
        /// Registra resultado de validação em lote.
        /// </summary>
        void LogBatchValidation(string componentType,
            int totalChecked, int totalPassed, int totalFailed,
            int criticalCount = 0);

        // ══════════════════════════════════════════════════════════
        //  LOGS DE CÁLCULO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra resultado de um cálculo hidráulico.
        /// Usado para auditoria de dimensionamento.
        /// </summary>
        /// <param name="calculationType">Tipo (ProbableFlow, HazenWilliams, Manning).</param>
        /// <param name="inputs">Dados de entrada do cálculo.</param>
        /// <param name="outputs">Resultados do cálculo.</param>
        /// <param name="isCompliant">Se atende à norma.</param>
        void LogCalculation(string calculationType, object inputs,
            object outputs, bool isCompliant);

        // ══════════════════════════════════════════════════════════
        //  LOGS DE INTEGRAÇÃO EXTERNA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra interação com Dynamo.
        /// </summary>
        void LogDynamoExecution(string scriptName, bool success,
            TimeSpan duration, string errorMessage = null);

        /// <summary>
        /// Registra interação com unMEP.
        /// </summary>
        void LogUnMepExecution(string operationType, bool success,
            TimeSpan duration, string errorMessage = null);

        // ══════════════════════════════════════════════════════════
        //  CORRELAÇÃO E ESCOPO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Inicia uma nova execução do pipeline.
        /// Gera um novo CorrelationId e marca o início da sessão.
        /// </summary>
        /// <param name="sessionName">Nome da sessão (ex: "FullPipeline_20260118").</param>
        /// <returns>CorrelationId gerado.</returns>
        string StartSession(string sessionName);

        /// <summary>
        /// Finaliza a sessão atual.
        /// </summary>
        /// <param name="success">Se a sessão terminou com sucesso.</param>
        void EndSession(bool success);

        /// <summary>
        /// Retorna o CorrelationId da sessão atual.
        /// </summary>
        string GetCurrentCorrelationId();

        // ══════════════════════════════════════════════════════════
        //  CONSULTAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o nível mínimo de log ativo.
        /// Logs abaixo deste nível são ignorados.
        /// </summary>
        LogLevel GetMinLevel();

        /// <summary>
        /// Define o nível mínimo de log.
        /// </summary>
        void SetMinLevel(LogLevel level);

        /// <summary>
        /// Verifica se um nível está habilitado (para evitar serialização desnecessária).
        /// </summary>
        bool IsEnabled(LogLevel level);
    }
}
```

---

## 5. DTOs e Estruturas

### 5.1 LogEntry (Estrutura do log)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Entrada de log estruturada.
    /// Formato de persistência: 1 LogEntry = 1 linha JSON.
    /// </summary>
    public class LogEntry
    {
        // ── Campos obrigatórios ─────────────────────────────────

        /// <summary>Timestamp em UTC ISO 8601.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Nível de severidade.</summary>
        public LogLevel Level { get; set; }

        /// <summary>Mensagem descritiva.</summary>
        public string Message { get; set; }

        // ── Contexto ────────────────────────────────────────────

        /// <summary>
        /// ID de correlação. Agrupa todos os logs de uma sessão.
        /// Formato: "sess_20260118_143022_a1b2c3"
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Etapa do pipeline ativa.
        /// Ex: "E01_DetectRooms", "E04_InsertEquipment"
        /// </summary>
        public string Step { get; set; }

        /// <summary>
        /// Serviço que gerou o log.
        /// Ex: "IRoomService", "ISizingService"
        /// </summary>
        public string Source { get; set; }

        // ── Dados adicionais ────────────────────────────────────

        /// <summary>
        /// Dados arbitrários serializados como JSON.
        /// Ex: { "roomCount": 12, "wetAreas": 5 }
        /// </summary>
        public object Data { get; set; }

        // ── Erro (quando aplicável) ─────────────────────────────

        /// <summary>Tipo da exceção.</summary>
        public string ExceptionType { get; set; }

        /// <summary>Mensagem da exceção.</summary>
        public string ExceptionMessage { get; set; }

        /// <summary>Stack trace (apenas Error/Critical).</summary>
        public string StackTrace { get; set; }

        // ── Métricas (quando aplicável) ─────────────────────────

        /// <summary>Duração da operação em ms.</summary>
        public double? DurationMs { get; set; }

        /// <summary>Uso de memória em MB (opcional).</summary>
        public double? MemoryMb { get; set; }
    }
}
```

### 5.2 LogValidationIssue

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Problema de validação para logging.
    /// Versão simplificada dos issues de cada serviço.
    /// </summary>
    public class LogValidationIssue
    {
        public string Code { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }

        public LogValidationIssue() { }

        public LogValidationIssue(string code, string level, string message)
        {
            Code = code;
            Level = level;
            Message = message;
        }
    }
}
```

### 5.3 LogSessionInfo

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Informações de uma sessão de execução.
    /// </summary>
    public class LogSessionInfo
    {
        public string CorrelationId { get; set; }
        public string SessionName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool? Success { get; set; }
        public TimeSpan Duration => (EndedAt ?? DateTime.UtcNow) - StartedAt;
        public List<string> StepsExecuted { get; set; } = new();
        public int TotalLogs { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
    }
}
```

---

## 6. Correlação de Execução

### 6.1 CorrelationId

```
Formato: sess_{data}_{hora}_{hash}
Exemplo: sess_20260118_143022_a7f3b2

Geração: StartSession("FullPipeline") → "sess_20260118_143022_a7f3b2"

Uso:
  TODOS os logs dentro dessa sessão carregam o mesmo CorrelationId.
  Permite filtrar: "mostre-me TUDO que aconteceu nessa execução".
```

### 6.2 Hierarquia de escopo

```
Session: sess_20260118_143022_a7f3b2
│
├── Step: E01_DetectRooms
│   ├── LogInfo("Iniciando detecção...")
│   ├── LogDebug("Convertendo Room 1 de 12")
│   ├── LogDebug("Convertendo Room 2 de 12")
│   └── LogInfo("Detecção concluída: 12 ambientes")
│
├── Step: E02_ClassifyRooms
│   ├── LogInfo("Classificando 12 ambientes...")
│   ├── LogWarning("Sala TV: confiança baixa (0.45)")
│   └── LogInfo("Classificação: 5 molhadas, 7 secas")
│
├── Step: E04_InsertEquipment
│   ├── LogInfo("Gerando equipamentos...")
│   ├── LogCalculation("EquipmentPositioning", ...)
│   └── LogValidation("Equipment", "eq_001", true, [])
│
└── EndSession(success: true)
```

---

## 7. Estratégia de Persistência

### 7.1 Arquivo local

```
Estrutura de pastas:
  %AppData%/HidraulicoPlugin/logs/
  ├── 2026-01-18.jsonl          ← logs do dia
  ├── 2026-01-17.jsonl
  ├── 2026-01-16.jsonl
  └── sessions/
      ├── sess_20260118_143022.json  ← resumo da sessão
      └── sess_20260117_091500.json

Formato: JSON Lines (1 LogEntry por linha)
Tamanho médio: ~200 bytes por log
Estimativa por execução: ~500 logs ≈ 100 KB
```

### 7.2 Rotação de logs

```csharp
namespace HidraulicoPlugin.Core.Configuration
{
    /// <summary>
    /// Configuração de logging.
    /// </summary>
    public class LogConfiguration
    {
        /// <summary>Nível mínimo de log.</summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Info;

        /// <summary>Diretório base para logs.</summary>
        public string LogDirectory { get; set; } = "%AppData%/HidraulicoPlugin/logs";

        /// <summary>Dias para reter logs antigos. 0 = sem rotação.</summary>
        public int RetentionDays { get; set; } = 30;

        /// <summary>Tamanho máximo por arquivo em MB.</summary>
        public int MaxFileSizeMb { get; set; } = 10;

        /// <summary>Se deve incluir dados de debug em produção.</summary>
        public bool IncludeDebugData { get; set; } = false;

        /// <summary>Se deve logar stack traces completos.</summary>
        public bool IncludeStackTraces { get; set; } = true;

        /// <summary>Se deve salvar resumos de sessão.</summary>
        public bool SaveSessionSummaries { get; set; } = true;

        /// <summary>Se deve escrever no console (para debug).</summary>
        public bool WriteToConsole { get; set; } = false;
    }
}
```

---

## 8. Etapas do Pipeline (constantes)

```csharp
namespace HidraulicoPlugin.Core.Constants
{
    /// <summary>
    /// Identificadores das etapas do pipeline.
    /// Usados em LogStepStart/LogStepEnd para correlação.
    /// </summary>
    public static class PipelineSteps
    {
        public const string E01_DetectRooms = "E01_DetectRooms";
        public const string E02_ClassifyRooms = "E02_ClassifyRooms";
        public const string E03_IdentifyEquipment = "E03_IdentifyEquipment";
        public const string E04_InsertEquipment = "E04_InsertEquipment";
        public const string E05_ValidateModel = "E05_ValidateModel";
        public const string E06_BuildRisers = "E06_BuildRisers";
        public const string E07_BuildColdWater = "E07_BuildColdWater";
        public const string E08_BuildSewer = "E08_BuildSewer";
        public const string E08b_BuildVentilation = "E08b_BuildVentilation";
        public const string E09_OptimizeNetworks = "E09_OptimizeNetworks";
        public const string E10_ExportToRevit = "E10_ExportToRevit";
        public const string E11_SizeNetworks = "E11_SizeNetworks";
        public const string E12_GenerateTables = "E12_GenerateTables";
    }
}
```

---

## 9. Exemplo de Uso (nos serviços)

```csharp
public class RoomService : IRoomService
{
    private readonly ILogService _log;

    public RoomService(ILogService log)
    {
        _log = log;
    }

    public RoomDetectionResult DetectRooms(List<RoomRawData> rawRooms)
    {
        var stopwatch = Stopwatch.StartNew();
        _log.LogStepStart(PipelineSteps.E01_DetectRooms,
            "Detecção de ambientes",
            new { totalInput = rawRooms.Count });

        var result = new RoomDetectionResult { TotalInput = rawRooms.Count };

        for (int i = 0; i < rawRooms.Count; i++)
        {
            _log.LogStepProgress(PipelineSteps.E01_DetectRooms,
                i + 1, rawRooms.Count, rawRooms[i].Name);

            try
            {
                var room = ConvertToRoomInfo(rawRooms[i]);
                result.Rooms.Add(room);

                _log.LogDebug($"Room convertido: {room.Name}",
                    new { id = room.Id, area = room.AreaSqM });
            }
            catch (Exception ex)
            {
                _log.LogError($"Falha ao converter Room '{rawRooms[i].Name}'",
                    ex, new { revitId = rawRooms[i].RevitId });

                result.Errors.Add(new RoomConversionError
                {
                    RevitId = rawRooms[i].RevitId,
                    RoomName = rawRooms[i].Name,
                    ErrorMessage = ex.Message
                });
            }
        }

        stopwatch.Stop();
        result.ExecutionTime = stopwatch.Elapsed;

        _log.LogStepEnd(PipelineSteps.E01_DetectRooms,
            result.IsSuccessful,
            new
            {
                converted = result.TotalConverted,
                errors = result.TotalErrors,
                durationMs = result.ExecutionTime.TotalMilliseconds
            },
            result.ExecutionTime);

        return result;
    }
}
```

### 9.1 Uso no Orchestrator

```csharp
public class PipelineOrchestrator
{
    private readonly ILogService _log;
    private readonly IRoomService _roomService;
    private readonly IEquipmentService _equipmentService;

    public async Task Execute(List<RoomRawData> rawData)
    {
        // Iniciar sessão
        string correlationId = _log.StartSession("FullPipeline");
        _log.LogInfo("Pipeline iniciado", new { correlationId, inputCount = rawData.Count });

        try
        {
            // E01: Detecção
            var detection = _roomService.DetectRooms(rawData);

            // E02: Classificação
            var classification = _roomService.ClassifyAll(detection.Rooms);

            // Validação com log
            var validation = _roomService.ValidateAll(detection.Rooms);
            _log.LogBatchValidation("Room",
                validation.Results.Count,
                validation.TotalValid,
                validation.TotalInvalid,
                validation.TotalCritical);

            if (validation.TotalCritical > 0)
            {
                _log.LogCritical("Pipeline abortado: validação de ambientes falhou",
                    data: new { critical = validation.TotalCritical });
                _log.EndSession(success: false);
                return;
            }

            // E03-E04: Equipamentos
            var wetAreas = _roomService.GetWetAreas(detection.Rooms);
            var equipment = _equipmentService.ProcessAll(wetAreas);

            _log.LogInfo("Pipeline concluído com sucesso",
                new
                {
                    rooms = detection.TotalConverted,
                    equipment = equipment.TotalEquipment,
                    duration = "..."
                });

            _log.EndSession(success: true);
        }
        catch (Exception ex)
        {
            _log.LogCritical("Pipeline falhou com exceção não tratada", ex);
            _log.EndSession(success: false);
            throw;
        }
    }
}
```

---

## 10. Exemplos de Log (JSON Lines)

### 10.1 Início de sessão

```json
{"timestamp":"2026-01-18T14:30:22.001Z","level":"Info","message":"Sessão iniciada","correlationId":"sess_20260118_143022_a7f3b2","step":null,"source":"PipelineOrchestrator","data":{"sessionName":"FullPipeline","inputCount":12},"durationMs":null}
```

### 10.2 Início de etapa

```json
{"timestamp":"2026-01-18T14:30:22.015Z","level":"Info","message":"▶ E01_DetectRooms: Detecção de ambientes","correlationId":"sess_20260118_143022_a7f3b2","step":"E01_DetectRooms","source":"IRoomService","data":{"totalInput":12},"durationMs":null}
```

### 10.3 Debug (desativado em produção)

```json
{"timestamp":"2026-01-18T14:30:22.045Z","level":"Debug","message":"Room convertido: Banheiro Social","correlationId":"sess_20260118_143022_a7f3b2","step":"E01_DetectRooms","source":"IRoomService","data":{"id":"room_001","area":4.85},"durationMs":null}
```

### 10.4 Warning

```json
{"timestamp":"2026-01-18T14:30:22.120Z","level":"Warning","message":"Ambiente 'Sala TV' sem classificação — marcado como Unclassified","correlationId":"sess_20260118_143022_a7f3b2","step":"E02_ClassifyRooms","source":"IRoomService","data":{"roomId":"room_007","confidence":0.0},"durationMs":null}
```

### 10.5 Validação

```json
{"timestamp":"2026-01-18T14:30:22.200Z","level":"Info","message":"Validação em lote: Room","correlationId":"sess_20260118_143022_a7f3b2","step":"E05_ValidateModel","source":"IRoomService","data":{"componentType":"Room","totalChecked":12,"totalPassed":11,"totalFailed":1,"criticalCount":0},"durationMs":null}
```

### 10.6 Cálculo (auditoria)

```json
{"timestamp":"2026-01-18T14:30:23.500Z","level":"Info","message":"Cálculo: ProbableFlow","correlationId":"sess_20260118_143022_a7f3b2","step":"E11_SizeNetworks","source":"ISizingService","data":{"calculationType":"ProbableFlow","inputs":{"totalWeight":0.9},"outputs":{"flowRateLs":0.285},"isCompliant":true},"durationMs":0.2}
```

### 10.7 Erro com exceção

```json
{"timestamp":"2026-01-18T14:30:24.100Z","level":"Error","message":"Falha ao posicionar lavatório no Banheiro 02","correlationId":"sess_20260118_143022_a7f3b2","step":"E04_InsertEquipment","source":"IEquipmentService","data":{"equipmentId":"eq_005","roomId":"room_002"},"exceptionType":"InvalidOperationException","exceptionMessage":"Posição fora do bounding box do ambiente","stackTrace":"at EquipmentPositioningRules.CalculatePosition() in ...","durationMs":null}
```

### 10.8 Fim de etapa

```json
{"timestamp":"2026-01-18T14:30:22.300Z","level":"Info","message":"■ E01_DetectRooms: concluído ✅","correlationId":"sess_20260118_143022_a7f3b2","step":"E01_DetectRooms","source":"IRoomService","data":{"converted":12,"errors":0},"durationMs":285.0}
```

### 10.9 Fim de sessão

```json
{"timestamp":"2026-01-18T14:30:25.000Z","level":"Info","message":"Sessão concluída ✅","correlationId":"sess_20260118_143022_a7f3b2","step":null,"source":"PipelineOrchestrator","data":{"success":true,"rooms":12,"equipment":15,"totalDurationMs":4999},"durationMs":4999.0}
```

---

## 11. Boas Práticas

### 11.1 Fazer

```
✅ Verificar IsEnabled() antes de serializar dados pesados:
   if (_log.IsEnabled(LogLevel.Debug))
       _log.LogDebug("Dados do room", new { polygon = room.BoundaryPoints });

✅ Usar PipelineSteps.* como constantes (não strings soltas)
✅ Incluir IDs de componentes nos logs
✅ Logar inputs e outputs de cada etapa
✅ Usar Stopwatch para duração
✅ Capturar exceção no nível correto (Error ≠ Critical)
✅ LogStepStart/End em par (sempre fechar)
```

### 11.2 Não fazer

```
❌ Logar dados sensíveis (caminhos completos do disco, nomes de usuário)
❌ Logar dentro de loops apertados sem verificar IsEnabled()
❌ Duplicar logs (não logar o mesmo evento em 2 serviços)
❌ Usar Console.WriteLine() em vez de ILogService
❌ Ignorar exceções silenciosamente (sempre LogError)
❌ Logar objetos do Revit (ElementId, Document)
❌ Lançar exceção se o logging falhar
```

---

## 12. Resumo Visual

```
ILogService
│
├── Logs Gerais
│   ├── LogDebug(msg, data)
│   ├── LogInfo(msg, data)
│   ├── LogWarning(msg, data)
│   ├── LogError(msg, ex, data)
│   ├── LogCritical(msg, ex, data)
│   └── Log(level, msg, data, ex)
│
├── Logs de Pipeline
│   ├── LogStepStart(step, desc, inputSummary)
│   ├── LogStepEnd(step, success, outputSummary, duration)
│   └── LogStepProgress(step, current, total, detail)
│
├── Logs de Validação
│   ├── LogValidation(type, id, isValid, issues)
│   └── LogBatchValidation(type, checked, passed, failed, critical)
│
├── Logs de Cálculo
│   └── LogCalculation(calcType, inputs, outputs, isCompliant)
│
├── Logs de Integração
│   ├── LogDynamoExecution(script, success, duration, error)
│   └── LogUnMepExecution(operation, success, duration, error)
│
├── Correlação
│   ├── StartSession(name) → correlationId
│   ├── EndSession(success)
│   └── GetCurrentCorrelationId() → string
│
├── Configuração
│   ├── GetMinLevel() → LogLevel
│   ├── SetMinLevel(level)
│   └── IsEnabled(level) → bool
│
├── DTOs
│   ├── LogEntry (formato de persistência)
│   ├── LogValidationIssue
│   └── LogSessionInfo
│
├── Constantes
│   ├── PipelineSteps (E01..E12)
│   └── LogConfiguration (nível, retenção, rotação)
│
└── Persistência
    ├── logs/{data}.jsonl (JSON Lines por dia)
    ├── sessions/{correlationId}.json (resumo)
    └── Rotação: 30 dias, 10 MB/arquivo
```
