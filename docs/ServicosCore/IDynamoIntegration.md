# Serviço de Integração — IDynamoIntegration

> Especificação completa da interface de integração com o Dynamo, responsável pelo disparo, controle, monitoramento e tratamento de resultados de scripts Dynamo, usando JSON como contrato de comunicação, totalmente agnóstica ao Revit.

---

## 1. Definição da Interface

### 1.1 O que é IDynamoIntegration

`IDynamoIntegration` é a **interface de fronteira** entre o PluginCore e o Dynamo. O Core não sabe como o Dynamo funciona internamente — ele apenas sabe que pode enviar um JSON de entrada, pedir a execução de um script identificado por nome, e receber um JSON de saída. Toda a mecânica real de execução fica na implementação (camada Infrastructure).

### 1.2 Papel no sistema

```
IOrchestratorService (Core)
    │
    │  "Preciso criar redes de tubulação no Revit"
    │
    ├──→ INetworkService.ExportForRevit() → NetworkExportData
    │
    │  "Agora preciso materializar no modelo"
    │
    ├──→ IDynamoIntegration.ExecuteScript(         ← ESTA INTERFACE
    │        scriptId: "CreatePipeNetwork",
    │        inputJson: serialized NetworkExportData)
    │
    │  Implementação (Infrastructure):
    │    ├── DynamoScriptExecutor
    │    │     ├── Localiza o .dyn no disco
    │    │     ├── Passa inputs via Dynamo Sandbox API
    │    │     ├── Executa o script
    │    │     └── Captura outputs e erros
    │    └── Retorna DynamoExecutionResult
    │
    └──→ Core recebe resultado (JSON com ElementIds criados)
```

### 1.3 Por que é independente do Revit

```
A INTERFACE (Core):
  - Define CONTRATOS (DTOs puros)
  - Não importa nenhum namespace Dynamo ou Revit
  - Usa: string (scriptId), string (JSON), DTOs próprios
  - 100% testável com mock

A IMPLEMENTAÇÃO (Infrastructure):
  - DynamoScriptExecutor : IDynamoIntegration
  - Importa: Dynamo.Applications, Autodesk.Revit.DB
  - Conhece: DynamoModel, WorkspaceModel, RunSettings
  - Traduz JSON → Dynamo inputs → execução → captura outputs

RESULTADO:
  O Core testa toda a lógica de integração SEM Dynamo instalado.
  Mocks retornam JSONs pré-definidos simulando execução real.
```

---

## 2. Responsabilidades

### 2.1 O que o serviço DEVE fazer

| Ação | Detalhe |
|------|---------|
| Disparar execução | Enviar scriptId + inputJson → implementação executa |
| Validar entrada | Verificar JSON antes de enviar ao Dynamo |
| Validar saída | Verificar JSON retornado pelo Dynamo |
| Monitorar execução | Status: Running, Completed, Failed, Timeout |
| Cancelar execução | Abortar se exceder timeout ou por comando do usuário |
| Retry | Reexecutar com mesmo input ou input corrigido |
| Capturar erros | Mensagens do Dynamo, exceções, warnings |
| Logar | Registrar via ILogService (início, fim, erro, duração) |
| Listar scripts | Retornar scripts disponíveis e seus schemas |
| Verificar saúde | Confirmar que Dynamo está acessível |

### 2.2 O que NÃO DEVE fazer

| Proibição | Quem faz |
|-----------|---------|
| ❌ Montar lógica de negócio | Serviços do Core |
| ❌ Decidir quais pipes criar | INetworkService |
| ❌ Calcular diâmetros | ISizingService |
| ❌ Manipular Dynamo diretamente | DynamoScriptExecutor (Infrastructure) |
| ❌ Acessar API do Revit | Camada Infrastructure |
| ❌ Modificar scripts .dyn | Responsabilidade do desenvolvedor |

---

## 3. Conceitos-Chave

### 3.1 Script Dynamo

```
Um script Dynamo (.dyn) = automação visual que manipula o modelo Revit.
No nosso sistema, cada script tem:
  - scriptId: identificador único (ex: "CreatePipeNetwork")
  - Arquivo .dyn no disco
  - Schema de entrada: quais inputs espera (JSON Schema)
  - Schema de saída: o que retorna (JSON Schema)
```

