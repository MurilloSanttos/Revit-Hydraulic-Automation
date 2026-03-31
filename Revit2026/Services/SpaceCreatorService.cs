using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Resultado da criação automática de Spaces.
    /// </summary>
    public class ResultadoCriacaoSpaces
    {
        /// <summary>Total de Rooms processadas.</summary>
        public int TotalRooms { get; set; }

        /// <summary>Rooms que já tinham Space.</summary>
        public int JaPossuiam { get; set; }

        /// <summary>Spaces criados com sucesso.</summary>
        public int Criados { get; set; }

        /// <summary>Falhas na criação.</summary>
        public int Falhas { get; set; }

        /// <summary>Rooms ignoradas (sem nível ou centro).</summary>
        public int Ignorados { get; set; }

        /// <summary>IDs dos Spaces criados.</summary>
        public List<long> SpaceIdsCriados { get; set; } = new();

        /// <summary>ElementIds dos Spaces criados (para retorno da API).</summary>
        public List<ElementId> SpaceElementIds { get; set; } = new();

        /// <summary>Erros encontrados.</summary>
        public List<string> Erros { get; set; } = new();

        public bool Sucesso => Falhas == 0 && Erros.Count == 0;

        public override string ToString() =>
            $"Spaces: {Criados} criados, {JaPossuiam} existentes, " +
            $"{Ignorados} ignorados, {Falhas} falhas (de {TotalRooms} Rooms)";
    }

    /// <summary>
    /// Serviço responsável pela criação automática de MEP Spaces
    /// para Rooms que não possuem Space correspondente.
    /// Suporta execução com Transaction interna ou externa.
    /// </summary>
    public class SpaceCreatorService
    {
        private readonly Document _doc;
        private readonly ILogService _log;

        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "SpaceCreator";

        public SpaceCreatorService(Document doc, ILogService log)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO COM MAPA (Dictionary)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Spaces para Rooms que NÃO existem no mapa de correspondência.
        /// Abre Transaction interna, cria os Spaces e retorna seus ElementIds.
        /// </summary>
        /// <param name="doc">Documento Revit ativo.</param>
        /// <param name="rooms">Lista completa de ambientes (Rooms).</param>
        /// <param name="mapaRoomSpace">Mapa RoomId → SpaceId existente.</param>
        /// <param name="logService">Serviço de log (usa o do construtor se null).</param>
        /// <returns>Lista de ElementId dos Spaces criados.</returns>
        public List<ElementId> CriarSpacesParaRoomsSemSpace(
            Document doc,
            List<AmbienteInfo> rooms,
            Dictionary<long, long> mapaRoomSpace,
            ILogService? logService = null)
        {
            var log = logService ?? _log;
            var spacesCriados = new List<ElementId>();

            // Filtrar Rooms sem Space
            var roomsSemSpace = rooms
                .Where(r => r.TipoElemento == TipoElemento.Room)
                .Where(r => !mapaRoomSpace.ContainsKey(r.ElementId))
                .Where(r => r.SpaceIdCorrespondente <= 0)
                .ToList();

            if (roomsSemSpace.Count == 0)
            {
                log.Info(ETAPA, COMPONENTE,
                    "Todos os Rooms já possuem Space correspondente. Nada a criar.");
                return spacesCriados;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Criando Spaces para {roomsSemSpace.Count} Rooms sem correspondência...");

            // Cache de Rooms do modelo
            var roomsCache = BuildRoomsCache(doc);

            using var trans = new Transaction(doc, "Criar Spaces Automáticos");

            try
            {
                trans.Start();

                foreach (var ambiente in roomsSemSpace)
                {
                    try
                    {
                        var spaceId = CriarSpaceIndividual(doc, ambiente, roomsCache, log);
                        if (spaceId != null)
                        {
                            spacesCriados.Add(spaceId);

                            // Atualizar mapa
                            mapaRoomSpace[ambiente.ElementId] = spaceId.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Critico(ETAPA, COMPONENTE,
                            $"Falha ao criar Space para Room {ambiente.ElementId} " +
                            $"('{ambiente.NomeOriginal}'): {ex.Message}",
                            ambiente.ElementId, ex.StackTrace);
                    }
                }

                if (spacesCriados.Count > 0)
                {
                    trans.Commit();
                    log.Info(ETAPA, COMPONENTE,
                        $"Transaction committed: {spacesCriados.Count} Spaces criados.");
                }
                else
                {
                    trans.RollBack();
                    log.Medio(ETAPA, COMPONENTE,
                        "Transaction rolled back — nenhum Space criado.");
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                log.Critico(ETAPA, COMPONENTE,
                    $"Transaction falhou: {ex.Message}",
                    detalhes: ex.StackTrace);
            }

            log.Info(ETAPA, COMPONENTE,
                $"Resultado: {spacesCriados.Count}/{roomsSemSpace.Count} Spaces criados.");

            return spacesCriados;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM LOTE (via AmbienteInfo)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Spaces para todas as Rooms sem Space associado.
        /// IMPORTANTE: Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ResultadoCriacaoSpaces CriarSpacesParaRoomsSemSpace(List<AmbienteInfo> ambientes)
        {
            var resultado = new ResultadoCriacaoSpaces
            {
                TotalRooms = ambientes.Count,
            };

            _log.Info(ETAPA, COMPONENTE,
                $"Iniciando criação de Spaces para {ambientes.Count} ambientes...");

            var roomsCache = BuildRoomsCache(_doc);

            foreach (var ambiente in ambientes)
            {
                // Pular se já possui Space
                if (ambiente.SpaceIdCorrespondente > 0)
                {
                    resultado.JaPossuiam++;
                    continue;
                }

                // Pular Spaces
                if (ambiente.TipoElemento != TipoElemento.Room)
                    continue;

                try
                {
                    var spaceId = CriarSpaceIndividual(_doc, ambiente, roomsCache, _log);
                    if (spaceId != null)
                    {
                        resultado.Criados++;
                        resultado.SpaceIdsCriados.Add(spaceId.Value);
                        resultado.SpaceElementIds.Add(spaceId);
                    }
                    else
                    {
                        resultado.Ignorados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.Falhas++;
                    resultado.Erros.Add($"{ambiente.NomeOriginal}: {ex.Message}");

                    _log.Critico(ETAPA, COMPONENTE,
                        $"Erro ao criar Space para '{ambiente.NomeOriginal}': {ex.Message}",
                        ambiente.ElementId, ex.StackTrace);
                }
            }

            _log.Info(ETAPA, COMPONENTE, $"Criação concluída: {resultado}");

            return resultado;
        }

        /// <summary>
        /// Executa criação de Spaces dentro de sua própria Transaction.
        /// </summary>
        public ResultadoCriacaoSpaces ExecutarComTransaction(List<AmbienteInfo> ambientes)
        {
            using var trans = new Transaction(_doc, "Criar MEP Spaces ausentes");

            try
            {
                trans.Start();

                var resultado = CriarSpacesParaRoomsSemSpace(ambientes);

                if (resultado.Sucesso || resultado.Criados > 0)
                {
                    trans.Commit();
                    _log.Info(ETAPA, COMPONENTE,
                        $"Transaction committed: {resultado.Criados} Spaces criados.");
                }
                else
                {
                    trans.RollBack();
                    _log.Medio(ETAPA, COMPONENTE,
                        "Transaction rolled back — nenhum Space criado.");
                }

                return resultado;
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                _log.Critico(ETAPA, COMPONENTE,
                    $"Transaction falhou: {ex.Message}",
                    detalhes: ex.StackTrace);

                return new ResultadoCriacaoSpaces
                {
                    TotalRooms = ambientes.Count,
                    Falhas = 1,
                    Erros = { ex.Message },
                };
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um Space para um ambiente individual.
        /// Retorna ElementId do Space criado ou null se ignorado.
        /// </summary>
        private ElementId? CriarSpaceIndividual(
            Document doc,
            AmbienteInfo ambiente,
            Dictionary<long, Room> roomsCache,
            ILogService log)
        {
            // 1. Encontrar Room no modelo
            if (!roomsCache.TryGetValue(ambiente.ElementId, out var room))
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Room não encontrada no modelo para ElementId {ambiente.ElementId}.",
                    ambiente.ElementId);
                return null;
            }

            // 2. Verificar Location
            if (room.Location == null)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Room '{ambiente.NomeOriginal}' sem Location — não colocada.",
                    ambiente.ElementId);
                return null;
            }

            // 3. Obter Level
            var level = doc.GetElement(room.LevelId) as Level;
            if (level == null)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Room '{ambiente.NomeOriginal}' sem Level definido.",
                    ambiente.ElementId);
                return null;
            }

            // 4. Obter centro geométrico
            var centro = ObterCentro(room);
            if (centro == null)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Não foi possível obter centro da Room '{ambiente.NomeOriginal}'.",
                    ambiente.ElementId);
                return null;
            }

            // 5. Criar Space
            var uv = new UV(centro.X, centro.Y);
            var space = doc.Create.NewSpace(level, uv);

            if (space == null)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Revit retornou null ao criar Space para '{ambiente.NomeOriginal}'.",
                    ambiente.ElementId);
                return null;
            }

            // 6. Configurar propriedades
            space.Name = ambiente.NomeOriginal;
            space.Number = ambiente.Numero;

            // 7. Atualizar AmbienteInfo
            ambiente.SpaceIdCorrespondente = space.Id.Value;
            ambiente.SpaceCriadoAutomaticamente = true;

            log.Info(ETAPA, COMPONENTE,
                $"Space criado automaticamente para Room {ambiente.ElementId} " +
                $"('{ambiente.NomeOriginal}') no Level '{level.Name}'. " +
                $"SpaceId={space.Id.Value}",
                ambiente.ElementId);

            return space.Id;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Monta cache de Rooms por ElementId para lookup rápido.
        /// </summary>
        private Dictionary<long, Room> BuildRoomsCache(Document doc)
        {
            var cache = new Dictionary<long, Room>();

            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            foreach (var element in collector)
            {
                if (element is Room room)
                    cache[room.Id.Value] = room;
            }

            _log.Info(ETAPA, COMPONENTE,
                $"Cache de Rooms criado: {cache.Count} elementos.");

            return cache;
        }

        /// <summary>
        /// Obtém o centro da Room (LocationPoint ou centroide do Solid).
        /// Retorna XYZ em unidades internas do Revit (pés).
        /// </summary>
        private XYZ? ObterCentro(Room room)
        {
            // Estratégia 1: LocationPoint
            if (room.Location is LocationPoint locationPoint)
                return locationPoint.Point;

            // Estratégia 2: Centroide do BoundingBox
            try
            {
                var bbox = room.get_BoundingBox(null);
                if (bbox != null)
                    return (bbox.Min + bbox.Max) / 2.0;
            }
            catch { /* fallback para geometria */ }

            // Estratégia 3: Centroide geométrico
            try
            {
                var options = new Options
                {
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = false,
                    DetailLevel = ViewDetailLevel.Fine,
                };

                var geom = room.get_Geometry(options);
                if (geom != null)
                {
                    foreach (var obj in geom)
                    {
                        if (obj is Solid solid && solid.Volume > 0)
                            return solid.ComputeCentroid();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Fallback geométrico falhou para Room '{room.Name}': {ex.Message}",
                    room.Id.Value);
            }

            return null;
        }
    }
}
