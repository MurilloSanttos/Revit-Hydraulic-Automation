using PluginCore.Models;
using PluginCore.Domain.Enums;

namespace PluginCore.Interfaces;

/// <summary>
/// Serviço de gerenciamento de redes hidráulicas (AF, ES, VE).
/// </summary>
public interface IRedeService
{
    /// <summary>Gera a rede de tubulação para um sistema.</summary>
    SistemaMEP GerarRede(HydraulicSystem sistema, List<PontoHidraulico> pontos,
        List<Prumada> prumadas);

    /// <summary>Valida conectividade da rede (todos os pontos conectados).</summary>
    bool ValidarConectividade(SistemaMEP sistema);

    /// <summary>Detecta loops na rede.</summary>
    List<string> DetectarLoops(SistemaMEP sistema);

    /// <summary>Calcula comprimento total da rede.</summary>
    double CalcularComprimentoTotal(SistemaMEP sistema);
}
