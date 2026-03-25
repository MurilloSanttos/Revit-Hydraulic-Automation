using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Resultado da detecção de Spaces órfãos.
    /// </summary>
    public class ResultadoOrfaos
    {
        /// <summary>Total de Spaces analisados.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Spaces com Room correspondente válido.</summary>
        public int Validos { get; set; }

        /// <summary>Spaces órfãos detectados.</summary>
        public int Orfaos { get; set; }

        /// <summary>Spaces com Room inexistente no documento.</summary>
        public int RoomInexistente { get; set; }

        /// <summary>Spaces sem correspondência no mapa.</summary>
        public int SemCorrespondencia { get; set; }

        /// <summary>Spaces com Room inválido no modelo interno.</summary>
        public int RoomInvalido { get; set; }

        /// <summary>Erros durante verificação.</summary>
        public int Erros { get; set; }

        /// <summary>Detalhes dos Spaces órfãos.</summary>
        public List<SpaceOrfaoInfo> Detalhes { get; set; } = new();

        public override string ToString() =>
            $"Spaces: {Validos} válidos, {Orfaos} órfãos " +
            $"({SemCorrespondencia} sem match, {RoomInexistente} Room excluída, " +
            $"{RoomInvalido} Room inválida) de {TotalSpaces} total";
    }

    /// <summary>
    /// Informações de um Space órfão detectado.
    /// </summary>
    public class SpaceOrfaoInfo
    {
        public long SpaceId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Nivel { get; set; } = string.Empty;
        public double AreaM2 { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Serviço responsável pela detecção de Spaces órfãos —
    /// Spaces MEP que não possuem Room correspondente válido no modelo.
    ///
    /// Um Space é considerado órfão se:
    /// - Seu Id não aparece nos valores do mapa RoomId→SpaceId
    /// - OU o Room associado foi excluído do documento
    /// - OU o Room associado não existe na lista de AmbienteInfo válidos
    /// </summary>
    public class SpaceOrphanDetectorService
    {
        private readonly ILogService _log;

        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "OrphanDetector";

        public SpaceOrphanDetectorService(ILogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  DETECÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Detecta todos os Spaces órfãos no documento.
        /// Verifica correspondência contra o mapa RoomId→SpaceId
        /// e contra a lista de AmbienteInfo válidos.
        /// </summary>
        /// <param name="doc">Documento Revit ativo.</param>
        /// <param name="rooms">Lista de AmbienteInfo válidos (Rooms processados).</param>
        /// <param name="mapaRoomSpace">Mapa RoomId → SpaceId de correspondências existentes.</param>
        /// <param name="logService">Serviço de log (opcional, usa o do construtor).</param>
        /// <returns>Lista de Spaces órfãos.</returns>
        public List<Space> DetectarSpacesOrfaos(
            Document doc,
            List<AmbienteInfo> rooms,
            Dictionary<long, long> mapaRoomSpace,
            ILogService? logService = null)
        {
            var log = logService ?? _log;

            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (mapaRoomSpace == null) throw new ArgumentNullException(nameof(mapaRoomSpace));

            log.Info(ETAPA, COMPONENTE,
                "Iniciando detecção de Spaces órfãos...");

            // ── 1. Coletar todos os Spaces do modelo ──────────
            var todosSpaces = ColetarSpaces(doc);

            log.Info(ETAPA, COMPONENTE,
                $"{todosSpaces.Count} Spaces MEP encontrados no modelo.");

            if (todosSpaces.Count == 0)
            {
                log.Info(ETAPA, COMPONENTE,
                    "Nenhum Space no modelo. Detecção encerrada.");
                return new List<Space>();
            }

            // ── 2. Construir índices ──────────────────────────
            var spacesComMatch = new HashSet<long>(mapaRoomSpace.Values);
            var roomIdsValidos = new HashSet<long>(rooms.Select(r => r.ElementId));

            // Mapa inverso: SpaceId → RoomId
            var mapaSpaceRoom = new Dictionary<long, long>();
            foreach (var kvp in mapaRoomSpace)
            {
                mapaSpaceRoom[kvp.Value] = kvp.Key;
            }

            // ── 3. Verificar cada Space ───────────────────────
            var orfaos = new List<Space>();
            var resultado = new ResultadoOrfaos { TotalSpaces = todosSpaces.Count };

            foreach (var space in todosSpaces)
            {
                try
                {
                    var motivo = VerificarSpace(
                        space, doc, spacesComMatch, roomIdsValidos, mapaSpaceRoom);

                    if (motivo != null)
                    {
                        // Space órfão
                        orfaos.Add(space);
                        resultado.Orfaos++;
                        ClassificarMotivo(motivo, resultado);

                        var nivel = doc.GetElement(space.LevelId) as Level;

                        resultado.Detalhes.Add(new SpaceOrfaoInfo
                        {
                            SpaceId = space.Id.Value,
                            Nome = space.Name ?? string.Empty,
                            Numero = space.Number ?? string.Empty,
                            Nivel = nivel?.Name ?? "Sem Nível",
                            AreaM2 = UnitUtils.ConvertFromInternalUnits(
                                space.Area, UnitTypeId.SquareMeters),
                            Motivo = motivo
                        });

                        log.Medio(ETAPA, COMPONENTE,
                            $"Space órfão detectado: SpaceId={space.Id.Value}, " +
                            $"Nome='{space.Name}', " +
                            $"Level='{nivel?.Name ?? "?"}', " +
                            $"Motivo: {motivo}",
                            space.Id.Value);
                    }
                    else
                    {
                        resultado.Validos++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.Erros++;
                    log.Critico(ETAPA, COMPONENTE,
                        $"Erro ao verificar Space {space.Id.Value}: {ex.Message}",
                        space.Id.Value, ex.StackTrace);
                }
            }

            // ── 4. Resumo ────────────────────────────────────
            log.Info(ETAPA, COMPONENTE,
                $"Detecção concluída: {resultado}");

            if (orfaos.Count > 0)
            {
                var nomes = string.Join(", ",
                    resultado.Detalhes.Select(d =>
                        $"'{d.Nome}' ({d.Motivo})"));
                log.Medio(ETAPA, COMPONENTE,
                    $"Resumo órfãos: {nomes}");
            }

            return orfaos;
        }

        /// <summary>
        /// Sobrecarga simplificada usando apenas lista de AmbienteInfo.
        /// Constrói mapa interno a partir de SpaceIdCorrespondente.
        /// </summary>
        public List<Space> DetectarSpacesOrfaos(
            Document doc, List<AmbienteInfo> rooms)
        {
            var mapa = new Dictionary<long, long>();
            foreach (var room in rooms)
            {
                if (room.SpaceIdCorrespondente > 0)
                    mapa[room.ElementId] = room.SpaceIdCorrespondente;
            }

            return DetectarSpacesOrfaos(doc, rooms, mapa);
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se um Space é órfão.
        /// Retorna null se válido, ou o motivo se órfão.
        /// </summary>
        private static string? VerificarSpace(
            Space space,
            Document doc,
            HashSet<long> spacesComMatch,
            HashSet<long> roomIdsValidos,
            Dictionary<long, long> mapaSpaceRoom)
        {
            var spaceId = space.Id.Value;

            // V1: Space não está no mapa de correspondência
            if (!spacesComMatch.Contains(spaceId))
                return "sem correspondência no mapa Room↔Space.";

            // V2: Tem correspondência, verificar se o Room existe
            if (!mapaSpaceRoom.TryGetValue(spaceId, out var roomId))
                return "mapa inverso inconsistente.";

            // V3: Room existe no documento?
            var roomElement = doc.GetElement(new ElementId(roomId));
            if (roomElement == null)
                return $"Room associado (Id={roomId}) não existe mais no documento.";

            // V4: Room está na lista de AmbienteInfo válidos?
            if (!roomIdsValidos.Contains(roomId))
                return $"Room associado (Id={roomId}) não é válido no modelo interno.";

            // V5: Space tem localização?
            if (space.Location == null)
                return "Space sem localização (não colocado).";

            // V6: Space tem área?
            if (space.Area <= 0)
                return "Space com área zero.";

            return null; // Space válido
        }

        // ══════════════════════════════════════════════════════════
        //  COLETA DE SPACES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Coleta todos os MEP Spaces do documento.
        /// </summary>
        private static List<Space> ColetarSpaces(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .OfType<Space>()
                .ToList();
        }

        // ══════════════════════════════════════════════════════════
        //  CLASSIFICAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Classifica o motivo de orfandade para contabilização.
        /// </summary>
        private static void ClassificarMotivo(string motivo, ResultadoOrfaos resultado)
        {
            if (motivo.Contains("sem correspondência"))
                resultado.SemCorrespondencia++;
            else if (motivo.Contains("não existe mais"))
                resultado.RoomInexistente++;
            else if (motivo.Contains("não é válido"))
                resultado.RoomInvalido++;
            else
                resultado.SemCorrespondencia++;
        }

        // ══════════════════════════════════════════════════════════
        //  REMOÇÃO (OPCIONAL)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Remove Spaces órfãos do documento.
        /// Deve ser chamado dentro de uma Transaction.
        /// </summary>
        public int RemoverSpacesOrfaos(
            Document doc, List<Space> spacesOrfaos)
        {
            if (spacesOrfaos.Count == 0)
                return 0;

            _log.Info(ETAPA, COMPONENTE,
                $"Removendo {spacesOrfaos.Count} Spaces órfãos...");

            int removidos = 0;

            foreach (var space in spacesOrfaos)
            {
                try
                {
                    var nome = space.Name;
                    doc.Delete(space.Id);
                    removidos++;

                    _log.Info(ETAPA, COMPONENTE,
                        $"Space órfão removido: '{nome}' (Id={space.Id.Value})");
                }
                catch (Exception ex)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Erro ao remover Space órfão Id={space.Id.Value}: {ex.Message}",
                        space.Id.Value);
                }
            }

            _log.Info(ETAPA, COMPONENTE,
                $"{removidos}/{spacesOrfaos.Count} Spaces órfãos removidos.");

            return removidos;
        }

        /// <summary>
        /// Remove Spaces órfãos com Transaction interna.
        /// </summary>
        public int RemoverComTransaction(Document doc, List<Space> spacesOrfaos)
        {
            if (spacesOrfaos.Count == 0)
                return 0;

            using var trans = new Transaction(doc, "Remover Spaces Órfãos");

            try
            {
                trans.Start();
                var removidos = RemoverSpacesOrfaos(doc, spacesOrfaos);

                if (removidos > 0)
                    trans.Commit();
                else
                    trans.RollBack();

                return removidos;
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                _log.Critico(ETAPA, COMPONENTE,
                    $"Transaction de remoção falhou: {ex.Message}",
                    stackTrace: ex.StackTrace);

                return 0;
            }
        }
    }
}
