using PluginCore.Domain.Enums;

namespace PluginCore.Models;

/// <summary>
/// Prumada — coluna vertical que conecta os andares.
/// </summary>
public class Prumada
{
    /// <summary>Identificador único.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Sistema (AF, ES, VE).</summary>
    public HydraulicSystem Sistema { get; set; }

    /// <summary>Diâmetro nominal (mm).</summary>
    public int DiametroNominal { get; set; }

    /// <summary>Material.</summary>
    public PipeMaterial Material { get; set; }

    /// <summary>Nível inferior (nome).</summary>
    public string NivelInferior { get; set; } = string.Empty;

    /// <summary>Nível superior (nome).</summary>
    public string NivelSuperior { get; set; } = string.Empty;

    /// <summary>Altura total (metros).</summary>
    public double Altura { get; set; }

    /// <summary>Posição X (metros).</summary>
    public double PosX { get; set; }

    /// <summary>Posição Y (metros).</summary>
    public double PosY { get; set; }

    /// <summary>IDs dos pontos hidráulicos conectados.</summary>
    public List<string> PontosConectados { get; set; } = new();

    /// <summary>IDs dos ambientes atendidos.</summary>
    public List<string> AmbientesAtendidos { get; set; } = new();

    /// <summary>Soma dos pesos (UHC) de todos os pontos.</summary>
    public double SomaPesos { get; set; }

    /// <summary>Vazão total (L/s).</summary>
    public double VazaoTotal { get; set; }
}
