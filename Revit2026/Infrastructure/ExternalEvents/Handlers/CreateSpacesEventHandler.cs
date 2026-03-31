using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginCore.Interfaces;
using Revit2026.Services;

namespace Revit2026.Infrastructure.ExternalEvents.Handlers
{
    /// <summary>
    /// ExternalEventHandler para criação automática de MEP Spaces.
    /// Consome SpaceManagerService no thread principal do Revit via
    /// BaseExternalEventHandler (fila thread-safe).
    ///
    /// Fluxo:
    /// 1. Lê Rooms do modelo (via RoomReaderService ou direto)
    /// 2. Lê Spaces existentes
    /// 3. Valida correspondência Room ↔ Space
    /// 4. Cria Spaces faltantes
    /// 5. Loga resultados detalhados
    /// </summary>
    public class CreateSpacesEventHandler : BaseExternalEventHandler
    {
        private readonly ILogService _log;

        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "CreateSpacesHandler";

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTOR
        // ══════════════════════════════════════════════════════════

        public CreateSpacesEventHandler(ILogService logService)
            : base(logService)
        {
            _log = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        // ══════════════════════════════════════════════════════════
        //  NOME DO HANDLER
        // ══════════════════════════════════════════════════════════

        public override string GetName() => "CreateSpacesEventHandler";

        // ══════════════════════════════════════════════════════════
        //  DISPARO DA OPERAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Enfileira a operação de criação de Spaces para execução
        /// no thread principal do Revit.
        /// </summary>
        public void ExecutarCriacaoDeSpaces()
        {
            EnfileirarAcao(app =>
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        "Nenhum documento ativo no Revit.");
                    return;
                }

                CriarSpacesFaltantes(doc);
            });
        }

        // ══════════════════════════════════════════════════════════
        //  LÓGICA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa o fluxo completo de criação de Spaces.
        /// Chamado internamente no thread do Revit.
        /// </summary>
        private void CriarSpacesFaltantes(Document doc)
        {
            _log.Info(ETAPA, COMPONENTE,
                "Iniciando criação automática de Spaces...");

            var spaceManager = new SpaceManagerService(doc, _log);

            try
            {
                // ── 1. Ler Rooms ──────────────────────────────
                var rooms = LerRooms(doc);

                if (rooms.Count == 0)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        "Nenhum Room encontrado no modelo. Operação cancelada.");
                    return;
                }

                _log.Info(ETAPA, COMPONENTE,
                    $"{rooms.Count} Rooms encontrados no modelo.");

                // ── 2. Ler Spaces existentes ──────────────────
                var spacesExistentes = spaceManager.LerSpaces(doc);

                _log.Info(ETAPA, COMPONENTE,
                    $"{spacesExistentes.Count} Spaces MEP já existentes.");

                // ── 3. Validar correspondência ────────────────
                var validacao = spaceManager.ValidarCorrespondencia(rooms, spacesExistentes);

                var roomsSemSpace = validacao.RoomsSemSpace;
                var correspondentes = validacao.Correspondentes.Count;
                var orfaos = validacao.SpacesOrfaos.Count;

                _log.Info(ETAPA, COMPONENTE,
                    $"Correspondência: {correspondentes} pares, " +
                    $"{roomsSemSpace.Count} Rooms sem Space, " +
                    $"{orfaos} Spaces órfãos.");

                // ── 4. Verificar se há Spaces a criar ─────────
                if (roomsSemSpace.Count == 0)
                {
                    _log.Info(ETAPA, COMPONENTE,
                        "Todos os Rooms já possuem Space correspondente. " +
                        "Nenhuma criação necessária.");
                    LogResumo(rooms.Count, spacesExistentes.Count, 0, 0);
                    return;
                }

                // ── 5. Filtrar Rooms válidos para criação ─────
                var roomsValidos = roomsSemSpace
                    .Where(r => r.PontoCentral != null)
                    .Where(r => r.Area > 0)
                    .Where(r => !string.IsNullOrEmpty(r.Nivel))
                    .ToList();

                var roomsIgnorados = roomsSemSpace.Count - roomsValidos.Count;

                if (roomsIgnorados > 0)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"{roomsIgnorados} Rooms ignorados por dados inválidos " +
                        $"(sem ponto central, área zero ou sem nível).");
                }

                if (roomsValidos.Count == 0)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        "Nenhum Room válido para criação de Space.");
                    LogResumo(rooms.Count, spacesExistentes.Count, 0, roomsIgnorados);
                    return;
                }

                // ── 6. Criar Spaces ───────────────────────────
                _log.Info(ETAPA, COMPONENTE,
                    $"Criando {roomsValidos.Count} Spaces...");

                using var trans = new Transaction(doc, "Criar Spaces Automáticos");
                trans.Start();

                try
                {
                    var spacesCriados = spaceManager.CriarSpacesParaRooms(roomsValidos);

                    trans.Commit();

                    _log.Info(ETAPA, COMPONENTE,
                        $"{spacesCriados.Count} Spaces criados com sucesso.");

                    LogResumo(rooms.Count, spacesExistentes.Count,
                        spacesCriados.Count, roomsIgnorados);
                }
                catch (Exception ex)
                {
                    if (trans.HasStarted())
                        trans.RollBack();

                    _log.Critico(ETAPA, COMPONENTE,
                        $"Transaction de criação falhou: {ex.Message}",
                        detalhes: ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    $"Erro na criação automática de Spaces: {ex.Message}",
                    detalhes: ex.StackTrace);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  LEITURA DE ROOMS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Lê todos os Rooms válidos do modelo.
        /// </summary>
        private List<PluginCore.Models.AmbienteInfo> LerRooms(Document doc)
        {
            var rooms = new List<PluginCore.Models.AmbienteInfo>();

            try
            {
                var collector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType();

                foreach (var elem in collector)
                {
                    if (elem is not Autodesk.Revit.DB.Architecture.Room room)
                        continue;

                    // Ignorar Rooms não colocados ou redundantes
                    if (room.Area <= 0)
                        continue;

                    if (room.Location == null)
                        continue;

                    var locationPoint = room.Location as LocationPoint;
                    var ponto = locationPoint?.Point;

                    var level = doc.GetElement(room.LevelId) as Level;

                    rooms.Add(new PluginCore.Models.AmbienteInfo
                    {
                        ElementId = room.Id.Value,
                        NomeOriginal = room.get_Parameter(
                            BuiltInParameter.ROOM_NAME)?.AsString() ?? room.Name,
                        Numero = room.Number ?? "",
                        Nivel = level?.Name ?? "",
                        Area = UnitUtils.ConvertFromInternalUnits(
                            room.Area, UnitTypeId.SquareMeters),
                        PontoCentral = ponto != null
                            ? new PluginCore.Models.PontoCentral
                            {
                                X = UnitUtils.ConvertFromInternalUnits(
                                    ponto.X, UnitTypeId.Meters),
                                Y = UnitUtils.ConvertFromInternalUnits(
                                    ponto.Y, UnitTypeId.Meters),
                                Z = UnitUtils.ConvertFromInternalUnits(
                                    ponto.Z, UnitTypeId.Meters)
                            }
                            : null
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    $"Erro ao ler Rooms: {ex.Message}",
                    detalhes: ex.StackTrace);
            }

            return rooms;
        }

        // ══════════════════════════════════════════════════════════
        //  LOG DE RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra resumo final da operação.
        /// </summary>
        private void LogResumo(
            int totalRooms,
            int spacesExistentes,
            int spacesCriados,
            int roomsIgnorados)
        {
            _log.Info(ETAPA, COMPONENTE,
                $"═══ Resumo Criação de Spaces ═══\n" +
                $"  Rooms analisados:     {totalRooms}\n" +
                $"  Spaces já existentes: {spacesExistentes}\n" +
                $"  Spaces criados:       {spacesCriados}\n" +
                $"  Rooms ignorados:      {roomsIgnorados}\n" +
                $"  Total Spaces final:   {spacesExistentes + spacesCriados}");
        }
    }
}
