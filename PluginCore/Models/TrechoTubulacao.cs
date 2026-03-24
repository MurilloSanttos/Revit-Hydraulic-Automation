using PluginCore.Domain.Enums;

namespace PluginCore.Models;

/// <summary>
/// Trecho de tubulação entre dois pontos.
/// </summary>
public class TrechoTubulacao
{
    /// <summary>Identificador único do trecho.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Sistema (AF, ES, VE).</summary>
    public HydraulicSystem Sistema { get; set; }

    /// <summary>Material do tubo.</summary>
    public PipeMaterial Material { get; set; }

    /// <summary>Diâmetro nominal (mm).</summary>
    public int DiametroNominal { get; set; }

    /// <summary>Diâmetro interno (mm).</summary>
    public double DiametroInterno { get; set; }

    /// <summary>Comprimento do trecho (metros).</summary>
    public double Comprimento { get; set; }

    /// <summary>Declividade (% — apenas esgoto).</summary>
    public double Declividade { get; set; }

    /// <summary>Vazão de projeto (L/s).</summary>
    public double Vazao { get; set; }

    /// <summary>Velocidade (m/s).</summary>
    public double Velocidade { get; set; }

    /// <summary>Perda de carga unitária (m/m).</summary>
    public double PerdaCargaUnitaria { get; set; }

    /// <summary>Perda de carga total do trecho (m).</summary>
    public double PerdaCargaTotal { get; set; }

    /// <summary>Soma dos pesos relativos no trecho.</summary>
    public double SomaPesos { get; set; }

    /// <summary>ID do ponto de origem.</summary>
    public string PontoOrigemId { get; set; } = string.Empty;

    /// <summary>ID do ponto de destino.</summary>
    public string PontoDestinoId { get; set; } = string.Empty;

    /// <summary>Lista de conexões (joelhos, tês) no trecho.</summary>
    public List<ConnectionType> Conexoes { get; set; } = new();

    /// <summary>Comprimento equivalente das conexões (metros).</summary>
    public double ComprimentoEquivalente { get; set; }

    /// <summary>Comprimento total (real + equivalente).</summary>
    public double ComprimentoTotal => Comprimento + ComprimentoEquivalente;

    /// <summary>Se o trecho possui ponto de ventilação conectado.</summary>
    public bool PossuiVentilacao { get; set; }

    /// <summary>Diâmetro da ventilação conectada (mm).</summary>
    public int DiametroVentilacao { get; set; }
}
