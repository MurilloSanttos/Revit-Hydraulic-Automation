using PluginCore.Domain.Enums;

namespace PluginCore.Models;

/// <summary>
/// Ponto hidráulico — ligação entre equipamento e rede.
/// </summary>
public class PontoHidraulico
{
    /// <summary>Identificador único.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID do equipamento associado.</summary>
    public string EquipamentoId { get; set; } = string.Empty;

    /// <summary>Tipo do equipamento.</summary>
    public EquipmentType TipoEquipamento { get; set; }

    /// <summary>Sistema hidráulico (AF, ES, VE).</summary>
    public HydraulicSystem Sistema { get; set; }

    /// <summary>Diâmetro nominal (mm).</summary>
    public int DiametroNominal { get; set; }

    /// <summary>Peso relativo (UHC) — NBR 5626.</summary>
    public double PesoRelativo { get; set; }

    /// <summary>ID do ambiente.</summary>
    public string AmbienteId { get; set; } = string.Empty;

    /// <summary>Nível.</summary>
    public string Nivel { get; set; } = string.Empty;

    /// <summary>Posição X (metros).</summary>
    public double PosX { get; set; }

    /// <summary>Posição Y (metros).</summary>
    public double PosY { get; set; }

    /// <summary>Posição Z (metros).</summary>
    public double PosZ { get; set; }
}
