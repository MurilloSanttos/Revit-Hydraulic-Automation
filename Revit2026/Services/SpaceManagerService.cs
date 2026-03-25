using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço responsável pela leitura, correspondência e criação de MEP Spaces.
    ///
    /// Fluxo:
    /// 1. Lê Spaces existentes no modelo com dados geométricos completos
    /// 2. Compara com Rooms para encontrar correspondências por proximidade
    /// 3. Cria Spaces faltantes para Rooms relevantes
    /// 4. Valida a correspondência final
    /// </summary>
    public class SpaceManagerService
    {
        private readonly Document _doc;
        private readonly ILogService _log;

        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "SpaceManager";
        private const string FILTRO = "FiltroSpace";
        private const string GEOMETRIA = "GeometriaSpace";

        // Tolerância para considerar Room e Space na mesma posição (metros)
        private const double TOLERANCIA_POSICAO = 0.5;

        // Área mínima em m² para considerar Space válido
        private const double AREA_MINIMA_M2 = 0.1;

        public SpaceManagerService(Document doc, ILogService log)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  LEITURA DE SPACES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Lê todos os MEP Spaces válidos do modelo.
        /// Extrai área, perímetro (via boundary segments) e ponto central.
        /// Filtra Spaces inválidos com logs estruturados.
        /// </summary>
        public List<AmbienteInfo> LerSpaces(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            _log.Info(ETAPA, COMPONENTE, "Iniciando leitura de Spaces MEP...");

            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType();

            var todosElementos = collector.ToList();

            _log.Info(ETAPA, COMPONENTE,
                $"Collector retornou {todosElementos.Count} elementos MEPSpace.");

            var spaces = new List<AmbienteInfo>();
            int descartados = 0;
            int semLocation = 0;
            int semArea = 0;
            int naoDelimitados = 0;

            foreach (var element in todosElementos)
            {
                if (element is not Space space)
                    continue;

                // ── Validações ────────────────────────────────
                // V1: Sem localização
                if (space.Location == null)
                {
                    semLocation++;
                    descartados++;
                    _log.Medio(ETAPA, FILTRO,
                        $"Space descartado: sem localização. " +
                        $"('{space.Name}' #{space.Number}, Id={space.Id.Value})",
                        space.Id.Value);
                    continue;
                }

                // V2: Sem área
                var areaM2 = ConverterArea(space.Area);
                if (space.Area <= 0 || areaM2 < AREA_MINIMA_M2)
                {
                    semArea++;
                    descartados++;
                    _log.Medio(ETAPA, FILTRO,
                        $"Space descartado: área zero ou insuficiente ({areaM2:F4} m²). " +
                        $"('{space.Name}', Id={space.Id.Value})",
                        space.Id.Value);
                    continue;
                }

                // V3: Sem LevelId válido
                var nivel = doc.GetElement(space.LevelId) as Level;
                if (nivel == null)
                {
                    descartados++;
                    _log.Medio(ETAPA, FILTRO,
                        $"Space descartado: LevelId inválido. " +
                        $"('{space.Name}', Id={space.Id.Value})",
                        space.Id.Value);
                    continue;
                }

                // ── Extrair geometria ─────────────────────────
                var pontoCentral = ObterPontoCentral(space);
                var perimetroM = CalcularPerimetro(space);

                // V4: Verificar se delimitado
                if (perimetroM <= 0)
                {
                    naoDelimitados++;
                    _log.Leve(ETAPA, GEOMETRIA,
                        $"Space '{space.Name}' sem boundary segments. " +
                        $"Usando Perimeter property como fallback.",
                        space.Id.Value);
                    perimetroM = ConverterComprimento(space.Perimeter);
                }

                // ── Criar AmbienteInfo ────────────────────────
                var ambiente = new AmbienteInfo
                {
                    ElementId = space.Id.Value,
                    NomeOriginal = space.Name ?? string.Empty,
                    Numero = space.Number ?? string.Empty,
                    Nivel = nivel.Name ?? "Sem Nível",
                    AreaM2 = areaM2,
                    PerimetroM = perimetroM,
                    TipoElemento = TipoElemento.Space,
                    PontoCentral = pontoCentral
                };

                spaces.Add(ambiente);

                _log.Info(ETAPA, COMPONENTE,
                    $"Space válido: '{space.Name}' (#{space.Number}) " +
                    $"Id={space.Id.Value}, " +
                    $"Area={areaM2:F2} m², " +
                    $"Perímetro={perimetroM:F2} m, " +
                    $"Centro=({pontoCentral.X:F3}, {pontoCentral.Y:F3}, {pontoCentral.Z:F3}) m, " +
                    $"Nível='{nivel.Name}'");
            }

            // ── Resumo ───────────────────────────────────────
            _log.Info(ETAPA, COMPONENTE,
                $"Leitura concluída: {spaces.Count} Spaces válidos, " +
                $"{descartados} descartados " +
                $"({semLocation} sem localização, " +
                $"{semArea} sem área, " +
                $"{naoDelimitados} não delimitados).");

            return spaces;
        }

        /// <summary>
        /// Sobrecarga que usa o documento do construtor.
        /// </summary>
        public List<AmbienteInfo> LerTodosOsSpaces()
        {
            return LerSpaces(_doc);
        }

        // ══════════════════════════════════════════════════════════
        //  CORRESPONDÊNCIA ROOM ↔ SPACE
        // ══════════════════════════════════════════════════════════

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

                    _log.Info(ETAPA, COMPONENTE,
                        $"Match: Room '{room.NomeOriginal}' ↔ Space '{spaceMaisProximo.Space.NomeOriginal}' " +
                        $"(dist={spaceMaisProximo.Distancia:F3} m)");
                }
                else
                {
                    resultado.RoomsSemSpace.Add(room);
                }
            }

            // Spaces não associados
            resultado.SpacesOrfaos = spaces
                .Where(s => !spacesUsados.Contains(s.ElementId))
                .ToList();

            // Logs
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

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO DE SPACES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Spaces MEP para Rooms que não possuem Space correspondente.
        /// IMPORTANTE: Deve ser executado dentro de uma Transaction.
        /// </summary>
        public List<AmbienteInfo> CriarSpacesParaRooms(List<AmbienteInfo> roomsSemSpace)
        {
            _log.Info(ETAPA, COMPONENTE,
                $"Criando {roomsSemSpace.Count} Spaces para Rooms sem correspondência...");

            var spacesCriados = new List<AmbienteInfo>();

            // Cache de Levels
            var levelsPorNome = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToDictionary(l => l.Name ?? "", l => l, StringComparer.OrdinalIgnoreCase);

            foreach (var room in roomsSemSpace)
            {
                try
                {
                    var space = CriarSpaceParaRoom(room, levelsPorNome);
                    if (space != null)
                    {
                        spacesCriados.Add(space);
                        _log.Info(ETAPA, COMPONENTE,
                            $"Space criado para Room '{room.NomeOriginal}' " +
                            $"(#{room.Numero}) → SpaceId={space.ElementId}.",
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
        private AmbienteInfo? CriarSpaceParaRoom(
            AmbienteInfo room, Dictionary<string, Level> levelsPorNome)
        {
            if (!levelsPorNome.TryGetValue(room.Nivel, out var level))
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Nível '{room.Nivel}' não encontrado para criação de Space.",
                    room.ElementId);
                return null;
            }

            // Converter ponto de metros de volta para pés (unidade interna do Revit)
            var pontoInsercao = new UV(
                ConverterParaPes(room.PontoCentral.X),
                ConverterParaPes(room.PontoCentral.Y)
            );

            // Criar o Space
            var space = _doc.Create.NewSpace(level, pontoInsercao);

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

            // Criar AmbienteInfo do Space criado
            var spaceInfo = new AmbienteInfo
            {
                ElementId = space.Id.Value,
                NomeOriginal = space.Name,
                Numero = space.Number,
                Nivel = room.Nivel,
                AreaM2 = ConverterArea(space.Area),
                PerimetroM = ConverterComprimento(space.Perimeter),
                TipoElemento = TipoElemento.Space,
                SpaceCriadoAutomaticamente = true,
                PontoCentral = room.PontoCentral
            };

            // Atualizar o Room original
            room.SpaceIdCorrespondente = space.Id.Value;
            room.SpaceCriadoAutomaticamente = true;

            return spaceInfo;
        }

        // ══════════════════════════════════════════════════════════
        //  GEOMETRIA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o ponto central de um Space.
        /// Estratégia 1: LocationPoint. Estratégia 2: BoundingBox.
        /// Fallback: (0,0,0) com log Critico.
        /// </summary>
        private PontoXYZ ObterPontoCentral(Space space)
        {
            // Estratégia 1: LocationPoint
            try
            {
                if (space.Location is LocationPoint locationPoint)
                    return ConverterPonto(locationPoint.Point);
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, GEOMETRIA,
                    $"Erro ao obter LocationPoint do Space '{space.Name}': {ex.Message}",
                    space.Id.Value);
            }

            // Estratégia 2: BoundingBox centroide
            try
            {
                var bbox = space.get_BoundingBox(null);
                if (bbox != null)
                {
                    var centroide = (bbox.Min + bbox.Max) / 2.0;
                    _log.Leve(ETAPA, GEOMETRIA,
                        $"Ponto central via BoundingBox (fallback): Space '{space.Name}'.",
                        space.Id.Value);
                    return ConverterPonto(centroide);
                }
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, GEOMETRIA,
                    $"Erro ao obter BoundingBox do Space '{space.Name}': {ex.Message}",
                    space.Id.Value);
            }

            // Fallback
            _log.Critico(ETAPA, GEOMETRIA,
                $"Não foi possível obter ponto central do Space '{space.Name}' " +
                $"(Id={space.Id.Value}). Usando (0,0,0).",
                space.Id.Value);

            return new PontoXYZ();
        }

        /// <summary>
        /// Calcula o perímetro real do Space via BoundarySegments.
        /// Soma os comprimentos de todos os segmentos de todos os loops.
        /// Retorna 0 se não houver boundary segments.
        /// </summary>
        private double CalcularPerimetro(Space space)
        {
            try
            {
                var boundaries = space.GetBoundarySegments(
                    new SpatialElementBoundaryOptions());

                if (boundaries == null || boundaries.Count == 0)
                    return 0;

                double perimetroInterno = 0;

                foreach (var loop in boundaries)
                {
                    foreach (var segment in loop)
                    {
                        var curve = segment.GetCurve();
                        if (curve != null)
                            perimetroInterno += curve.Length;
                    }
                }

                return ConverterComprimento(perimetroInterno);
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, GEOMETRIA,
                    $"Erro ao calcular perímetro do Space '{space.Name}': {ex.Message}",
                    space.Id.Value);
                return 0;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONVERSÃO DE UNIDADES
        // ══════════════════════════════════════════════════════════

        private static double ConverterArea(double areaInterna)
        {
            return UnitUtils.ConvertFromInternalUnits(areaInterna, UnitTypeId.SquareMeters);
        }

        private static double ConverterComprimento(double comprimentoInterno)
        {
            return UnitUtils.ConvertFromInternalUnits(comprimentoInterno, UnitTypeId.Meters);
        }

        private static double ConverterParaPes(double metros)
        {
            return UnitUtils.ConvertToInternalUnits(metros, UnitTypeId.Meters);
        }

        private static PontoXYZ ConverterPonto(XYZ point)
        {
            return new PontoXYZ(
                UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Meters),
                UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Meters),
                UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Meters)
            );
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
    }
}
