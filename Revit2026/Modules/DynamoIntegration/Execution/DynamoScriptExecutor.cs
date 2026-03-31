using System.Diagnostics;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;

namespace Revit2026.Modules.DynamoIntegration.Execution
{
    public class DynamoExecutionResult
    {
        public bool Success { get; set; }
        public string? OutputJson { get; set; }
        public string ErrorMessage { get; set; } = "";
        public long ExecutionTimeMs { get; set; }
        public int NodesExecuted { get; set; }
        public string WorkspacePath { get; set; } = "";

        public override string ToString() =>
            $"{(Success ? "OK" : "FAIL")}: {ErrorMessage} ({ExecutionTimeMs}ms)";
    }

    public class DynamoScriptExecutorConfig
    {
        public int TimeoutMs { get; set; } = 60000;
        public string OutputNodeName { get; set; } = "OUT_JSON";
        public bool ForceRecompute { get; set; } = false;
        public bool CloseWorkspaceAfter { get; set; } = true;
    }

    public interface IDynamoScriptExecutor
    {
        DynamoExecutionResult RunScript(
            string scriptPath,
            Dictionary<string, object>? inputs = null);
    }

    public class DynamoScriptExecutor : IDynamoScriptExecutor
    {
        private readonly DynamoModel _dynamoModel;
        private readonly DynamoScriptExecutorConfig _config;
        private readonly object _executionLock = new();

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTOR
        // ══════════════════════════════════════════════════════════

        public DynamoScriptExecutor(DynamoModel dynamoModel)
        {
            _dynamoModel = dynamoModel
                ?? throw new ArgumentNullException(nameof(dynamoModel));
            _config = new DynamoScriptExecutorConfig();
        }