### 3.2 Execução

```
Uma execução = 1 chamada a um script com inputs específicos.
Tem:
  - executionId: GUID único
  - Entrada: JSON
  - Saída: JSON (ou erro)
  - Status: Pending → Running → Completed/Failed/Timeout/Cancelled
  - Duração
```

### 3.3 Contrato JSON

```
ENTRADA (Core → Dynamo):
  O Core serializa seus DTOs como JSON.
  O script Dynamo lê esse JSON via nós Data.ImportJSON ou Python Script.
  Caminho: arquivo temporário ou memória compartilhada.

SAÍDA (Dynamo → Core):
  O script Dynamo escreve um JSON com resultados.
  O Core deserializa para seus DTOs.
  Formato: definido por schema no catálogo de scripts.
```

---

## 4. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Interface de integração com o Dynamo.
    /// Dispara, monitora e captura resultados de scripts Dynamo.
    /// Usa JSON como contrato de comunicação.
    /// Independente do Revit e do Dynamo.
    /// </summary>
    public interface IDynamoIntegration
    {
        // ══════════════════════════════════════════════════════════
        //  EXECUÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa um script Dynamo de forma síncrona.
        /// Bloqueia até concluir, falhar ou atingir timeout.
        /// </summary>
        /// <param name="request">Requisição com scriptId, inputJson e opções.</param>
        /// <returns>Resultado com outputJson, status e erros.</returns>
        DynamoExecutionResult ExecuteScript(DynamoExecutionRequest request);

        /// <summary>
        /// Executa um script Dynamo de forma assíncrona.
        /// Retorna imediatamente com executionId.
        /// Usar GetExecutionStatus() para monitorar.
        /// </summary>
        Task<DynamoExecutionResult> ExecuteScriptAsync(
            DynamoExecutionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atalho: executa script por ID com JSON de entrada.
        /// </summary>
        DynamoExecutionResult Execute(string scriptId, string inputJson);

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida o JSON de entrada contra o schema do script.
        /// Deve ser chamado ANTES de ExecuteScript.
        /// </summary>
        DynamoValidationResult ValidateInput(string scriptId, string inputJson);

        /// <summary>
        /// Valida o JSON de saída contra o schema esperado.
        /// Chamado internamente após execução.
        /// </summary>
        DynamoValidationResult ValidateOutput(string scriptId, string outputJson);

        // ══════════════════════════════════════════════════════════
        //  MONITORAMENTO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o status de uma execução em andamento.
        /// </summary>
        DynamoExecutionStatus GetExecutionStatus(string executionId);

        /// <summary>
        /// Cancela uma execução em andamento.
        /// </summary>
        bool CancelExecution(string executionId);

        /// <summary>
        /// Reexecuta um script que falhou.
        /// Usa o mesmo input da execução anterior.
        /// </summary>
        DynamoExecutionResult RetryExecution(string executionId);

        // ══════════════════════════════════════════════════════════
        //  CATÁLOGO DE SCRIPTS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna a lista de scripts Dynamo disponíveis.
        /// </summary>
        List<DynamoScriptInfo> GetAvailableScripts();

        /// <summary>
        /// Retorna informações de um script específico.
        /// </summary>
        DynamoScriptInfo GetScriptInfo(string scriptId);

        /// <summary>
        /// Verifica se um script está disponível e acessível.
        /// </summary>
        bool IsScriptAvailable(string scriptId);

        // ══════════════════════════════════════════════════════════
        //  SAÚDE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se o Dynamo está acessível e funcional.
        /// </summary>
        DynamoHealthCheck CheckHealth();

        // ══════════════════════════════════════════════════════════
        //  HISTÓRICO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o histórico de execuções da sessão atual.
        /// </summary>
        List<DynamoExecutionSummary> GetExecutionHistory();
    }
}
```

---

## 5. DTOs

### 5.1 DynamoExecutionRequest

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Requisição de execução de script Dynamo.
    /// </summary>
    public class DynamoExecutionRequest
    {
        /// <summary>
        /// Identificador do script no catálogo.
        /// Ex: "CreatePipeNetwork", "InsertEquipment"
        /// </summary>
        public string ScriptId { get; set; }

        /// <summary>
        /// JSON de entrada para o script.
        /// Deve estar conforme o InputSchema do script.
        /// </summary>
        public string InputJson { get; set; }

        /// <summary>
        /// Timeout em segundos. 0 = sem timeout.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// Se deve validar o input antes de executar.
        /// </summary>
        public bool ValidateBeforeExecute { get; set; } = true;

        /// <summary>
        /// Se deve validar o output após executar.
        /// </summary>
        public bool ValidateAfterExecute { get; set; } = true;

        /// <summary>
        /// Número máximo de tentativas automáticas em caso de falha.
        /// </summary>
        public int MaxRetries { get; set; } = 1;

        /// <summary>
        /// Etapa do pipeline que solicitou a execução.
        /// Para correlação de logs.
        /// </summary>
        public string PipelineStep { get; set; }

        /// <summary>
        /// Metadados adicionais (para logging e rastreabilidade).
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
```

