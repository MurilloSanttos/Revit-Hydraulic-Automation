using PluginCore.Models;
using PluginCore.Domain.Enums;

namespace PluginCore.Interfaces;

/// <summary>
/// Serviço de gerenciamento de equipamentos hidráulicos.
/// Define operações de identificação, inserção e validação.
/// </summary>
public interface IEquipamentoService
{
    /// <summary>Identifica equipamentos por ambiente.</summary>
    List<EquipamentoHidraulico> IdentificarPorAmbiente(AmbienteInfo ambiente);

    /// <summary>Define equipamentos que devem ser inseridos com base no tipo de ambiente.</summary>
    List<EquipmentType> ObterEquipamentosEsperados(RoomType tipo);

    /// <summary>Valida se os equipamentos do ambiente estão completos.</summary>
    bool ValidarEquipamentos(AmbienteInfo ambiente, List<EquipamentoHidraulico> equipamentos);

    /// <summary>Obtém equipamentos ausentes (obrigatórios mas não encontrados).</summary>
    List<EquipmentType> ObterEquipamentosAusentes(
        RoomType tipo, List<EquipamentoHidraulico> existentes);
}
