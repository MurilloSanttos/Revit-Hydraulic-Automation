using System.Collections;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Revit2026.Modules.DynamoIntegration.Serialization
{
    public class SerializedInput
    {
        [JsonProperty("nodeId")]
        public string NodeId { get; set; } = "";

        [JsonProperty("dataType")]
        public string DataType { get; set; } = "null";

        [JsonProperty("value")]
        public object? Value { get; set; }

        public string ToJson(bool indented = false)
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                Culture = CultureInfo.InvariantCulture
            });
        }

        public override string ToString() =>
            $"[{NodeId}] {DataType} = {JsonConvert.SerializeObject(Value)}";
    }

    public class SerializedInputBatch
    {
        [JsonProperty("inputs")]
        public List<SerializedInput> Inputs { get; set; } = new();

        public string ToJson(bool indented = false)
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                Culture = CultureInfo.InvariantCulture
            });
        }

        public static SerializedInputBatch FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SerializedInputBatch>(json)
                ?? new SerializedInputBatch();
        }
    }

    public static class InputParameterSerializer
    {
        private static readonly HashSet<Type> PrimitiveTypes = new()
        {
            typeof(string),
            typeof(bool),
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal),
            typeof(char)
        };

        // ══════════════════════════════════════════════════════════
        //  SERIALIZE INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        public static SerializedInput Serialize(string nodeId, object? value)
        {
            if (string.IsNullOrEmpty(nodeId))
                throw new ArgumentNullException(nameof(nodeId));

            var normalized = NormalizeValue(value);
            var dataType = GetDataType(value);

            return new SerializedInput
            {
                NodeId = nodeId,
                DataType = dataType,
                Value = normalized
            };
        }

        public static SerializedInput Serialize(string nodeId, object? value, string dataType)
        {
            if (string.IsNullOrEmpty(nodeId))
                throw new ArgumentNullException(nameof(nodeId));

            return new SerializedInput
            {
                NodeId = nodeId,
                DataType = dataType,
                Value = NormalizeValue(value)
            };
        }

        // ══════════════════════════════════════════════════════════
        //  SERIALIZE BATCH
        // ══════════════════════════════════════════════════════════

        public static SerializedInputBatch SerializeBatch(
            params (string NodeId, object? Value)[] items)
        {
            var batch = new SerializedInputBatch();

            foreach (var (nodeId, value) in items)
            {
                batch.Inputs.Add(Serialize(nodeId, value));
            }

            return batch;
        }

        public static SerializedInputBatch SerializeBatch(
            IDictionary<string, object?> nodeValues)
        {
            var batch = new SerializedInputBatch();

            foreach (var (nodeId, value) in nodeValues.OrderBy(kv => kv.Key))
            {
                batch.Inputs.Add(Serialize(nodeId, value));
            }

            return batch;
        }

        // ══════════════════════════════════════════════════════════
        //  SERIALIZE TIPADOS
        // ══════════════════════════════════════════════════════════

        public static SerializedInput SerializeString(string nodeId, string value) =>
            new() { NodeId = nodeId, DataType = "string", Value = value ?? "" };

        public static SerializedInput SerializeNumber(string nodeId, double value) =>
            new() { NodeId = nodeId, DataType = "number", Value = NormalizeNumber(value) };

        public static SerializedInput SerializeInt(string nodeId, int value) =>
            new() { NodeId = nodeId, DataType = "number", Value = (double)value };

        public static SerializedInput SerializeBool(string nodeId, bool value) =>
            new() { NodeId = nodeId, DataType = "boolean", Value = value };

        public static SerializedInput SerializeList(string nodeId, IEnumerable values) =>
            new() { NodeId = nodeId, DataType = "list", Value = SerializeList(values) };

        public static SerializedInput SerializeDict(string nodeId, IDictionary<string, object> dict) =>
            new() { NodeId = nodeId, DataType = "dict", Value = SerializeDictionary(dict) };

        public static SerializedInput SerializeElementIds(string nodeId, IEnumerable<long> ids) =>
            new()
            {
                NodeId = nodeId,
                DataType = "elementId",
                Value = ids.Select(id => (object)(double)id).ToList()
            };

        public static SerializedInput SerializeXyz(string nodeId, double x, double y, double z) =>
            new()
            {
                NodeId = nodeId,
                DataType = "xyz",
                Value = new SortedDictionary<string, object>
                {
                    ["x"] = NormalizeNumber(x),
                    ["y"] = NormalizeNumber(y),
                    ["z"] = NormalizeNumber(z)
                }
            };

        public static SerializedInput SerializeNull(string nodeId) =>
            new() { NodeId = nodeId, DataType = "null", Value = null };

        // ══════════════════════════════════════════════════════════
        //  NORMALIZAÇÃO
        // ══════════════════════════════════════════════════════════

        private static object? NormalizeValue(object? value)
        {
            if (value == null || value is DBNull)
                return null;

            var type = value.GetType();

            // String
            if (value is string s)
                return s;

            // Boolean
            if (value is bool)
                return value;

            // Números → double
            if (IsNumericType(type))
                return NormalizeNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));

            // Char → string
            if (value is char c)
                return c.ToString();

            // Enum → string
            if (type.IsEnum)
                return value.ToString();

            // Guid → string
            if (value is Guid g)
                return g.ToString();

            // DateTime → ISO-8601
            if (value is DateTime dt)
                return dt.ToString("o", CultureInfo.InvariantCulture);

            // DateTimeOffset → ISO-8601
            if (value is DateTimeOffset dto)
                return dto.ToString("o", CultureInfo.InvariantCulture);

            // JToken (Newtonsoft)
            if (value is JToken token)
                return NormalizeJToken(token);

            // Dictionary<string, *>
            if (value is IDictionary<string, object> dictSO)
                return SerializeDictionary(dictSO);

            if (value is IDictionary<string, string> dictSS)
                return SerializeDictionary(dictSS.ToDictionary(
                    kv => kv.Key, kv => (object)kv.Value));

            // IDictionary genérico
            if (value is IDictionary dict)
                return SerializeGenericDictionary(dict);

            // IEnumerable (arrays, lists, etc.)
            if (value is IEnumerable enumerable)
                return SerializeList(enumerable);

            // Objeto complexo → converter para dicionário de propriedades
            return SerializeComplexObject(value);
        }

        private static double NormalizeNumber(double value)
        {
            if (double.IsNaN(value)) return 0.0;
            if (double.IsPositiveInfinity(value)) return double.MaxValue;
            if (double.IsNegativeInfinity(value)) return double.MinValue;
            return value;
        }

        // ══════════════════════════════════════════════════════════
        //  SERIALIZAÇÃO DE PRIMITIVOS
        // ══════════════════════════════════════════════════════════

        private static object SerializePrimitive(object value)
        {
            if (value is bool) return value;
            if (value is string s) return s;
            if (IsNumericType(value.GetType()))
                return NormalizeNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            if (value is char c) return c.ToString();
            if (value is Guid g) return g.ToString();
            if (value is DateTime dt) return dt.ToString("o", CultureInfo.InvariantCulture);
            return value.ToString() ?? "";
        }

        // ══════════════════════════════════════════════════════════
        //  SERIALIZAÇÃO DE LISTAS
        // ══════════════════════════════════════════════════════════

        private static List<object?> SerializeList(IEnumerable values)
        {
            var result = new List<object?>();

            foreach (var item in values)
            {
                result.Add(NormalizeValue(item));
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  SERIALIZAÇÃO DE DICIONÁRIOS
        // ══════════════════════════════════════════════════════════

        private static SortedDictionary<string, object?> SerializeDictionary(
            IDictionary<string, object> dict)
        {
            var sorted = new SortedDictionary<string, object?>(
                StringComparer.Ordinal);

            foreach (var kv in dict)
            {
                sorted[kv.Key] = NormalizeValue(kv.Value);
            }

            return sorted;
        }

        private static SortedDictionary<string, object?> SerializeGenericDictionary(
            IDictionary dict)
        {
            var sorted = new SortedDictionary<string, object?>(
                StringComparer.Ordinal);

            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? "";
                sorted[key] = NormalizeValue(entry.Value);
            }

            return sorted;
        }

        // ══════════════════════════════════════════════════════════
        //  SERIALIZAÇÃO DE OBJETOS COMPLEXOS
        // ══════════════════════════════════════════════════════════

        private static SortedDictionary<string, object?> SerializeComplexObject(
            object obj)
        {
            var sorted = new SortedDictionary<string, object?>(
                StringComparer.Ordinal);

            var type = obj.GetType();
            var properties = type.GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            foreach (var prop in properties.OrderBy(p => p.Name))
            {
                if (!prop.CanRead) continue;

                try
                {
                    var value = prop.GetValue(obj);
                    sorted[prop.Name] = NormalizeValue(value);
                }
                catch
                {
                    sorted[prop.Name] = null;
                }
            }

            return sorted;
        }

        // ══════════════════════════════════════════════════════════
        //  NORMALIZE JTOKEN
        // ══════════════════════════════════════════════════════════

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
                    return NormalizeNumber(token.Value<double>());

                case JTokenType.Array:
                    var list = new List<object?>();
                    foreach (var item in (JArray)token)
                        list.Add(NormalizeJToken(item));
                    return list;

                case JTokenType.Object:
                    var dict = new SortedDictionary<string, object?>(
                        StringComparer.Ordinal);
                    foreach (var prop in ((JObject)token).Properties().OrderBy(p => p.Name))
                        dict[prop.Name] = NormalizeJToken(prop.Value);
                    return dict;

                default:
                    return token.ToString();
            }
        }

        // ══════════════════════════════════════════════════════════
        //  DETECÇÃO DE TIPO
        // ══════════════════════════════════════════════════════════

        private static string GetDataType(object? value)
        {
            if (value == null || value is DBNull)
                return "null";

            var type = value.GetType();

            if (value is string)
                return "string";

            if (value is bool)
                return "boolean";

            if (IsNumericType(type))
                return "number";

            if (value is char)
                return "string";

            if (value is Guid || value is DateTime || value is DateTimeOffset)
                return "string";

            if (type.IsEnum)
                return "string";

            if (value is IDictionary)
                return "dict";

            if (value is IEnumerable)
                return "list";

            return "dict";
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        // ══════════════════════════════════════════════════════════
        //  DESERIALIZAÇÃO (RESPONSE → TIPADO)
        // ══════════════════════════════════════════════════════════

        public static T? DeserializeOutput<T>(object? value)
        {
            if (value == null)
                return default;

            if (value is T typed)
                return typed;

            if (value is JToken token)
                return token.ToObject<T>();

            try
            {
                var json = JsonConvert.SerializeObject(value);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }

        public static string? DeserializeString(object? value) =>
            value?.ToString();

        public static double DeserializeDouble(object? value)
        {
            if (value == null) return 0.0;
            if (value is double d) return d;
            if (value is long l) return l;
            if (value is int i) return i;
            if (double.TryParse(value.ToString(), NumberStyles.Any,
                CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return 0.0;
        }

        public static bool DeserializeBool(object? value)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (bool.TryParse(value.ToString(), out var parsed))
                return parsed;
            return false;
        }

        public static List<long> DeserializeElementIds(object? value)
        {
            if (value == null) return new();

            if (value is JArray jArray)
                return jArray.Select(t => t.Value<long>()).ToList();

            if (value is IEnumerable<object> list)
                return list.Select(v => Convert.ToInt64(v, CultureInfo.InvariantCulture)).ToList();

            return new();
        }

        public static (double X, double Y, double Z) DeserializeXyz(object? value)
        {
            if (value == null) return (0, 0, 0);

            if (value is JObject jObj)
            {
                return (
                    jObj["x"]?.Value<double>() ?? 0,
                    jObj["y"]?.Value<double>() ?? 0,
                    jObj["z"]?.Value<double>() ?? 0);
            }

            if (value is IDictionary<string, object> dict)
            {
                return (
                    dict.ContainsKey("x") ? Convert.ToDouble(dict["x"]) : 0,
                    dict.ContainsKey("y") ? Convert.ToDouble(dict["y"]) : 0,
                    dict.ContainsKey("z") ? Convert.ToDouble(dict["z"]) : 0);
            }

            return (0, 0, 0);
        }
    }
}
