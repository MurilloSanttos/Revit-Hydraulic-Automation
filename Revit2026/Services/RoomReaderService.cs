using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using PluginCore.Interfaces;
using PluginCore.Logging;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço responsável pela leitura de Rooms do modelo arquitetônico.
    /// Extrai dados geométricos e de identificação de cada Room e os
    /// converte para o modelo de domínio AmbienteInfo.
    /// </summary>
    public class RoomReaderService
    {
        private readonly Document _doc;
        private readonly ILogService _log;
        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "RoomReader";

        // Fator de conversão de pés (unidade do Revit) para metros
        private const double FEET_TO_METERS = 0.3048;
        private const double SQFEET_TO_SQM = 0.3048 * 0.3048;

        public RoomReaderService(Document doc, ILogService log)
        {
            _doc = doc;
            _log = log;
        }

        /// <summary>
        /// Lê todos os Rooms válidos do modelo.
        /// Rooms redundantes, não colocados ou sem área são filtrados.
        /// </summary>
        public List<AmbienteInfo> LerTodosOsRooms()
        {
            _log.Info(ETAPA, COMPONENTE, "Iniciando leitura de Rooms do modelo...");

            var collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            var ambientes = new List<AmbienteInfo>();
            int ignorados = 0;

            foreach (var element in collector)
            {
                if (element is not Room room)
                    continue;

                // Ignorar Rooms não colocados ou redundantes
                if (room.Location == null)
                {
                    ignorados++;
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Room '{room.Name}' ignorado — não está colocado (sem Location).",
                        room.Id.Value);
                    continue;
                }

                if (room.Area <= 0)
                {
                    ignorados++;
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Room '{room.Name}' ignorado — área zero ou negativa.",
                        room.Id.Value);
                    continue;
                }

                var ambiente = ConverterParaAmbienteInfo(room);
                ambientes.Add(ambiente);
            }

            _log.Info(ETAPA, COMPONENTE,
                $"Leitura concluída: {ambientes.Count} Rooms válidos encontrados, {ignorados} ignorados.");

            return ambientes;
        }

        /// <summary>
        /// Converte um Room do Revit para o modelo de domínio AmbienteInfo.
        /// </summary>
        private AmbienteInfo ConverterParaAmbienteInfo(Room room)
        {
            var pontoCentral = ObterPontoCentral(room);
            var nivel = _doc.GetElement(room.LevelId) as Level;

            return new AmbienteInfo
            {
                ElementId = room.Id.Value,
                NomeOriginal = room.Name ?? string.Empty,
                Numero = room.Number ?? string.Empty,
                Nivel = nivel?.Name ?? "Sem Nível",
                AreaM2 = room.Area * SQFEET_TO_SQM,
                PerimetroM = room.Perimeter * FEET_TO_METERS,
                TipoElemento = TipoElemento.Room,
                PontoCentral = pontoCentral
            };
        }

        /// <summary>
        /// Obtém o ponto central de um Room.
        /// </summary>
        private PontoXYZ ObterPontoCentral(Room room)
        {
            try
            {
                if (room.Location is LocationPoint locationPoint)
                {
                    var point = locationPoint.Point;
                    return new PontoXYZ(
                        point.X * FEET_TO_METERS,
                        point.Y * FEET_TO_METERS,
                        point.Z * FEET_TO_METERS
                    );
                }
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Não foi possível obter ponto central do Room '{room.Name}': {ex.Message}",
                    room.Id.Value);
            }

            return new PontoXYZ();
        }
    }
}
