namespace PluginCore.Common;

/// <summary>
/// Contrato JSON para comunicação com Dynamo.
/// Representa os dados enviados/recebidos por script.
/// </summary>
public class DynamoPayload
{
    /// <summary>ID do script Dynamo.</summary>
    public string ScriptId { get; set; } = string.Empty;

    /// <summary>Dados de entrada (key-value).</summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>Dados de saída (preenchido pelo Dynamo).</summary>
    public Dictionary<string, object> OutputData { get; set; } = new();

    /// <summary>Se a execução foi bem-sucedida.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Mensagem de erro (se houver).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Timestamp de criação.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Configuração do projeto hidráulico (persistida em JSON).
/// </summary>
public class ProjectConfig
{
    /// <summary>Nome do projeto.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Norma de água fria.</summary>
    public string ColdWaterStandard { get; set; } = "NBR 5626:2020";

    /// <summary>Norma de esgoto.</summary>
    public string SewerStandard { get; set; } = "NBR 8160:1999";

    /// <summary>Material padrão para água fria.</summary>
    public string DefaultColdWaterMaterial { get; set; } = "PVC";

    /// <summary>Material padrão para esgoto.</summary>
    public string DefaultSewerMaterial { get; set; } = "PVC";

    /// <summary>Pressão de alimentação (mca).</summary>
    public double SupplyPressureMca { get; set; } = 15.0;

    /// <summary>Velocidade máxima permitida (m/s).</summary>
    public double MaxVelocityMs { get; set; } = 3.0;

    /// <summary>Diretório de logs.</summary>
    public string LogDirectory { get; set; } = string.Empty;

    /// <summary>Habilitar debug verbose.</summary>
    public bool VerboseDebug { get; set; }
}
