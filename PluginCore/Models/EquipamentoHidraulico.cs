using PluginCore.Domain.Enums;

namespace PluginCore.Models;

/// <summary>
/// Equipamento hidráulico (aparelho sanitário).
/// Representa uma peça com ponto de consumo.
/// </summary>
public class EquipamentoHidraulico
{
    /// <summary>Identificador único.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID do elemento no Revit (ElementId).</summary>
    public long RevitElementId { get; set; }

    /// <summary>Tipo do equipamento.</summary>
    public EquipmentType Tipo { get; set; }

    /// <summary>Nome da família Revit.</summary>
    public string FamilyName { get; set; } = string.Empty;

    /// <summary>Nome do tipo Revit.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Identificador do ambiente onde está posicionado.</summary>
    public string AmbienteId { get; set; } = string.Empty;

    /// <summary>Nome do ambiente.</summary>
    public string AmbienteNome { get; set; } = string.Empty;

    /// <summary>Nível (Level name).</summary>
    public string Nivel { get; set; } = string.Empty;

    /// <summary>Mark (identificador de instância).</summary>
    public string Mark { get; set; } = string.Empty;

    /// <summary>Peso relativo (UHC) — NBR 5626.</summary>
    public double PesoRelativo { get; set; }

    /// <summary>Sistemas que alimentam este equipamento.</summary>
    public List<HydraulicSystem> Sistemas { get; set; } = new();

    /// <summary>Diâmetro nominal da conexão de água fria (mm).</summary>
    public int DiametroAF { get; set; }

    /// <summary>Diâmetro nominal da conexão de esgoto (mm).</summary>
    public int DiametroES { get; set; }

    /// <summary>Posição X (metros).</summary>
    public double PosX { get; set; }

    /// <summary>Posição Y (metros).</summary>
    public double PosY { get; set; }

    /// <summary>Posição Z (metros).</summary>
    public double PosZ { get; set; }

    /// <summary>Se o equipamento foi processado pelo plugin.</summary>
    public bool Processado { get; set; }
}