        public DynamoScriptExecutor(
            DynamoModel dynamoModel,
            DynamoScriptExecutorConfig config)
        {
            _dynamoModel = dynamoModel
                ?? throw new ArgumentNullException(nameof(dynamoModel));
            _config = config ?? new DynamoScriptExecutorConfig();
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUTAR SCRIPT
        // ══════════════════════════════════════════════════════════

        public DynamoExecutionResult RunScript(
            string scriptPath,
            Dictionary<string, object>? inputs = null)
        {
            return RunScript(scriptPath, inputs, _config);
        }

        public DynamoExecutionResult RunScript(
            string scriptPath,
            Dictionary<string, object>? inputs,
            DynamoScriptExecutorConfig config)
        {
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrEmpty(scriptPath))
            {
                return new DynamoExecutionResult
                {
                    ErrorMessage = "Script path is empty."
                };
            }

            if (!File.Exists(scriptPath))
            {
                return new DynamoExecutionResult
                {
                    ErrorMessage = $"Script not found: {scriptPath}",
                    WorkspacePath = scriptPath
                };
            }

            lock (_executionLock)
            {
                try
                {
                    // 1. Carregar workspace
                    var workspace = LoadWorkspace(scriptPath);
                    if (workspace == null)
                    {
                        return new DynamoExecutionResult
                        {
                            ErrorMessage = "Failed to load workspace.",
                            WorkspacePath = scriptPath,
                            ExecutionTimeMs = sw.ElapsedMilliseconds
                        };
                    }

                    // 2. Injetar inputs
                    if (inputs != null && inputs.Count > 0)
                    {
                        var injectResult = InjectInputs(workspace, inputs);
                        if (!injectResult.Success)
                        {
                            return new DynamoExecutionResult
                            {
                                ErrorMessage = injectResult.Message,
                                WorkspacePath = scriptPath,
                                ExecutionTimeMs = sw.ElapsedMilliseconds
                            };
                        }
                    }

                    // 3. Executar
                    var runResult = RunAndWait(workspace, config.TimeoutMs);
                    if (!runResult.Success)
                    {
                        return new DynamoExecutionResult
                        {
                            ErrorMessage = runResult.Message,
                            WorkspacePath = scriptPath,
                            ExecutionTimeMs = sw.ElapsedMilliseconds
                        };
                    }

                    // 4. Extrair saída
                    var output = ExtractOutput(workspace, config.OutputNodeName);

                    return new DynamoExecutionResult
                    {
                        Success = true,
                        OutputJson = output,
                        WorkspacePath = scriptPath,
                        ExecutionTimeMs = sw.ElapsedMilliseconds,
                        NodesExecuted = workspace.Nodes.Count()
                    };
                }
                catch (Exception ex)
                {
                    return new DynamoExecutionResult
                    {
                        ErrorMessage = $"Execution error: {ex.Message}",
                        WorkspacePath = scriptPath,
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }
                finally
                {
                    if (config.CloseWorkspaceAfter)
                    {
                        try
                        {
                            // Dynamo 3.x: CloseWorkspaceCommand removed
                            // Use reflection or skip — workspace auto-closes
                        }
                        catch { /* silencioso */ }
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUTAR MÚLTIPLOS
        // ══════════════════════════════════════════════════════════

        public List<DynamoExecutionResult> RunMultiple(
            IList<(string ScriptPath, Dictionary<string, object>? Inputs)> scripts)
        {
            var results = new List<DynamoExecutionResult>();

            foreach (var (path, inputs) in scripts)
            {
                results.Add(RunScript(path, inputs));
            }

            return results;
        }

        // ══════════════════════════════════════════════════════════
        //  CARREGAR WORKSPACE
        // ══════════════════════════════════════════════════════════

        private HomeWorkspaceModel? LoadWorkspace(string path)
        {
            try
            {
                _dynamoModel.ExecuteCommand(
                    new DynamoModel.OpenFileCommand(path));

                var workspace = _dynamoModel.CurrentWorkspace as HomeWorkspaceModel;

                if (workspace == null)
                    return null;

                workspace.RunSettings.RunType = RunType.Manual;
                workspace.RunSettings.RunEnabled = true;

                return workspace;
            }
            catch
            {
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  INJETAR INPUTS
        // ══════════════════════════════════════════════════════════

        private (bool Success, string Message) InjectInputs(
            HomeWorkspaceModel workspace,
            Dictionary<string, object> inputs)
        {
            var inputNodes = workspace.Nodes
                .Where(n => IsInputNode(n))
                .ToList();

            int injected = 0;

            foreach (var kv in inputs)
            {
                var nodeName = kv.Key;
                var value = kv.Value;

                var targetNode = inputNodes.FirstOrDefault(n =>
                    string.Equals(GetNodeDisplayName(n), nodeName,
                        StringComparison.OrdinalIgnoreCase));

                if (targetNode == null)
                {
                    targetNode = inputNodes.FirstOrDefault(n =>
                        n.Name.IndexOf(nodeName,
                            StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (targetNode == null)
                    continue;

                try
                {
                    InjectValueIntoNode(targetNode, value);
                    injected++;
                }
                catch (Exception ex)
                {
                    return (false,
                        $"Failed to inject '{nodeName}': {ex.Message}");
                }
            }

            return (true, $"{injected}/{inputs.Count} inputs injected.");
        }

        private static bool IsInputNode(NodeModel node)
        {
            var typeName = node.GetType().Name;

            return typeName is
                "StringInput" or
                "DoubleInput" or
                "IntegerSlider" or
                "DoubleSlider" or
                "BoolSelector" or
                "FilePath" or
                "Directory" or
                "CodeBlockNodeModel" or
                "StringNode" or
                "NumberNode";
        }

        private static string GetNodeDisplayName(NodeModel node)
        {
            if (!string.IsNullOrEmpty(node.Name))
                return node.Name;

            return node.GetType().Name;
        }

        private static void InjectValueIntoNode(NodeModel node, object value)
        {
            var nodeType = node.GetType();
            var typeName = nodeType.Name;

            switch (typeName)
            {
                case "StringInput":
                case "StringNode":
                    SetNodeProperty(node, "Value",
                        value?.ToString() ?? "");
                    break;

                case "DoubleInput":
                case "NumberNode":
                    SetNodeProperty(node, "Value",
                        Convert.ToDouble(value,
                            System.Globalization.CultureInfo.InvariantCulture)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;

                case "IntegerSlider":
                    SetNodeProperty(node, "Value",
                        Convert.ToInt32(value));
                    break;

                case "DoubleSlider":
                    SetNodeProperty(node, "Value",
                        Convert.ToDouble(value,
                            System.Globalization.CultureInfo.InvariantCulture));
                    break;

                case "BoolSelector":
                    SetNodeProperty(node, "Value",
                        Convert.ToBoolean(value)
                            .ToString()
                            .ToLowerInvariant());
                    break;

                case "FilePath":
                case "Directory":
                    SetNodeProperty(node, "Value",
                        value?.ToString() ?? "");
                    break;

                case "CodeBlockNodeModel":
                    SetNodeProperty(node, "Code",
                        FormatCodeBlockValue(value));
                    break;

                default:
                    SetNodeProperty(node, "Value",
                        value?.ToString() ?? "");
                    break;
            }

            node.MarkNodeAsModified(forceExecute: true);
        }

        private static void SetNodeProperty(
            NodeModel node, string propertyName, object value)
        {
            var prop = node.GetType().GetProperty(propertyName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(node, value);
                return;
            }

            var field = node.GetType().GetField(propertyName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(node, value);
            }
        }

        private static string FormatCodeBlockValue(object value)
        {
            if (value is string s)
                return $"\"{s.Replace("\"", "\\\"")}\"";

            if (value is bool b)
                return b ? "true" : "false";

            if (value is double or int or long or float or decimal)
                return Convert.ToDouble(value,
                    System.Globalization.CultureInfo.InvariantCulture)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);

            return value?.ToString() ?? "null";
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUTAR E AGUARDAR
        // ══════════════════════════════════════════════════════════

        private (bool Success, string Message) RunAndWait(
            HomeWorkspaceModel workspace,
            int timeoutMs)
        {
            try
            {
                var tcs = new TaskCompletionSource<bool>();

                void OnEvaluationCompleted(object? sender,
                    EvaluationCompletedEventArgs e)
                {
                    if (e.EvaluationSucceeded)
                        tcs.TrySetResult(true);
                    else
                        tcs.TrySetResult(false);
                }

                workspace.EvaluationCompleted += OnEvaluationCompleted;

                try
                {
                    _dynamoModel.ExecuteCommand(
                        new DynamoModel.RunCancelCommand(false, false));

                    var completed = tcs.Task.Wait(timeoutMs);

                    if (!completed)
                    {
                        return (false,
                            $"Execution timed out after {timeoutMs}ms.");
                    }

                    if (!tcs.Task.Result)
                    {
                        return (false,
                            "Dynamo evaluation failed.");
                    }

                    return (true, "OK");
                }
                finally
                {
                    workspace.EvaluationCompleted -= OnEvaluationCompleted;
                }
            }
            catch (Exception ex)
            {
                return (false, $"Run error: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRAIR SAÍDA
        // ══════════════════════════════════════════════════════════

        private static string? ExtractOutput(
            HomeWorkspaceModel workspace,
            string outputNodeName)
        {
            // Buscar por nome exato
            var outputNode = workspace.Nodes
                .FirstOrDefault(n =>
                    string.Equals(GetNodeDisplayName(n), outputNodeName,
                        StringComparison.OrdinalIgnoreCase));

            // Fallback: buscar por nome parcial
            if (outputNode == null)
            {
                outputNode = workspace.Nodes
                    .FirstOrDefault(n =>
                        GetNodeDisplayName(n).IndexOf(outputNodeName,
                            StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Fallback: buscar último Watch node
            if (outputNode == null)
            {
                outputNode = workspace.Nodes
                    .LastOrDefault(n =>
                        n.GetType().Name == "Watch" ||
                        n.GetType().Name == "WatchNode");
            }

            if (outputNode == null)
                return null;

            return ExtractNodeValue(outputNode);
        }

        private static string? ExtractNodeValue(NodeModel node)
        {
            // Tentar CachedValue
            try
            {
                var cachedProp = node.GetType().GetProperty("CachedValue",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (cachedProp != null)
                {
                    var mirrorData = cachedProp.GetValue(node);

                    if (mirrorData != null)
                    {
                        var dataProp = mirrorData.GetType().GetProperty("Data");
                        if (dataProp != null)
                        {
                            var data = dataProp.GetValue(mirrorData);
                            if (data != null)
                                return data.ToString();
                        }

                        var stringProp = mirrorData.GetType()
                            .GetProperty("StringData");
                        if (stringProp != null)
                        {
                            var str = stringProp.GetValue(mirrorData);
                            if (str != null)
                                return str.ToString();
                        }

                        return mirrorData.ToString();
                    }
                }
            }
            catch { /* fallback abaixo */ }

            // Tentar Value direto
            try
            {
                var valueProp = node.GetType().GetProperty("Value",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (valueProp != null)
                {
                    var val = valueProp.GetValue(node);
                    return val?.ToString();
                }
            }
            catch { /* fallback abaixo */ }

            // Tentar OutputPorts
            try
            {
                var outPorts = node.OutPorts;
                if (outPorts != null && outPorts.Any())
                {
                    var firstPort = outPorts.First();
                    var portValueProp = firstPort.GetType()
                        .GetProperty("DefaultValue");

                    if (portValueProp != null)
                    {
                        var val = portValueProp.GetValue(firstPort);
                        return val?.ToString();
                    }
                }
            }
            catch { /* sem output */ }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  UTILITÁRIOS
        // ══════════════════════════════════════════════════════════

        public List<string> ListInputNodes(string scriptPath)
        {
            var names = new List<string>();

            try
            {
                var workspace = LoadWorkspace(scriptPath);
                if (workspace == null) return names;

                names = workspace.Nodes
                    .Where(IsInputNode)
                    .Select(GetNodeDisplayName)
                    .OrderBy(n => n)
                    .ToList();
            }
            catch { /* silencioso */ }

            return names;
        }

        public List<(string Name, string Type)> ListAllNodes(string scriptPath)
        {
            var nodes = new List<(string, string)>();

            try
            {
                var workspace = LoadWorkspace(scriptPath);
                if (workspace == null) return nodes;

                nodes = workspace.Nodes
                    .Select(n => (GetNodeDisplayName(n), n.GetType().Name))
                    .OrderBy(x => x.Item1)
                    .ToList();
            }
            catch { /* silencioso */ }

            return nodes;
        }

        public bool ValidateScript(string scriptPath, out string errorMessage)
        {
            errorMessage = "";

            if (!File.Exists(scriptPath))
            {
                errorMessage = $"File not found: {scriptPath}";
                return false;
            }

            if (!scriptPath.EndsWith(".dyn", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "File must be a .dyn workspace.";
                return false;
            }

            try
            {
                var workspace = LoadWorkspace(scriptPath);
                if (workspace == null)
                {
                    errorMessage = "Failed to load workspace.";
                    return false;
                }

                if (!workspace.Nodes.Any())
                {
                    errorMessage = "Workspace has no nodes.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
