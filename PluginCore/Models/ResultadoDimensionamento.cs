using PluginCore.Domain.Enums;

namespace PluginCore.Models;

/// <summary>
/// Resultado do dimensionamento hidráulico de um sistema ou trecho.
/// </summary>
public class ResultadoDimensionamento
{
    /// <summary>ID do sistema ou trecho dimensionado.</summary>
    public string ReferenciaId { get; set; } = string.Empty;

    /// <summary>Tipo do sistema.</summary>
    public HydraulicSystem Sistema { get; set; }

    /// <summary>Se o dimensionamento foi aprovado.</summary>
    public bool Aprovado { get; set; }

    /// <summary>Vazão de projeto (L/s).</summary>
    public double VazaoProjeto { get; set; }

    /// <summary>Diâmetro recomendado (mm).</summary>
    public int DiametroRecomendado { get; set; }

    /// <summary>Velocidade resultante (m/s).</summary>
    public double Velocidade { get; set; }

    /// <summary>Pressão disponível no ponto mais desfavorável (mca).</summary>
    public double PressaoDisponivel { get; set; }

    /// <summary>Pressão mínima requerida (mca).</summary>
    public double PressaoMinima { get; set; }

    /// <summary>Perda de carga total do percurso (m).</summary>
    public double PerdaCargaTotal { get; set; }

    /// <summary>Norma utilizada no cálculo.</summary>
    public string NormaUtilizada { get; set; } = string.Empty;

    /// <summary>Alertas gerados durante o dimensionamento.</summary>
    public List<string> Alertas { get; set; } = new();

    /// <summary>Timestamp do cálculo.</summary>
    public DateTime CalculadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Pressão é suficiente?</summary>
    public bool PressaoSuficiente => PressaoDisponivel >= PressaoMinima;

    /// <summary>Velocidade dentro do limite (≤ 3.0 m/s)?</summary>
    public bool VelocidadeOk => Velocidade <= 3.0;
}