### 5.2 DynamoExecutionResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da execução de um script Dynamo.
    /// </summary>
    public class DynamoExecutionResult
    {
        // ── Identidade ──────────────────────────────────────────

        /// <summary>ID único desta execução.</summary>
        public string ExecutionId { get; set; }

        /// <summary>ID do script executado.</summary>
        public string ScriptId { get; set; }

        // ── Status ──────────────────────────────────────────────

        /// <summary>Se a execução foi bem-sucedida.</summary>
        public bool IsSuccessful { get; set; }

        /// <summary>Status final da execução.</summary>
        public DynamoExecutionStatusEnum Status { get; set; }

        // ── Dados ───────────────────────────────────────────────

        /// <summary>JSON de saída do script (quando bem-sucedido).</summary>
        public string OutputJson { get; set; }

        /// <summary>JSON de entrada usado (para auditoria).</summary>
        public string InputJsonUsed { get; set; }

        // ── Erro ────────────────────────────────────────────────

        /// <summary>Mensagem de erro (quando falhou).</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Tipo de erro.</summary>
        public DynamoErrorType? ErrorType { get; set; }

        /// <summary>Detalhes técnicos do erro.</summary>
        public string ErrorDetails { get; set; }

        /// <summary>Warnings do Dynamo (não impeditivos).</summary>
        public List<string> Warnings { get; set; } = new();

        // ── Métricas ────────────────────────────────────────────

        /// <summary>Duração total da execução.</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Número da tentativa (1 = primeira).</summary>
        public int AttemptNumber { get; set; } = 1;

        /// <summary>Timestamp de início UTC.</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>Timestamp de fim UTC.</summary>
        public DateTime CompletedAt { get; set; }

        // ── Métodos auxiliares ───────────────────────────────────

        /// <summary>Deserializa o output para um tipo específico.</summary>
        public T GetOutput<T>()
        {
            if (string.IsNullOrEmpty(OutputJson)) return default;
            return System.Text.Json.JsonSerializer.Deserialize<T>(OutputJson);
        }

        public string GetSummary() =>
            $"[{ScriptId}] {Status} " +
            $"({Duration.TotalSeconds:F1}s, attempt #{AttemptNumber}) " +
            $"{(IsSuccessful ? "✅" : $"❌ {ErrorMessage}")}";
    }
}
```

### 5.3 Enums de Execução

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>Status de execução do Dynamo.</summary>
    public enum DynamoExecutionStatusEnum
    {
        Pending = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Timeout = 4,
        Cancelled = 5,
        ValidationFailed = 6
    }

    /// <summary>Tipo de erro do Dynamo.</summary>
    public enum DynamoErrorType
    {
        /// <summary>Script não encontrado no disco.</summary>
        ScriptNotFound = 1,

        /// <summary>JSON de entrada inválido.</summary>
        InvalidInput = 2,

        /// <summary>JSON de saída inválido ou inesperado.</summary>
        InvalidOutput = 3,

        /// <summary>Erro interno do Dynamo (nó com erro).</summary>
        ScriptError = 4,

        /// <summary>Execução excedeu o timeout.</summary>
        Timeout = 5,

        /// <summary>Dynamo não acessível.</summary>
        DynamoUnavailable = 6,

        /// <summary>Erro na API do Revit durante execução.</summary>
        RevitApiError = 7,

        /// <summary>Cancelado pelo usuário.</summary>
        Cancelled = 8,

        /// <summary>Erro desconhecido.</summary>
        Unknown = 99
    }
}
```

