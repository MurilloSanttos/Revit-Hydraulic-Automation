using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Revit2026.Modules.DynamoIntegration.Contracts
{
    // ══════════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════════

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DynamoCommand
    {
        RunWorkspace,
        LoadWorkspace,
        SetInputs,
        GetOutputs,
        Ping,
        Reload,
        Terminate,
        Response
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ExecutionMode
    {
        Run,
        Preview,
        Recompute
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum NodeDataType
    {
        String,
        Number,
        Boolean,
        List,
        Dict,
        ElementId,
        XYZ,
        Null
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DynamoErrorSeverity
    {
        Info,
        Warning,
        Error,
        Fatal
    }

    // ══════════════════════════════════════════════════════════════
    //  CONTRATO PRINCIPAL — REQUEST
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Contrato JSON de comunicação Plugin → Dynamo.
    /// Protocolo versionado, extensível e bidirecional.
    ///
    /// Serialização: Newtonsoft.Json
    /// Encoding: UTF-8
    /// Formato: JSON compacto (sem indentação em produção)
    /// </summary>
    public class PluginToDynamoContract
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; } = "1.0.0";

        [JsonProperty("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

        [JsonProperty("command")]
        public DynamoCommand Command { get; set; }

        [JsonProperty("expectReturn")]
        public bool ExpectReturn { get; set; } = true;

        [JsonProperty("payload")]
        public PayloadData Payload { get; set; } = new();

        // ── Serialização ──────────────────────────────────────

        public string ToJson(bool indented = false)
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include
            });
        }

        public static PluginToDynamoContract? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PluginToDynamoContract>(json);
        }

        // ── Builders ──────────────────────────────────────────

        public static PluginToDynamoContract CriarRun(
            string workspacePath,
            List<InputData>? inputs = null,
            List<OutputRequest>? outputs = null,
            int timeoutMs = 30000)
        {
            return new PluginToDynamoContract
            {
                Command = DynamoCommand.RunWorkspace,
                ExpectReturn = true,
                Payload = new PayloadData
                {
                    Workspace = new WorkspaceData { Path = workspacePath },
                    Inputs = inputs ?? new(),
                    Outputs = outputs ?? new(),
                    Execution = new ExecutionData
                    {
                        Mode = ExecutionMode.Run,
                        TimeoutMs = timeoutMs
                    }
                }
            };
        }

        public static PluginToDynamoContract CriarPing()
        {
            return new PluginToDynamoContract
            {
                Command = DynamoCommand.Ping,
                ExpectReturn = true,
                Payload = new PayloadData()
            };
        }

        public static PluginToDynamoContract CriarLoad(string workspacePath)
        {
            return new PluginToDynamoContract
            {
                Command = DynamoCommand.LoadWorkspace,
                ExpectReturn = false,
                Payload = new PayloadData
                {
                    Workspace = new WorkspaceData { Path = workspacePath }
                }
            };
        }

        public static PluginToDynamoContract CriarSetInputs(
            string workspacePath,
            List<InputData> inputs)
        {
            return new PluginToDynamoContract
            {
                Command = DynamoCommand.SetInputs,
                ExpectReturn = false,
                Payload = new PayloadData
                {
                    Workspace = new WorkspaceData { Path = workspacePath },
                    Inputs = inputs
                }
            };
        }

        public static PluginToDynamoContract CriarGetOutputs(
            string workspacePath,
            List<OutputRequest> outputs)
        {
            return new PluginToDynamoContract
            {
                Command = DynamoCommand.GetOutputs,
                ExpectReturn = true,
                Payload = new PayloadData
                {
                    Workspace = new WorkspaceData { Path = workspacePath },
                    Outputs = outputs
                }
            };
        }

        public static PluginToDynamoContract CriarTerminate()
        {
            return new PluginToDynamoContract
            {
                Command = DynamoCommand.Terminate,
                ExpectReturn = false
            };
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  PAYLOAD
    // ══════════════════════════════════════════════════════════════

    public class PayloadData
    {
        [JsonProperty("workspace", NullValueHandling = NullValueHandling.Ignore)]
        public WorkspaceData? Workspace { get; set; }

        [JsonProperty("inputs", NullValueHandling = NullValueHandling.Ignore)]
        public List<InputData>? Inputs { get; set; }

        [JsonProperty("outputs", NullValueHandling = NullValueHandling.Ignore)]
        public List<OutputRequest>? Outputs { get; set; }

        [JsonProperty("execution", NullValueHandling = NullValueHandling.Ignore)]
        public ExecutionData? Execution { get; set; }

        [JsonProperty("errorHandling", NullValueHandling = NullValueHandling.Ignore)]
        public ErrorHandlingData? ErrorHandling { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  WORKSPACE
    // ══════════════════════════════════════════════════════════════

    public class WorkspaceData
    {
        [JsonProperty("path")]
        public string Path { get; set; } = "";

        [JsonProperty("workspaceId", NullValueHandling = NullValueHandling.Ignore)]
        public string? WorkspaceId { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string? Version { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  INPUTS
    // ══════════════════════════════════════════════════════════════

    public class InputData
    {
        [JsonProperty("nodeId")]
        public string NodeId { get; set; } = "";

        [JsonProperty("nodeName", NullValueHandling = NullValueHandling.Ignore)]
        public string? NodeName { get; set; }

        [JsonProperty("dataType")]
        public NodeDataType DataType { get; set; } = NodeDataType.String;

        [JsonProperty("value")]
        public object? Value { get; set; }

        // Builders
        public static InputData String(string nodeId, string value, string? name = null) =>
            new() { NodeId = nodeId, DataType = NodeDataType.String, Value = value, NodeName = name };

        public static InputData Number(string nodeId, double value, string? name = null) =>
            new() { NodeId = nodeId, DataType = NodeDataType.Number, Value = value, NodeName = name };

        public static InputData Boolean(string nodeId, bool value, string? name = null) =>
            new() { NodeId = nodeId, DataType = NodeDataType.Boolean, Value = value, NodeName = name };

        public static InputData List(string nodeId, List<object> value, string? name = null) =>
            new() { NodeId = nodeId, DataType = NodeDataType.List, Value = value, NodeName = name };

        public static InputData Dict(string nodeId, Dictionary<string, object> value, string? name = null) =>
            new() { NodeId = nodeId, DataType = NodeDataType.Dict, Value = value, NodeName = name };

        public static InputData ElementIds(string nodeId, List<long> ids, string? name = null) =>
            new() { NodeId = nodeId, DataType = NodeDataType.ElementId, Value = ids, NodeName = name };

        public static InputData Xyz(string nodeId, double x, double y, double z, string? name = null) =>
            new() { NodeId = nodeId, DataType = NodeDataType.XYZ, Value = new { x, y, z }, NodeName = name };
    }

    // ══════════════════════════════════════════════════════════════
    //  OUTPUTS
    // ══════════════════════════════════════════════════════════════

    public class OutputRequest
    {
        [JsonProperty("nodeId")]
        public string NodeId { get; set; } = "";

        [JsonProperty("alias", NullValueHandling = NullValueHandling.Ignore)]
        public string? Alias { get; set; }

        [JsonProperty("expectedType", NullValueHandling = NullValueHandling.Ignore)]
        public NodeDataType? ExpectedType { get; set; }

        public static OutputRequest Of(string nodeId, string? alias = null) =>
            new() { NodeId = nodeId, Alias = alias };
    }

    // ══════════════════════════════════════════════════════════════
    //  EXECUTION
    // ══════════════════════════════════════════════════════════════

    public class ExecutionData
    {
        [JsonProperty("mode")]
        public ExecutionMode Mode { get; set; } = ExecutionMode.Run;

        [JsonProperty("timeoutMs")]
        public int TimeoutMs { get; set; } = 30000;

        [JsonProperty("forceRecompute")]
        public bool ForceRecompute { get; set; } = false;
    }

    // ══════════════════════════════════════════════════════════════
    //  ERROR HANDLING
    // ══════════════════════════════════════════════════════════════

    public class ErrorHandlingData
    {
        [JsonProperty("stopOnNodeError")]
        public bool StopOnNodeError { get; set; } = true;

        [JsonProperty("returnPartialResults")]
        public bool ReturnPartialResults { get; set; } = false;

        [JsonProperty("retryCount")]
        public int RetryCount { get; set; } = 0;
    }

    // ══════════════════════════════════════════════════════════════
    //  CONTRATO DE RESPOSTA — DYNAMO → PLUGIN
    // ══════════════════════════════════════════════════════════════

    public class DynamoToPluginResponse
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; } = "1.0.0";

        [JsonProperty("requestId")]
        public string RequestId { get; set; } = "";

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

        [JsonProperty("command")]
        public DynamoCommand Command { get; set; } = DynamoCommand.Response;

        [JsonProperty("result")]
        public ResultData Result { get; set; } = new();

        // ── Serialização ──────────────────────────────────────

        public string ToJson(bool indented = false)
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public static DynamoToPluginResponse? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<DynamoToPluginResponse>(json);
        }

        // ── Builders ──────────────────────────────────────────

        public static DynamoToPluginResponse Sucesso(
            string requestId,
            List<OutputResult>? outputs = null)
        {
            return new DynamoToPluginResponse
            {
                RequestId = requestId,
                Result = new ResultData
                {
                    Success = true,
                    Outputs = outputs ?? new(),
                    ExecutionTimeMs = 0
                }
            };
        }

        public static DynamoToPluginResponse Falha(
            string requestId,
            string errorMessage,
            string? errorCode = null)
        {
            return new DynamoToPluginResponse
            {
                RequestId = requestId,
                Result = new ResultData
                {
                    Success = false,
                    Errors = new List<DynamoError>
                    {
                        new()
                        {
                            Code = errorCode ?? "DYNAMO_ERROR",
                            Message = errorMessage,
                            Severity = DynamoErrorSeverity.Error
                        }
                    }
                }
            };
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULT
    // ══════════════════════════════════════════════════════════════

    public class ResultData
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("outputs", NullValueHandling = NullValueHandling.Ignore)]
        public List<OutputResult>? Outputs { get; set; }

        [JsonProperty("errors", NullValueHandling = NullValueHandling.Ignore)]
        public List<DynamoError>? Errors { get; set; }

        [JsonProperty("warnings", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Warnings { get; set; }

        [JsonProperty("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonProperty("nodesExecuted")]
        public int NodesExecuted { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  OUTPUT RESULT
    // ══════════════════════════════════════════════════════════════

    public class OutputResult
    {
        [JsonProperty("nodeId")]
        public string NodeId { get; set; } = "";

        [JsonProperty("alias", NullValueHandling = NullValueHandling.Ignore)]
        public string? Alias { get; set; }

        [JsonProperty("dataType")]
        public NodeDataType DataType { get; set; }

        [JsonProperty("value")]
        public object? Value { get; set; }

        [JsonProperty("isNull")]
        public bool IsNull { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  ERRORS
    // ══════════════════════════════════════════════════════════════

    public class DynamoError
    {
        [JsonProperty("code")]
        public string Code { get; set; } = "";

        [JsonProperty("message")]
        public string Message { get; set; } = "";

        [JsonProperty("severity")]
        public DynamoErrorSeverity Severity { get; set; } = DynamoErrorSeverity.Error;

        [JsonProperty("nodeId", NullValueHandling = NullValueHandling.Ignore)]
        public string? NodeId { get; set; }

        [JsonProperty("nodeName", NullValueHandling = NullValueHandling.Ignore)]
        public string? NodeName { get; set; }

        [JsonProperty("stackTrace", NullValueHandling = NullValueHandling.Ignore)]
        public string? StackTrace { get; set; }
    }
}
