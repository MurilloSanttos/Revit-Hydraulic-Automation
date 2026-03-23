using PluginCore.Models;

namespace PluginCore.Interfaces;

/// <summary>
/// Serviço de exportação — gera tabelas, quantitativos e pranchas.
/// </summary>
public interface IExportService
{
    /// <summary>Gera tabela de dimensionamento de água fria.</summary>
    Task GerarTabelaAguaFriaAsync(SistemaMEP sistema, string outputPath);

    /// <summary>Gera tabela de dimensionamento de esgoto.</summary>
    Task GerarTabelaEsgotoAsync(SistemaMEP sistema, string outputPath);

    /// <summary>Gera quantitativo de materiais.</summary>
    Task GerarQuantitativoAsync(List<SistemaMEP> sistemas, string outputPath);

    /// <summary>Gera prancha final do projeto.</summary>
    Task GerarPranchaAsync(string templatePath, string outputPath,
        Dictionary<string, object> dados);

    /// <summary>Exporta relatório completo em JSON.</summary>
    Task ExportarRelatorioJsonAsync(List<SistemaMEP> sistemas, string outputPath);
}
