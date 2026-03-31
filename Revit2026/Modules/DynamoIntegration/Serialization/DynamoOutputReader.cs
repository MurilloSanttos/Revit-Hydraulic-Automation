using System.Collections;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Revit2026.Modules.DynamoIntegration.Serialization
{
    public class DynamoOutputResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public object? Data { get; set; }
        public long ParseTimeMs { get; set; }
        public int NodeCount { get; set; }

        public T? GetData<T>()
        {
            if (Data == null) return default;
            if (Data is T typed) return typed;

            try
            {
                var json = JsonConvert.SerializeObject(Data);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }

        public string? GetString(string key)
        {
            if (Data is IDictionary<string, object?> dict &&
                dict.TryGetValue(key, out var val))
                return val?.ToString();
            return null;
        }

        public double GetDouble(string key, double fallback = 0.0)
        {
            if (Data is IDictionary<string, object?> dict &&
                dict.TryGetValue(key, out var val))
            {
                if (val is double d) return d;
                if (double.TryParse(val?.ToString(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }
            return fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            if (Data is IDictionary<string, object?> dict &&
                dict.TryGetValue(key, out var val))
            {
                if (val is bool b) return b;
                if (bool.TryParse(val?.ToString(), out var parsed))
                    return parsed;
            }
            return fallback;
        }

        public List<object?> GetList(string key)
        {
            if (Data is IDictionary<string, object?> dict &&
                dict.TryGetValue(key, out var val) &&
                val is List<object?> list)
                return list;
            return new();
        }

        public List<long> GetElementIds(string key)
        {
            var list = GetList(key);
            var ids = new List<long>();

            foreach (var item in list)
            {
                if (item is double d)
                    ids.Add((long)d);
                else if (item is long l)
                    ids.Add(l);
                else if (long.TryParse(item?.ToString(), out var parsed))
                    ids.Add(parsed);
            }

            return ids;
        }

        public (double X, double Y, double Z) GetXyz(string key)
        {
            if (Data is IDictionary<string, object?> dict &&
                dict.TryGetValue(key, out var val) &&
                val is IDictionary<string, object?> xyz)
            {
                double x = 0, y = 0, z = 0;

                if (xyz.TryGetValue("x", out var vx) && vx is double dx) x = dx;
                if (xyz.TryGetValue("y", out var vy) && vy is double dy) y = dy;
                if (xyz.TryGetValue("z", out var vz) && vz is double dz) z = dz;

                return (x, y, z);
            }
            return (0, 0, 0);
        }

        public override string ToString() =>
            $"{(Success ? "OK" : "FAIL")}: {Message} " +
            $"({ParseTimeMs}ms, {NodeCount} nodes)";
    }

    public class DynamoOutputNodeResult
    {
        public string NodeId { get; set; } = "";
        public string? Alias { get; set; }
        public string DataType { get; set; } = "null";
        public object? Value { get; set; }
        public bool IsNull { get; set; }

        public T? GetValue<T>()
        {
            if (Value == null) return default;
            if (Value is T typed) return typed;

            try
            {
                var json = JsonConvert.SerializeObject(Value);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }

        public override string ToString() =>
            $"[{Alias ?? NodeId}] {DataType} = {JsonConvert.SerializeObject(Value)}";
    }

    public class DynamoOutputMultiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<DynamoOutputNodeResult> Outputs { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public long ExecutionTimeMs { get; set; }
        public int NodesExecuted { get; set; }

        public DynamoOutputNodeResult? GetByAlias(string alias) =>
            Outputs.FirstOrDefault(o =>
                string.Equals(o.Alias, alias, StringComparison.OrdinalIgnoreCase));

        public DynamoOutputNodeResult? GetByNodeId(string nodeId) =>
            Outputs.FirstOrDefault(o =>
                string.Equals(o.NodeId, nodeId, StringComparison.OrdinalIgnoreCase));

        public T? GetValue<T>(string aliasOrNodeId)
        {
            var node = GetByAlias(aliasOrNodeId) ?? GetByNodeId(aliasOrNodeId);
            return node != null ? node.GetValue<T>() : default;
        }

        public override string ToString() =>
            $"{(Success ? "OK" : "FAIL")}: {Outputs.Count} outputs, " +
            $"{Errors.Count} errors ({ExecutionTimeMs}ms)";
    }

    public static class DynamoOutputReader
    {
        // ══════════════════════════════════════════════════════════
        //  READ — FORMATO SIMPLES
        // ══════════════════════════════════════════════════════════

        public static DynamoOutputResult Read(string json)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(json))
            {
                return new DynamoOutputResult
                {
                    Success = false,
                    Message = "Empty JSON input."
                };
            }

            try
            {
                var parsed = ParseJson(json);

                if (parsed == null)
                {
                    return new DynamoOutputResult
                    {
                        Success = false,
                        Message = "Failed to parse JSON."
                    };
                }

                return BuildResult(parsed, sw);
            }
            catch (JsonException ex)
            {
                return new DynamoOutputResult
                {
                    Success = false,
                    Message = $"JSON parsing error: {ex.Message}",
                    ParseTimeMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                return new DynamoOutputResult
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}",
                    ParseTimeMs = sw.ElapsedMilliseconds
                };
            }
        }

        // ══════════════════════════════════════════════════════════
        //  READ — FORMATO PROTOCOLO (DynamoToPluginResponse)
        // ══════════════════════════════════════════════════════════

        public static DynamoOutputMultiResult ReadProtocol(string json)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(json))
            {
                return new DynamoOutputMultiResult
                {
                    Success = false,
                    Message = "Empty JSON input."
                };
            }

            try
            {
                var jObj = JObject.Parse(json);
                var result = new DynamoOutputMultiResult();

                var resultToken = jObj["result"];
                if (resultToken == null)
                {
                    result.Success = false;
                    result.Message = "Invalid protocol format: missing 'result'.";
                    return result;
                }

                result.Success = resultToken["success"]?.Value<bool>() ?? false;
                result.ExecutionTimeMs = resultToken["executionTimeMs"]?.Value<long>() ?? 0;
                result.NodesExecuted = resultToken["nodesExecuted"]?.Value<int>() ?? 0;

                // Outputs
                var outputsToken = resultToken["outputs"] as JArray;
                if (outputsToken != null)
                {
                    foreach (var outputToken in outputsToken)
                    {
                        result.Outputs.Add(new DynamoOutputNodeResult
                        {
                            NodeId = outputToken["nodeId"]?.Value<string>() ?? "",
                            Alias = outputToken["alias"]?.Value<string>(),
                            DataType = outputToken["dataType"]?.Value<string>() ?? "null",
                            Value = Normalize(outputToken["value"]),
                            IsNull = outputToken["isNull"]?.Value<bool>() ?? false
                        });
                    }
                }

                // Errors
                var errorsToken = resultToken["errors"] as JArray;
                if (errorsToken != null)
                {
                    foreach (var errToken in errorsToken)
                    {
                        var msg = errToken["message"]?.Value<string>();
                        var code = errToken["code"]?.Value<string>();
                        if (!string.IsNullOrEmpty(msg))
                            result.Errors.Add($"[{code}] {msg}");
                    }
                }

                // Warnings
                var warningsToken = resultToken["warnings"] as JArray;
                if (warningsToken != null)
                {
                    foreach (var warnToken in warningsToken)
                    {
                        var val = warnToken.Value<string>();
                        if (!string.IsNullOrEmpty(val))
                            result.Warnings.Add(val);
                    }
                }

                if (!result.Success && result.Errors.Count > 0)
                    result.Message = string.Join("; ", result.Errors);
                else
                    result.Message = "OK";

                return result;
            }
            catch (Exception ex)
            {
                return new DynamoOutputMultiResult
                {
                    Success = false,
                    Message = $"Protocol parsing error: {ex.Message}"
                };
            }
        }

        // ══════════════════════════════════════════════════════════
        //  READ — ARQUIVO
        // ══════════════════════════════════════════════════════════

        public static DynamoOutputResult ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new DynamoOutputResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                    return Read(json);
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }

            return new DynamoOutputResult
            {
                Success = false,
                Message = $"Could not read file after 3 attempts: {filePath}"
            };
        }

        public static DynamoOutputMultiResult ReadProtocolFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new DynamoOutputMultiResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                    return ReadProtocol(json);
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }

            return new DynamoOutputMultiResult
            {
                Success = false,
                Message = $"Could not read file after 3 attempts: {filePath}"
            };
        }

        // ══════════════════════════════════════════════════════════
        //  PARSE
        // ══════════════════════════════════════════════════════════

        private static JObject? ParseJson(string json)
        {
            var trimmed = json.Trim();

            if (!trimmed.StartsWith("{"))
                return null;

            return JObject.Parse(trimmed);
        }

        private static DynamoOutputResult BuildResult(
            JObject parsed,
            System.Diagnostics.Stopwatch sw)
        {
            var result = new DynamoOutputResult();

            // success
            var successToken = parsed["success"];
            if (successToken == null)
            {
                result.Success = false;
                result.Message = "Invalid Dynamo output format: missing 'success'.";
                result.ParseTimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            result.Success = successToken.Value<bool>();

            // message
            result.Message = parsed["message"]?.Value<string>() ?? "";

            // data
            var dataToken = parsed["data"];
            result.Data = dataToken != null ? Normalize(dataToken) : null;

            // nodeCount
            result.NodeCount = parsed["nodeCount"]?.Value<int>() ?? 0;

            result.ParseTimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  NORMALIZAÇÃO
        // ══════════════════════════════════════════════════════════

        private static object? Normalize(object? value)
        {
            if (value == null || value is DBNull)
                return null;

            if (value is JToken token)
                return NormalizeJToken(token);

            var type = value.GetType();

            if (value is string)
                return value;

            if (value is bool)
                return value;

            if (IsNumericType(type))
                return NormalizePrimitive(value);

            if (value is IDictionary<string, object> dictSO)
                return NormalizeDictionary(dictSO);

            if (value is IDictionary dict)
                return NormalizeGenericDictionary(dict);

            if (value is IEnumerable enumerable)
                return NormalizeList(enumerable);

            return NormalizePrimitive(value);
        }

        private static object? NormalizeJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                case JTokenType.String:
                    return token.Value<string>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Integer:
                    return (double)token.Value<long>();

                case JTokenType.Float:
                {
                    var d = token.Value<double>();
                    if (double.IsNaN(d)) return 0.0;
                    if (double.IsInfinity(d)) return 0.0;
                    return d;
                }

                case JTokenType.Array:
                    return NormalizeJArray((JArray)token);

                case JTokenType.Object:
                    return NormalizeJObject((JObject)token);

                default:
                    return token.ToString();
            }
        }

        private static List<object?> NormalizeJArray(JArray array)
        {
            var result = new List<object?>(array.Count);

            foreach (var item in array)
            {
                result.Add(NormalizeJToken(item));
            }

            return result;
        }

        private static SortedDictionary<string, object?> NormalizeJObject(JObject obj)
        {
            var sorted = new SortedDictionary<string, object?>(
                StringComparer.Ordinal);

            foreach (var prop in obj.Properties().OrderBy(p => p.Name))
            {
                sorted[prop.Name] = NormalizeJToken(prop.Value);
            }

            return sorted;
        }

        private static object NormalizePrimitive(object value)
        {
            if (value is bool) return value;
            if (value is string s) return s;

            if (IsNumericType(value.GetType()))
            {
                var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(d)) return 0.0;
                if (double.IsInfinity(d)) return 0.0;
                return d;
            }

            if (value is DateTime dt)
                return dt.ToString("o", CultureInfo.InvariantCulture);

            if (value is Guid g)
                return g.ToString();

            return value.ToString() ?? "";
        }

        private static List<object?> NormalizeList(IEnumerable list)
        {
            var result = new List<object?>();

            foreach (var item in list)
            {
                result.Add(Normalize(item));
            }

            return result;
        }

        private static SortedDictionary<string, object?> NormalizeDictionary(
            IDictionary<string, object> dict)
        {
            var sorted = new SortedDictionary<string, object?>(
                StringComparer.Ordinal);

            foreach (var kv in dict.OrderBy(x => x.Key))
            {
                sorted[kv.Key] = Normalize(kv.Value);
            }

            return sorted;
        }

        private static SortedDictionary<string, object?> NormalizeGenericDictionary(
            IDictionary dict)
        {
            var sorted = new SortedDictionary<string, object?>(
                StringComparer.Ordinal);

            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? "";
                sorted[key] = Normalize(entry.Value);
            }

            return sorted;
        }

        private static bool IsNumericType(Type type) =>
            type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) ||
            type == typeof(long) || type == typeof(ulong) ||
            type == typeof(float) || type == typeof(double) ||
            type == typeof(decimal);
    }
}
