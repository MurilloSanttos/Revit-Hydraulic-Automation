using PluginCore.Domain.Enums;

namespace PluginCore.Models;

/// <summary>
/// Sistema MEP completo — agrupa trechos, prumadas e pontos de um sistema.
/// </summary>
public class SistemaMEP
{
    /// <summary>Identificador único.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Tipo do sistema.</summary>
    public HydraulicSystem Tipo { get; set; }

    /// <summary>Nome descritivo.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Trechos de tubulação do sistema.</summary>
    public List<TrechoTubulacao> Trechos { get; set; } = new();

    /// <summary>Prumadas do sistema.</summary>
    public List<Prumada> Prumadas { get; set; } = new();

    /// <summary>Pontos hidráulicos do sistema.</summary>
    public List<PontoHidraulico> Pontos { get; set; } = new();

    /// <summary>Total de pontos de consumo.</summary>
    public int TotalPontos => Pontos.Count;

    /// <summary>Soma total de pesos (UHC).</summary>
    public double SomaPesosTotal => Pontos.Sum(p => p.PesoRelativo);

    /// <summary>Comprimento total da rede (metros).</summary>
    public double ComprimentoTotal => Trechos.Sum(t => t.ComprimentoTotal);

    /// <summary>Se o sistema foi dimensionado.</summary>
    public bool Dimensionado { get; set; }

    /// <summary>Resultado do dimensionamento (se disponível).</summary>
    public ResultadoDimensionamento? Resultado { get; set; }
}