### 5.4 DynamoExecutionStatus (monitoramento)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Status de uma execução em andamento.
    /// </summary>
    public class DynamoExecutionStatus
    {
        public string ExecutionId { get; set; }
        public DynamoExecutionStatusEnum Status { get; set; }
        public double ProgressPercent { get; set; }
        public string CurrentNode { get; set; }
        public TimeSpan Elapsed { get; set; }
        public List<string> Warnings { get; set; } = new();
        public bool IsRunning => Status == DynamoExecutionStatusEnum.Running;
    }
}
```

### 5.5 DynamoValidationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da validação de JSON (input ou output).
    /// </summary>
    public class DynamoValidationResult
    {
        public bool IsValid { get; set; }
        public List<DynamoValidationIssue> Issues { get; set; } = new();

        public string GetSummary() =>
            IsValid ? "✅ JSON válido" :
            $"❌ {Issues.Count} problemas: " +
            string.Join(", ", Issues.Select(i => i.Message));
    }

    public class DynamoValidationIssue
    {
        /// <summary>Campo com problema (JSON path).</summary>
        public string Path { get; set; }

        /// <summary>Descrição do problema.</summary>
        public string Message { get; set; }

        /// <summary>Valor encontrado.</summary>
        public string ActualValue { get; set; }

        /// <summary>Valor esperado.</summary>
        public string ExpectedValue { get; set; }
    }
}
```

### 5.6 DynamoScriptInfo (catálogo)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Metadados de um script Dynamo disponível.
    /// </summary>
    public class DynamoScriptInfo
    {
        /// <summary>Identificador único (ex: "CreatePipeNetwork").</summary>
        public string ScriptId { get; set; }

        /// <summary>Nome amigável (ex: "Criar Rede de Tubulação").</summary>
        public string DisplayName { get; set; }

        /// <summary>Descrição do que o script faz.</summary>
        public string Description { get; set; }

        /// <summary>Caminho do arquivo .dyn (relativo à pasta de scripts).</summary>
        public string FilePath { get; set; }

        /// <summary>Versão do script.</summary>
        public string Version { get; set; }

        /// <summary>Etapa do pipeline onde é usado.</summary>
        public string PipelineStep { get; set; }

        /// <summary>JSON Schema do input esperado.</summary>
        public string InputSchema { get; set; }

        /// <summary>JSON Schema do output esperado.</summary>
        public string OutputSchema { get; set; }

        /// <summary>Timeout padrão em segundos.</summary>
        public int DefaultTimeoutSeconds { get; set; } = 120;

        /// <summary>Se é seguro reexecutar (idempotente).</summary>
        public bool IsIdempotent { get; set; }

        /// <summary>Se o arquivo .dyn existe no disco.</summary>
        public bool IsAvailable { get; set; }
    }
}
```

### 5.7 DynamoHealthCheck

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da verificação de saúde do Dynamo.
    /// </summary>
    public class DynamoHealthCheck
    {
        /// <summary>Dynamo está acessível?</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Versão do Dynamo detectada.</summary>
        public string DynamoVersion { get; set; }

        /// <summary>Pasta de scripts acessível?</summary>
        public bool ScriptsDirectoryExists { get; set; }

        /// <summary>Quantidade de scripts encontrados.</summary>
        public int ScriptsFound { get; set; }

        /// <summary>Scripts com erro (não encontrados ou inválidos).</summary>
        public List<string> MissingScripts { get; set; } = new();

        /// <summary>Mensagem de status.</summary>
        public string Message { get; set; }
    }
}
```

### 5.8 DynamoExecutionSummary (histórico)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class DynamoExecutionSummary
    {
        public string ExecutionId { get; set; }
        public string ScriptId { get; set; }
        public DynamoExecutionStatusEnum Status { get; set; }
        public bool IsSuccessful { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartedAt { get; set; }
        public int AttemptNumber { get; set; }
        public string ErrorMessage { get; set; }
    }
}
```

---

## 6. Catálogo de Scripts

```csharp
namespace HidraulicoPlugin.Core.Configuration
{
    /// <summary>
    /// Catálogo de scripts Dynamo usados pelo sistema.
    /// Fonte de verdade para scriptIds.
    /// </summary>
    public static class DynamoScriptCatalog
    {
        /// <summary>Diretório base dos scripts .dyn.</summary>
        public const string ScriptsDirectory = "DynamoScripts";

