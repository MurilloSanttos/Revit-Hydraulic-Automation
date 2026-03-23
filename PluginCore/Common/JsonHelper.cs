using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PluginCore.Common;

/// <summary>
/// Utilitário centralizado para operações JSON.
/// Usa Newtonsoft.Json com configurações padronizadas.
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerSettings DefaultSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
    };

    private static readonly JsonSerializerSettings CompactSettings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    // ══════════════════════════════════════════════════════════
    //  SERIALIZAÇÃO
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Serializa objeto para JSON formatado (indentado).
    /// </summary>
    public static string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj, DefaultSettings);
    }

    /// <summary>
    /// Serializa objeto para JSON compacto (sem indentação).
    /// Útil para comunicação com Dynamo.
    /// </summary>
    public static string SerializeCompact(object obj)
    {
        return JsonConvert.SerializeObject(obj, CompactSettings);
    }

    // ══════════════════════════════════════════════════════════
    //  DESERIALIZAÇÃO
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Deserializa JSON para objeto tipado.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
    }

    /// <summary>
    /// Tenta deserializar JSON. Retorna false se falhar.
    /// </summary>
    public static bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = JsonConvert.DeserializeObject<T>(json, DefaultSettings);
            return result is not null;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  ARQUIVOS
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Serializa e salva em arquivo.
    /// </summary>
    public static void SaveToFile(object obj, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = Serialize(obj);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Lê e deserializa de arquivo.
    /// </summary>
    public static T? LoadFromFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
            return default;

        var json = File.ReadAllText(filePath);
        return Deserialize<T>(json);
    }

    /// <summary>
    /// Tenta ler e deserializar de arquivo.
    /// </summary>
    public static bool TryLoadFromFile<T>(string filePath, out T? result)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                result = default;
                return false;
            }

            var json = File.ReadAllText(filePath);
            return TryDeserialize(json, out result);
        }
        catch
        {
            result = default;
            return false;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  CLONE
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Deep clone via serialização JSON.
    /// </summary>
    public static T? DeepClone<T>(T obj)
    {
        var json = JsonConvert.SerializeObject(obj, CompactSettings);
        return JsonConvert.DeserializeObject<T>(json, CompactSettings);
    }
}
