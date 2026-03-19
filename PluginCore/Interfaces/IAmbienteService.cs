using PluginCore.Models;

namespace PluginCore.Interfaces
{
    /// <summary>
    /// Contrato para o serviço de ambientes.
    /// Implementações concretas lidam com a API do Revit.
    /// </summary>
    public interface IAmbienteService
    {
        /// <summary>
        /// Lê todos os Rooms do modelo arquitetônico.
        /// </summary>
        List<AmbienteInfo> LerRooms();

        /// <summary>
        /// Lê todos os Spaces MEP do modelo.
        /// </summary>
        List<AmbienteInfo> LerSpaces();

        /// <summary>
        /// Cria Spaces MEP para os Rooms que não possuem Space correspondente.
        /// </summary>
        /// <param name="roomsSemSpace">Lista de ambientes (Rooms) sem Space associado.</param>
        /// <returns>Lista de Spaces criados.</returns>
        List<AmbienteInfo> CriarSpacesParaRooms(List<AmbienteInfo> roomsSemSpace);

        /// <summary>
        /// Valida a correspondência entre Rooms e Spaces.
        /// Identifica Rooms sem Space, Spaces órfãos, e inconsistências.
        /// </summary>
        ValidacaoCorrespondencia ValidarCorrespondencia(
            List<AmbienteInfo> rooms, List<AmbienteInfo> spaces);
    }

    /// <summary>
    /// Resultado da validação de correspondência Room ↔ Space.
    /// </summary>
    public class ValidacaoCorrespondencia
    {
        /// <summary>
        /// Rooms que possuem Space correspondente.
        /// </summary>
        public List<(AmbienteInfo Room, AmbienteInfo Space)> Correspondentes { get; set; } = new();

        /// <summary>
        /// Rooms sem Space correspondente.
        /// </summary>
        public List<AmbienteInfo> RoomsSemSpace { get; set; } = new();

        /// <summary>
        /// Spaces sem Room correspondente (órfãos).
        /// </summary>
        public List<AmbienteInfo> SpacesOrfaos { get; set; } = new();

        /// <summary>
        /// Indica se a correspondência está completa.
        /// </summary>
        public bool EhCompleta => RoomsSemSpace.Count == 0 && SpacesOrfaos.Count == 0;
    }
}
