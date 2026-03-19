using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using PluginCore.Interfaces;
using PluginCore.Logging;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço responsável pela criação e validação de Spaces MEP.
    /// 
    /// Fluxo:
    /// 1. Lê Spaces existentes no modelo
    /// 2. Compara com Rooms para encontrar correspondências
    /// 3. Cria Spaces faltantes para Rooms relevantes
    /// 4. Valida a correspondência final
    /// </summary>
    public class SpaceManagerService
    {
        private readonly Document _doc;
        private readonly ILogService _log;
        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "SpaceManager";

        private const double FEET_TO_METERS = 0.3048;
        private const double SQFEET_TO_SQM = 0.3048 * 0.3048;

        // Tolerância para considerar que Room e Space estão na mesma posição (em metros)
        private const double TOLERANCIA_POSICAO = 0.5;

        public SpaceManagerService(Document doc, ILogService log)
        {
            _doc = doc;
            _log = log;
        }

        /// <summary>
        /// Lê todos os Spaces MEP existentes no modelo.
        /// </summary>
        public List<AmbienteInfo> LerTodosOsSpaces()
        {
            _log.Info(ETAPA, COMPONENTE, "Lendo Spaces MEP existentes...");

            var collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType();

            var spaces = new List<AmbienteInfo>();

            foreach (var element in collector)
            {
                if (element is not Space space)
                    continue;

                if (space.Location == null || space.Area <= 0)
                    continue;

                var nivel = _doc.GetElement(space.LevelId) as Level;

                var ambiente = new AmbienteInfo
                {
                    ElementId = space.Id.Value,
                    NomeOriginal = space.Name ?? string.Empty,
                    Numero = space.Number ?? string.Empty,
                    Nivel = nivel?.Name ?? "Sem Nível",
                    AreaM2 = space.Area * SQFEET_TO_SQM,
                    PerimetroM = space.Perimeter * FEET_TO_METERS,
                    TipoElemento = TipoElemento.Space,
                    PontoCentral = ObterPontoCentral(space)
                };

                spaces.Add(ambiente);
            }

            _log.Info(ETAPA, COMPONENTE,
                $"{spaces.Count} Spaces MEP existentes encontrados.");

            return spaces;
        }

        /// <summary>
        /// Valida a correspondência entre Rooms e Spaces.
        /// Utiliza proximidade espacial e nível para associar Rooms a Spaces.
        /// </summary>
        public ValidacaoCorrespondencia ValidarCorrespondencia(
            List<AmbienteInfo> rooms, List<AmbienteInfo> spaces)
        {
            _log.Info(ETAPA, COMPONENTE,
                $"Validando correspondência: {rooms.Count} Rooms × {spaces.Count} Spaces...");

            var resultado = new ValidacaoCorrespondencia();
            var spacesUsados = new HashSet<long>();

            foreach (var room in rooms)
            {
                var spaceMaisProximo = spaces
                    .Where(s => !spacesUsados.Contains(s.ElementId))
                    .Where(s => s.Nivel == room.Nivel)
                    .Select(s => new
                    {
                        Space = s,
                        Distancia = CalcularDistancia(room.PontoCentral, s.PontoCentral)
                    })
                    .Where(x => x.Distancia <= TOLERANCIA_POSICAO)
                    .OrderBy(x => x.Distancia)
                    .FirstOrDefault();

                if (spaceMaisProximo != null)
                {
                    resultado.Correspondentes.Add((room, spaceMaisProximo.Space));
                    spacesUsados.Add(spaceMaisProximo.Space.ElementId);
                    room.SpaceIdCorrespondente = spaceMaisProximo.Space.ElementId;
                }
                else
                {
                    resultado.RoomsSemSpace.Add(room);
                }
            }

            // Spaces que não foram associados a nenhum Room
            resultado.SpacesOrfaos = spaces
                .Where(s => !spacesUsados.Contains(s.ElementId))
                .ToList();

            // Logs de resultado
            _log.Info(ETAPA, COMPONENTE,
                $"Correspondência: {resultado.Correspondentes.Count} pares, " +
                $"{resultado.RoomsSemSpace.Count} Rooms sem Space, " +
                $"{resultado.SpacesOrfaos.Count} Spaces órfãos.");

            if (resultado.RoomsSemSpace.Count > 0)
            {
                var nomes = string.Join(", ",
                    resultado.RoomsSemSpace.Select(r => $"'{r.NomeOriginal}'"));
                _log.Medio(ETAPA, COMPONENTE,
                    $"Rooms sem Space correspondente: {nomes}");
            }

            if (resultado.SpacesOrfaos.Count > 0)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"{resultado.SpacesOrfaos.Count} Spaces sem Room correspondente (órfãos).");
            }

            return resultado;
        }

        /// <summary>
        /// Cria Spaces MEP para Rooms que não possuem Space correspondente.
        /// IMPORTANTE: Deve ser executado dentro de uma Transaction.
        /// </summary>
        public List<AmbienteInfo> CriarSpacesParaRooms(List<AmbienteInfo> roomsSemSpace)
        {
            _log.Info(ETAPA, COMPONENTE,
                $"Criando {roomsSemSpace.Count} Spaces para Rooms sem correspondência...");

            var spacesCriados = new List<AmbienteInfo>();

            foreach (var room in roomsSemSpace)
            {
                try
                {
                    var space = CriarSpaceParaRoom(room);
                    if (space != null)
                    {
                        spacesCriados.Add(space);
                        _log.Info(ETAPA, COMPONENTE,
                            $"Space criado para Room '{room.NomeOriginal}' (#{room.Numero}).",
                            room.ElementId);
                    }
                }
                catch (Exception ex)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao criar Space para Room '{room.NomeOriginal}': {ex.Message}",
                        room.ElementId,
                        ex.StackTrace);
                }
            }

            _log.Info(ETAPA, COMPONENTE,
                $"{spacesCriados.Count}/{roomsSemSpace.Count} Spaces criados com sucesso.");

            return spacesCriados;
        }

        /// <summary>
        /// Cria um Space MEP individual a partir de um Room.
        /// </summary>
        private AmbienteInfo? CriarSpaceParaRoom(AmbienteInfo room)
        {
            // Encontrar o Level correspondente
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            var level = levels.FirstOrDefault(l => l.Name == room.Nivel);
            if (level == null)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Nível '{room.Nivel}' não encontrado para criação de Space.",
                    room.ElementId);
                return null;
            }

            // Ponto de inserção (converter de volta para pés)
            var pontoInsercao = new XYZ(
                room.PontoCentral.X / FEET_TO_METERS,
                room.PontoCentral.Y / FEET_TO_METERS,
                room.PontoCentral.Z / FEET_TO_METERS
            );

            // Criar o Space
            var space = _doc.Create.NewSpace(level, new UV(pontoInsercao.X, pontoInsercao.Y));

            if (space == null)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Revit retornou null ao criar Space para Room '{room.NomeOriginal}'.",
                    room.ElementId);
                return null;
            }

            // Configurar nome e número
            space.Name = room.NomeOriginal;
            space.Number = room.Numero;

            // Criar AmbienteInfo para o Space criado
            var spaceInfo = new AmbienteInfo
            {
                ElementId = space.Id.Value,
                NomeOriginal = space.Name,
                Numero = space.Number,
                Nivel = room.Nivel,
                AreaM2 = space.Area * SQFEET_TO_SQM,
                PerimetroM = space.Perimeter * FEET_TO_METERS,
                TipoElemento = TipoElemento.Space,
                SpaceCriadoAutomaticamente = true,
                PontoCentral = room.PontoCentral
            };

            // Atualizar o Room original com o ID do Space
            room.SpaceIdCorrespondente = space.Id.Value;
            room.SpaceCriadoAutomaticamente = true;

            return spaceInfo;
        }

        /// <summary>
        /// Calcula a distância euclidiana 2D entre dois pontos (ignora Z).
        /// </summary>
        private static double CalcularDistancia(PontoXYZ a, PontoXYZ b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Obtém o ponto central de um Space.
        /// </summary>
        private PontoXYZ ObterPontoCentral(Space space)
        {
            try
            {
                if (space.Location is LocationPoint locationPoint)
                {
                    var point = locationPoint.Point;
                    return new PontoXYZ(
                        point.X * FEET_TO_METERS,
                        point.Y * FEET_TO_METERS,
                        point.Z * FEET_TO_METERS
                    );
                }
            }
            catch
            {
                // Silencioso — ponto padrão será usado
            }

            return new PontoXYZ();
        }
    }
}