        // ── Scripts por etapa ───────────────────────────────────

        /// <summary>E04: Inserir famílias de equipamentos no modelo.</summary>
        public static readonly DynamoScriptInfo InsertEquipment = new()
        {
            ScriptId = "InsertEquipment",
            DisplayName = "Inserir Equipamentos",
            Description = "Insere FamilyInstances de aparelhos sanitários nas posições calculadas",
            FilePath = "E04_InsertEquipment.dyn",
            PipelineStep = "E04",
            DefaultTimeoutSeconds = 60,
            IsIdempotent = false,
            InputSchema = @"{
                'type': 'object',
                'properties': {
                    'equipment': {
                        'type': 'array',
                        'items': {
                            'type': 'object',
                            'properties': {
                                'familyName': { 'type': 'string' },
                                'typeName': { 'type': 'string' },
                                'positionX': { 'type': 'number' },
                                'positionY': { 'type': 'number' },
                                'positionZ': { 'type': 'number' },
                                'levelName': { 'type': 'string' },
                                'rotationDeg': { 'type': 'number' }
                            },
                            'required': ['familyName','typeName','positionX','positionY','positionZ','levelName']
                        }
                    }
                },
                'required': ['equipment']
            }"
        };

        /// <summary>E10: Criar tubulações no modelo Revit.</summary>
        public static readonly DynamoScriptInfo CreatePipeNetwork = new()
        {
            ScriptId = "CreatePipeNetwork",
            DisplayName = "Criar Rede de Tubulação",
            Description = "Cria Pipes, Fittings e PipingSystems a partir de segmentos",
            FilePath = "E10_CreatePipeNetwork.dyn",
            PipelineStep = "E10",
            DefaultTimeoutSeconds = 180,
            IsIdempotent = false,
            InputSchema = @"{
                'type': 'object',
                'properties': {
                    'systemTypeName': { 'type': 'string' },
                    'pipeTypeName': { 'type': 'string' },
                    'segments': {
                        'type': 'array',
                        'items': {
                            'type': 'object',
                            'properties': {
                                'segmentId': { 'type': 'string' },
                                'startX': { 'type': 'number' },
                                'startY': { 'type': 'number' },
                                'startZ': { 'type': 'number' },
                                'endX': { 'type': 'number' },
                                'endY': { 'type': 'number' },
                                'endZ': { 'type': 'number' },
                                'diameterMm': { 'type': 'integer' },
                                'levelName': { 'type': 'string' }
                            },
                            'required': ['segmentId','startX','startY','startZ','endX','endY','endZ','diameterMm','levelName']
                        }
                    }
                },
                'required': ['systemTypeName','pipeTypeName','segments']
            }"
        };

        /// <summary>E10: Criar fittings (conexões) entre pipes.</summary>
        public static readonly DynamoScriptInfo CreateFittings = new()
        {
            ScriptId = "CreateFittings",
            DisplayName = "Criar Connections/Fittings",
            Description = "Cria PipeFittings nos pontos de conexão",
            FilePath = "E10_CreateFittings.dyn",
            PipelineStep = "E10",
            DefaultTimeoutSeconds = 120,
            IsIdempotent = false
        };

        /// <summary>E10: Definir parâmetros de declividade (esgoto).</summary>
        public static readonly DynamoScriptInfo SetPipeSlope = new()
        {
            ScriptId = "SetPipeSlope",
            DisplayName = "Configurar Declividade",
            Description = "Aplica slope em pipes horizontais de esgoto",
            FilePath = "E10_SetPipeSlope.dyn",
            PipelineStep = "E10",
            DefaultTimeoutSeconds = 60,
            IsIdempotent = true
        };

        /// <summary>Todos os scripts registrados.</summary>
        public static List<DynamoScriptInfo> All => new()
        {
            InsertEquipment,
            CreatePipeNetwork,
            CreateFittings,
            SetPipeSlope
        };

        /// <summary>Busca por scriptId.</summary>
        public static DynamoScriptInfo GetById(string scriptId) =>
            All.FirstOrDefault(s => s.ScriptId == scriptId);
    }
}
```

---

## 7. Fluxo de Comunicação (JSON)

### 7.1 Input JSON — Criar redes

```json
{
  "systemTypeName": "Domestic Cold Water",
  "pipeTypeName": "PVC Soldável",
  "segments": [
    {
      "segmentId": "seg_af_001",
      "startX": 5.00, "startY": 4.00, "startZ": 2.80,
      "endX": 5.00, "endY": 1.50, "endZ": 2.80,
      "diameterMm": 25,
      "levelName": "1º Pavimento",
      "slopePercent": 0.0,
      "segmentType": "Branch"
    },
    {
      "segmentId": "seg_af_002",
      "startX": 5.00, "startY": 1.50, "startZ": 2.80,
      "endX": 5.00, "endY": 1.50, "endZ": 0.60,
      "diameterMm": 20,
      "levelName": "1º Pavimento",
      "slopePercent": 0.0,
      "segmentType": "SubBranch"
    }
  ]
}
```

### 7.2 Output JSON — Resultado de criação

```json
{
  "success": true,
  "createdElements": [
    {
      "segmentId": "seg_af_001",
      "revitElementId": "987654",
      "elementType": "Pipe",
      "created": true
    },
    {
      "segmentId": "seg_af_002",
      "revitElementId": "987655",
      "elementType": "Pipe",
      "created": true
    }
  ],
  "createdFittings": [
    {
      "revitElementId": "987656",
      "fittingType": "Elbow90",
      "position": { "x": 5.00, "y": 1.50, "z": 2.80 }
    }
  ],
  "systemId": "987650",
  "totalPipesCreated": 2,
  "totalFittingsCreated": 1,
  "totalLengthM": 4.70,
  "warnings": [],
  "executionTimeMs": 2350
}
```

---

## 8. Tratamento de Erros

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Estratégias de tratamento de erro para execução Dynamo.
    /// </summary>
    public static class DynamoErrorHandling
    {
        /// <summary>
        /// Determina se um erro é passível de retry.
        /// </summary>
        public static bool IsRetryable(DynamoErrorType errorType) => errorType switch
        {
            DynamoErrorType.Timeout => true,           // Pode ter sido lentidão pontual
            DynamoErrorType.DynamoUnavailable => true,  // Pode ficar disponível
            DynamoErrorType.ScriptError => false,       // Mesmo input = mesmo erro
            DynamoErrorType.InvalidInput => false,      // Precisa corrigir input
            DynamoErrorType.InvalidOutput => false,     // Problema no script
            DynamoErrorType.ScriptNotFound => false,    // Precisa corrigir caminho
            DynamoErrorType.Cancelled => false,         // Intencional
            _ => false
        };

        /// <summary>
        /// Tempo de espera antes de retry (backoff exponencial).
        /// </summary>
        public static TimeSpan GetRetryDelay(int attemptNumber) =>
            TimeSpan.FromSeconds(Math.Pow(2, attemptNumber - 1)); // 1s, 2s, 4s, 8s...
    }
}
```

