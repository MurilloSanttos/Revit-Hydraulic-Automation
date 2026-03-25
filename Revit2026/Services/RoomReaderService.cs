using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço responsável pela leitura completa de Rooms do modelo arquitetônico.
    /// Extrai dados geométricos (área, perímetro, centro), paredes de fronteira,
    /// portas e janelas para cada Room, filtra elementos inválidos e converte
    /// para AmbienteInfo com precisão centimétrica.
    /// </summary>
    public class RoomReaderService
    {
        private readonly Document _doc;
        private readonly ILogService _log;

        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "RoomReader";
        private const string FILTRO = "FiltroRoom";
        private const string GEOMETRIA = "Geometria";

        // Área mínima em m² para considerar Room válida
        private const double AREA_MINIMA_M2 = 0.1;

        // Expansão do BoundingBox (em pés) para capturar portas/janelas na fronteira
        // 0.1m ≈ 0.328 pés
        private const double BBOX_EXPANSAO_FT = 0.328;

        public RoomReaderService(Document doc, ILogService log)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  LEITURA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Lê todos os Rooms válidos do modelo ativo com geometria completa,
        /// paredes de fronteira, portas e janelas.
        /// Se nenhum Room for encontrado, tenta ler de linked documents.
        /// </summary>
        public List<AmbienteInfo> LerTodosOsRooms()
        {
            _log.Info(ETAPA, COMPONENTE, "Iniciando leitura de Rooms do modelo...");

            var ambientes = new List<AmbienteInfo>();
            var filtro = new FiltroResultado();

            // ── 1. Coletar elementos da categoria OST_Rooms ───
            var collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            var todosElementos = collector.ToList();

            _log.Info(ETAPA, COMPONENTE,
                $"Collector retornou {todosElementos.Count} elementos na categoria OST_Rooms.");

            // ── 2. Pré-coletar portas e janelas do modelo ─────
            var todasPortas = ColetarFamilyInstances(BuiltInCategory.OST_Doors);
            var todasJanelas = ColetarFamilyInstances(BuiltInCategory.OST_Windows);

            _log.Info(ETAPA, GEOMETRIA,
                $"Elementos coletados para matching: " +
                $"{todasPortas.Count} portas, {todasJanelas.Count} janelas.");

            // ── 3. Filtrar e converter cada elemento ──────────
            int index = 0;
            foreach (var element in todosElementos)
            {
                index++;

                // 3a. Verificar se é Room
                if (element is not Room room)
                {
                    filtro.NaoRoom++;
                    _log.Leve(ETAPA, FILTRO,
                        $"Elemento #{index} (Id={element.Id.Value}) " +
                        $"é {element.GetType().Name}, não Room — ignorado.");
                    continue;
                }

                // 3b. Aplicar validações
                var motivo = ValidarRoom(room);
                if (motivo != null)
                {
                    filtro.Registrar(motivo);
                    _log.Medio(ETAPA, FILTRO,
                        $"Room descartada: {motivo} " +
                        $"('{room.Name}' #{room.Number}, Id={room.Id.Value})",
                        room.Id.Value);
                    continue;
                }

                // 3c. Converter para modelo de domínio
                var ambiente = ConverterParaAmbienteInfo(room);

                // 3d. Coletar paredes de fronteira
                ColetarParedesDeFronteira(room, ambiente);

                // 3e. Coletar portas e janelas
                ColetarAberturas(room, ambiente, todasPortas, todasJanelas);

                // 3f. Log de diagnóstico completo
                _log.Info(ETAPA, COMPONENTE,
                    $"Room válida: '{room.Name}' (#{room.Number}) " +
                    $"Id={room.Id.Value}, " +
                    $"Area={ambiente.AreaM2:F2} m², " +
                    $"Perímetro={ambiente.PerimetroM:F2} m, " +
                    $"Centro=({ambiente.PontoCentral.X:F3}, " +
                    $"{ambiente.PontoCentral.Y:F3}, " +
                    $"{ambiente.PontoCentral.Z:F3}) m, " +
                    $"Paredes={ambiente.ParedesIds.Count}, " +
                    $"Portas={ambiente.PortasIds.Count}, " +
                    $"Janelas={ambiente.JanelasIds.Count}");

                ambientes.Add(ambiente);
            }

            // ── 4. Fallback para linked documents ─────────────
            if (ambientes.Count == 0 && todosElementos.Count == 0)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    "Nenhuma Room encontrada no documento ativo. " +
                    "Verificando linked documents...");

                ambientes = LerRoomsDeLinks();
            }

            // ── 5. Resumo final ───────────────────────────────
            _log.Info(ETAPA, COMPONENTE,
                $"Leitura concluída: {ambientes.Count} Rooms válidas, " +
                $"{filtro.TotalDescartados} descartadas " +
                $"({filtro.SemLocation} sem localização, " +
                $"{filtro.SemArea} área zero, " +
                $"{filtro.NaoDelimitada} não delimitadas, " +
                $"{filtro.NomeVazio} nome vazio, " +
                $"{filtro.NaoRoom} não-Room).");

            return ambientes;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE ROOM
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida se uma Room é utilizável pelo pipeline.
        /// Retorna null se válida, ou o motivo da rejeição.
        /// </summary>
        private string? ValidarRoom(Room room)
        {
            if (room.Location == null)
                return "sem localização.";

            if (room.Area <= 0)
                return "área igual a zero.";

            var areaM2 = ConverterArea(room.Area);
            if (areaM2 < AREA_MINIMA_M2)
                return $"área muito pequena ({areaM2:F4} m²).";

            try
            {
                var boundaries = room.GetBoundarySegments(
                    new SpatialElementBoundaryOptions());
                if (boundaries == null || boundaries.Count == 0)
                    return "não delimitada (sem boundary segments).";
            }
            catch
            {
                return "erro ao obter contorno da Room.";
            }

            if (string.IsNullOrWhiteSpace(room.Name))
                return "nome vazio.";

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  COLETA DE PAREDES DE FRONTEIRA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém todas as paredes que fazem fronteira com o Room
        /// via boundary segments. Evita duplicatas.
        /// </summary>
        private void ColetarParedesDeFronteira(Room room, AmbienteInfo ambiente)
        {
            try
            {
                var options = new SpatialElementBoundaryOptions();
                var boundarySegments = room.GetBoundarySegments(options);

                if (boundarySegments == null || boundarySegments.Count == 0)
                {
                    _log.Medio(ETAPA, GEOMETRIA,
                        $"Room '{room.Name}' não possui boundary segments.",
                        room.Id.Value);
                    return;
                }

                var paredesUnicas = new HashSet<long>();

                foreach (var loop in boundarySegments)
                {
                    foreach (var segment in loop)
                    {
                        var elementId = segment.ElementId;

                        // Ignorar segmentos sem elemento associado
                        if (elementId == ElementId.InvalidElementId)
                            continue;

                        var elemento = _doc.GetElement(elementId);
                        if (elemento is not Wall)
                            continue;

                        if (paredesUnicas.Add(elementId.Value))
                        {
                            ambiente.ParedesIds.Add(elementId.Value);
                        }
                    }
                }

                _log.Info(ETAPA, GEOMETRIA,
                    $"Room '{room.Name}': {ambiente.ParedesIds.Count} paredes de fronteira.",
                    room.Id.Value);
            }
            catch (Exception ex)
            {
                _log.Medio(ETAPA, GEOMETRIA,
                    $"Erro ao coletar paredes do Room '{room.Name}': {ex.Message}",
                    room.Id.Value);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  COLETA DE PORTAS E JANELAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Pré-coleta todas as FamilyInstance de uma categoria do modelo.
        /// </summary>
        private List<FamilyInstance> ColetarFamilyInstances(BuiltInCategory categoria)
        {
            return new FilteredElementCollector(_doc)
                .OfCategory(categoria)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();
        }

        /// <summary>
        /// Identifica portas e janelas pertencentes ao Room.
        /// Usa estratégia primária (Room property) com fallback por BoundingBox.
        /// </summary>
        private void ColetarAberturas(
            Room room,
            AmbienteInfo ambiente,
            List<FamilyInstance> todasPortas,
            List<FamilyInstance> todasJanelas)
        {
            try
            {
                // Obter BoundingBox do Room para fallback
                var bbox = room.get_BoundingBox(null);
                Outline? outlineExpandido = null;

                if (bbox != null)
                {
                    outlineExpandido = new Outline(
                        new XYZ(bbox.Min.X - BBOX_EXPANSAO_FT,
                                bbox.Min.Y - BBOX_EXPANSAO_FT,
                                bbox.Min.Z - BBOX_EXPANSAO_FT),
                        new XYZ(bbox.Max.X + BBOX_EXPANSAO_FT,
                                bbox.Max.Y + BBOX_EXPANSAO_FT,
                                bbox.Max.Z + BBOX_EXPANSAO_FT));
                }

                // ── Portas ────────────────────────────────────
                foreach (var porta in todasPortas)
                {
                    if (PertenceAoRoom(porta, room, outlineExpandido))
                    {
                        ambiente.PortasIds.Add(porta.Id.Value);
                    }
                }

                // ── Janelas ───────────────────────────────────
                foreach (var janela in todasJanelas)
                {
                    if (PertenceAoRoom(janela, room, outlineExpandido))
                    {
                        ambiente.JanelasIds.Add(janela.Id.Value);
                    }
                }

                // ── Log de resultado ──────────────────────────
                if (ambiente.PortasIds.Count == 0 && ambiente.JanelasIds.Count == 0)
                {
                    _log.Medio(ETAPA, GEOMETRIA,
                        $"Room '{room.Name}': nenhuma porta ou janela encontrada.",
                        room.Id.Value);
                }
                else
                {
                    _log.Info(ETAPA, GEOMETRIA,
                        $"Room '{room.Name}': {ambiente.PortasIds.Count} portas, " +
                        $"{ambiente.JanelasIds.Count} janelas.",
                        room.Id.Value);
                }
            }
            catch (Exception ex)
            {
                _log.Medio(ETAPA, GEOMETRIA,
                    $"Erro ao coletar aberturas do Room '{room.Name}': {ex.Message}",
                    room.Id.Value);
            }
        }

        /// <summary>
        /// Verifica se uma FamilyInstance (porta/janela) pertence ao Room.
        /// Estratégia 1: Verificar Room property da instância.
        /// Estratégia 2: Verificar se LocationPoint está dentro do BoundingBox expandido.
        /// </summary>
        private bool PertenceAoRoom(FamilyInstance instance, Room room, Outline? outlineRoom)
        {
            // Estratégia 1: Room/FromRoom/ToRoom property
            try
            {
                var fromRoom = instance.FromRoom;
                var toRoom = instance.ToRoom;

                if (fromRoom != null && fromRoom.Id == room.Id)
                    return true;
                if (toRoom != null && toRoom.Id == room.Id)
                    return true;

                // Para janelas: verificar Room property
                var roomParam = instance.Room;
                if (roomParam != null && roomParam.Id == room.Id)
                    return true;
            }
            catch { /* fallback para BoundingBox */ }

            // Estratégia 2: LocationPoint dentro do BoundingBox expandido
            if (outlineRoom == null)
                return false;

            try
            {
                if (instance.Location is LocationPoint locPoint)
                {
                    return outlineRoom.Contains(locPoint.Point, 0.01);
                }
            }
            catch { /* não pertence */ }

            return false;
        }

        // ══════════════════════════════════════════════════════════
        //  CONVERSÃO DE UNIDADES (UnitUtils)
        // ══════════════════════════════════════════════════════════

        private static double ConverterArea(double areaInterna)
        {
            return UnitUtils.ConvertFromInternalUnits(areaInterna, UnitTypeId.SquareMeters);
        }

        private static double ConverterComprimento(double comprimentoInterno)
        {
            return UnitUtils.ConvertFromInternalUnits(comprimentoInterno, UnitTypeId.Meters);
        }

        private static PontoXYZ ConverterPonto(XYZ point)
        {
            return new PontoXYZ(
                UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Meters),
                UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Meters),
                UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Meters)
            );
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRAÇÃO DE PONTO CENTRAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o ponto central de um Room.
        /// Estratégia 1: LocationPoint. Estratégia 2: BoundingBox centroide.
        /// Fallback: (0,0,0) com log Medio.
        /// </summary>
        private PontoXYZ ObterPontoCentral(Room room)
        {
            // Estratégia 1: LocationPoint
            try
            {
                if (room.Location is LocationPoint locationPoint)
                    return ConverterPonto(locationPoint.Point);
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao obter LocationPoint do Room '{room.Name}': {ex.Message}",
                    room.Id.Value);
            }

            // Estratégia 2: Centroide do BoundingBox
            try
            {
                var bbox = room.get_BoundingBox(null);
                if (bbox != null)
                {
                    var centroide = (bbox.Min + bbox.Max) / 2.0;
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Ponto central via BoundingBox (fallback): Room '{room.Name}'.",
                        room.Id.Value);
                    return ConverterPonto(centroide);
                }
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao obter BoundingBox do Room '{room.Name}': {ex.Message}",
                    room.Id.Value);
            }

            // Estratégia 3: Retornar (0,0,0)
            _log.Medio(ETAPA, COMPONENTE,
                $"Sem ponto central para Room '{room.Name}' (Id={room.Id.Value}). Usando (0,0,0).",
                room.Id.Value);

            return new PontoXYZ();
        }

        // ══════════════════════════════════════════════════════════
        //  CONVERSÃO PARA MODELO DE DOMÍNIO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Converte um Room do documento ativo para AmbienteInfo.
        /// Preenche geometria (área, perímetro, centro).
        /// Paredes e aberturas são preenchidas em etapa separada.
        /// </summary>
        private AmbienteInfo ConverterParaAmbienteInfo(Room room)
        {
            var nivel = _doc.GetElement(room.LevelId) as Level;

            return new AmbienteInfo
            {
                ElementId = room.Id.Value,
                NomeOriginal = room.Name ?? string.Empty,
                Numero = room.Number ?? string.Empty,
                Nivel = nivel?.Name ?? "Sem Nível",
                AreaM2 = ConverterArea(room.Area),
                PerimetroM = ConverterComprimento(room.Perimeter),
                TipoElemento = TipoElemento.Room,
                PontoCentral = ObterPontoCentral(room)
            };
        }

        /// <summary>
        /// Converte Room de linked document, aplicando transform de coordenadas.
        /// </summary>
        private AmbienteInfo ConverterParaAmbienteInfoDeLink(
            Room room, Document linkedDoc, Transform transform)
        {
            var nivel = linkedDoc.GetElement(room.LevelId) as Level;
            var pontoCentral = new PontoXYZ();

            try
            {
                if (room.Location is LocationPoint locationPoint)
                {
                    var pointTransformado = transform.OfPoint(locationPoint.Point);
                    pontoCentral = ConverterPonto(pointTransformado);
                }
                else
                {
                    var bbox = room.get_BoundingBox(null);
                    if (bbox != null)
                    {
                        var centroide = (bbox.Min + bbox.Max) / 2.0;
                        var centroideTransformado = transform.OfPoint(centroide);
                        pontoCentral = ConverterPonto(centroideTransformado);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao obter centro do Room de link '{room.Name}': {ex.Message}");
            }

            return new AmbienteInfo
            {
                ElementId = room.Id.Value,
                NomeOriginal = room.Name ?? string.Empty,
                Numero = room.Number ?? string.Empty,
                Nivel = nivel?.Name ?? "Sem Nível",
                AreaM2 = ConverterArea(room.Area),
                PerimetroM = ConverterComprimento(room.Perimeter),
                TipoElemento = TipoElemento.Room,
                PontoCentral = pontoCentral,
            };
        }

        // ══════════════════════════════════════════════════════════
        //  LEITURA DE LINKED DOCUMENTS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Tenta ler Rooms de modelos vinculados.
        /// Aplica mesmas validações. Não coleta paredes/aberturas de links
        /// (requer transação no documento de link).
        /// </summary>
        private List<AmbienteInfo> LerRoomsDeLinks()
        {
            var ambientes = new List<AmbienteInfo>();

            var linkInstances = new FilteredElementCollector(_doc)
                .OfClass(typeof(RevitLinkInstance))
                .ToList();

            _log.Info(ETAPA, COMPONENTE,
                $"Encontrados {linkInstances.Count} linked documents.");

            foreach (var linkElement in linkInstances)
            {
                if (linkElement is not RevitLinkInstance linkInstance)
                    continue;

                var linkedDoc = linkInstance.GetLinkDocument();
                if (linkedDoc == null)
                {
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Link '{linkElement.Name}' — documento não carregado.");
                    continue;
                }

                _log.Info(ETAPA, COMPONENTE,
                    $"Lendo Rooms do link: '{linkedDoc.Title}'...");

                var linkCollector = new FilteredElementCollector(linkedDoc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType();

                var transform = linkInstance.GetTotalTransform();
                int countLink = 0;

                foreach (var element in linkCollector)
                {
                    if (element is not Room room)
                        continue;

                    var motivo = ValidarRoom(room);
                    if (motivo != null)
                    {
                        _log.Medio(ETAPA, FILTRO,
                            $"Room descartada (link): {motivo} " +
                            $"('{room.Name}', Id={room.Id.Value})");
                        continue;
                    }

                    var ambiente = ConverterParaAmbienteInfoDeLink(room, linkedDoc, transform);
                    ambientes.Add(ambiente);
                    countLink++;
                }

                _log.Info(ETAPA, COMPONENTE,
                    $"Link '{linkedDoc.Title}': {countLink} Rooms válidas encontradas.");
            }

            return ambientes;
        }

        // ══════════════════════════════════════════════════════════
        //  CONTABILIZAÇÃO DE FILTRO
        // ══════════════════════════════════════════════════════════

        private class FiltroResultado
        {
            public int SemLocation { get; set; }
            public int SemArea { get; set; }
            public int NaoDelimitada { get; set; }
            public int NomeVazio { get; set; }
            public int NaoRoom { get; set; }
            public int Outros { get; set; }

            public int TotalDescartados =>
                SemLocation + SemArea + NaoDelimitada + NomeVazio + NaoRoom + Outros;

            public void Registrar(string motivo)
            {
                if (motivo.StartsWith("sem localização"))
                    SemLocation++;
                else if (motivo.StartsWith("área igual a zero"))
                    SemArea++;
                else if (motivo.StartsWith("área muito pequena"))
                    SemArea++;
                else if (motivo.Contains("não delimitada") || motivo.Contains("contorno"))
                    NaoDelimitada++;
                else if (motivo.StartsWith("nome vazio"))
                    NomeVazio++;
                else
                    Outros++;
            }
        }
    }
}
