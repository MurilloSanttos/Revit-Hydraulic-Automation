using PluginCore.Models;
using PluginCore.Domain.Enums;

namespace PluginCore.Interfaces;

/// <summary>
/// Serviço de dimensionamento hidráulico.
/// Aplica normas NBR 5626 (AF) e NBR 8160 (ES).
/// </summary>
public interface IDimensionamentoService
{
    /// <summary>Dimensiona um sistema completo.</summary>
    ResultadoDimensionamento Dimensionar(SistemaMEP sistema);

    /// <summary>Calcula vazão provável (método dos pesos).</summary>
    double CalcularVazaoProvavel(double somaPesos);

    /// <summary>Determina diâmetro mínimo para a vazão.</summary>
    int DeterminarDiametro(double vazao, HydraulicSystem sistema);

    /// <summary>Calcula perda de carga em um trecho.</summary>
    double CalcularPerdaCarga(TrechoTubulacao trecho);

    /// <summary>Verifica pressão disponível no ponto mais desfavorável.</summary>
    double VerificarPressao(SistemaMEP sistema, double pressaoAlimentacao);

    /// <summary>Calcula velocidade no trecho (m/s).</summary>
    double CalcularVelocidade(double vazao, double diametroInterno);
}