---

## 9. Exemplo de Uso (no Orchestrator / E10)

```csharp
public class PipelineOrchestrator
{
    private readonly IDynamoIntegration _dynamo;
    private readonly INetworkService _networkService;
    private readonly ILogService _log;

    public async Task ExecuteE10_ExportToRevit(PipeNetwork networkAF)
    {
        _log.LogStepStart("E10_ExportToRevit", "Criação de elementos no Revit");

        // 1. Verificar saúde do Dynamo
        var health = _dynamo.CheckHealth();
        if (!health.IsAvailable)
        {
            _log.LogCritical("Dynamo não disponível", data: health);
            return;
        }

        // 2. Exportar dados da rede
        var exportData = _networkService.ExportForRevit(networkAF);

        // 3. Serializar para JSON
        string inputJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            systemTypeName = exportData.RevitSystemTypeName,
            pipeTypeName = exportData.RevitPipeTypeName,
            segments = exportData.Segments.Select(s => new
            {
                segmentId = s.SegmentId,
                startX = s.StartPosition.X,
                startY = s.StartPosition.Y,
                startZ = s.StartPosition.Z,
                endX = s.EndPosition.X,
                endY = s.EndPosition.Y,
                endZ = s.EndPosition.Z,
                diameterMm = s.DiameterMm,
                levelName = s.LevelName,
                slopePercent = s.SlopePercent,
                segmentType = s.Type.ToString()
            })
        });

        // 4. Validar input
        var validation = _dynamo.ValidateInput("CreatePipeNetwork", inputJson);
        if (!validation.IsValid)
        {
            _log.LogError("Input JSON inválido", data: validation);
            return;
        }

        // 5. Executar script
        var result = await _dynamo.ExecuteScriptAsync(new DynamoExecutionRequest
        {
            ScriptId = "CreatePipeNetwork",
            InputJson = inputJson,
            TimeoutSeconds = 180,
            PipelineStep = "E10",
            MaxRetries = 2,
            Metadata = new()
            {
                ["system"] = "ColdWater",
                ["segmentCount"] = exportData.Segments.Count.ToString()
            }
        });

        // 6. Processar resultado
        if (result.IsSuccessful)
        {
            var output = result.GetOutput<PipeCreationOutput>();
            _log.LogInfo($"Rede AF criada: {output.TotalPipesCreated} pipes, " +
                         $"{output.TotalFittingsCreated} fittings",
                data: new { systemId = output.SystemId });

            _log.LogDynamoExecution("CreatePipeNetwork", true, result.Duration);
        }
        else
        {
            _log.LogError($"Falha ao criar rede: {result.ErrorMessage}",
                data: new { errorType = result.ErrorType, attempt = result.AttemptNumber });

            _log.LogDynamoExecution("CreatePipeNetwork", false,
                result.Duration, result.ErrorMessage);
        }

        _log.LogStepEnd("E10_ExportToRevit", result.IsSuccessful,
            new { status = result.Status }, result.Duration);
    }
}
```

---

## 10. Resumo Visual

```
IDynamoIntegration
│
├── Execução
│   ├── ExecuteScript(request) → DynamoExecutionResult
│   ├── ExecuteScriptAsync(request, ct) → Task<DynamoExecutionResult>
│   └── Execute(scriptId, inputJson) → DynamoExecutionResult
│
├── Validação
│   ├── ValidateInput(scriptId, json) → DynamoValidationResult
│   └── ValidateOutput(scriptId, json) → DynamoValidationResult
│
├── Monitoramento
│   ├── GetExecutionStatus(executionId) → DynamoExecutionStatus
│   ├── CancelExecution(executionId) → bool
│   └── RetryExecution(executionId) → DynamoExecutionResult
│
├── Catálogo
│   ├── GetAvailableScripts() → List<DynamoScriptInfo>
│   ├── GetScriptInfo(scriptId) → DynamoScriptInfo
│   └── IsScriptAvailable(scriptId) → bool
│
├── Saúde
│   └── CheckHealth() → DynamoHealthCheck
│
├── Histórico
│   └── GetExecutionHistory() → List<DynamoExecutionSummary>
│
├── DTOs
│   ├── DynamoExecutionRequest (scriptId + inputJson + opções)
│   ├── DynamoExecutionResult (outputJson + status + erros)
│   ├── DynamoExecutionStatus (monitoramento em tempo real)
│   ├── DynamoValidationResult (JSON schema check)
│   ├── DynamoScriptInfo (metadados do .dyn)
│   ├── DynamoHealthCheck (disponibilidade)
│   └── DynamoExecutionSummary (histórico)
│
├── Enums
│   ├── DynamoExecutionStatusEnum (7 estados)
│   └── DynamoErrorType (9 tipos de erro)
│
├── Catálogo de Scripts
│   ├── InsertEquipment (E04 — inserir famílias)
│   ├── CreatePipeNetwork (E10 — criar tubulações)
│   ├── CreateFittings (E10 — criar conexões)
│   └── SetPipeSlope (E10 — aplicar declividade)
│
├── Comunicação
│   ├── Input: JSON serializado dos DTOs do Core
│   ├── Output: JSON com ElementIds criados no Revit
│   └── Schema: definido por script no catálogo
│
└── Erro
    ├── Retry: apenas Timeout e DynamoUnavailable
    ├── Backoff: exponencial (1s, 2s, 4s)
    └── MaxRetries: configurável por request
```
