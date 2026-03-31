using System.Text.Json;
using System.Text.Json.Serialization;
using Revit2026.Modules.Rooms;

namespace Revit2026.Modules.Spaces
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO — ROOM ÓRFÃO
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Representa um Room válido que não possui Space MEP correspondente.
    /// Contém todas as informações necessárias para criação automática.
    /// </summary>
    public class OrphanRoom
    {
        [JsonPropertyName("roomId")]
        public long RoomId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        [JsonPropertyName("levelId")]
        public long LevelId { get; set; }

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("levelElevationM")]
        public double LevelElevationM { get; set; }

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("perimeterM")]
        public double PerimeterM { get; set; }

        [JsonPropertyName("centroid")]
        public OrphanCentroid Centroid { get; set; } = new();

        [JsonPropertyName("department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("phase")]
        public string Phase { get; set; } = "";

        [JsonPropertyName("isEnclosed")]
        public bool IsEnclosed { get; set; }

        [JsonPropertyName("orphanReason")]
        public string OrphanReason { get; set; } = "";

        [JsonPropertyName("orphanReasonCode")]
        public string OrphanReasonCode { get; set; } = "";

        [JsonPropertyName("nearestSpaceId")]
        public long? NearestSpaceId { get; set; }

        [JsonPropertyName("nearestSpaceDistanceM")]
        public double? NearestSpaceDistanceM { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "NORMAL";

        [JsonPropertyName("priorityEnum")]
        [JsonIgnore]
        public OrphanPriority PriorityEnum { get; set; } = OrphanPriority.Normal;
    }

    public class OrphanCentroid
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("z")]
        public double Z { get; set; }

        public OrphanCentroid() { }

        public OrphanCentroid(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public OrphanCentroid(double[] coords)
        {
            if (coords.Length >= 3)
            {
                X = coords[0]; Y = coords[1]; Z = coords[2];
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════════

    public enum OrphanReason
    {
        /// Nenhum Space no mesmo Level
        SemSpaceNoLevel,

        /// Spaces existem no Level mas todos estão além da distância máxima
        DistanciaExcedida,

        /// Todos os Spaces do Level já foram pareados com outros Rooms
        SpacesEsgotados,

        /// Room não possui centroid válido para matching
        CentroidInvalido,

        /// Correspondência anterior não encontrou match
        SemCorrespondencia
    }

    public enum OrphanPriority
    {
        /// Área grande, enclosed, com department — criação urgente
        Alta,

        /// Room padrão que precisa de Space
        Normal,

        /// Área pequena ou dados incompletos — criar por último
        Baixa
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class OrphanDetectionConfig
    {
        /// <summary>
        /// Percentual máximo de Rooms órfãos antes de emitir alerta.
        /// Ex: 0.30 = alertar se mais de 30% forem órfãos.
        /// </summary>
        [JsonPropertyName("orphanAlertThreshold")]
        public double OrphanAlertThreshold { get; set; } = 0.30;

        /// <summary>
        /// Quantidade absoluta máxima de órfãos antes de alertar.
        /// </summary>
        [JsonPropertyName("orphanAlertAbsolute")]
        public int OrphanAlertAbsolute { get; set; } = 20;

        /// <summary>
        /// Área mínima (m²) para considerar o Room como prioridade alta.
        /// </summary>
        [JsonPropertyName("highPriorityMinAreaM2")]
        public double HighPriorityMinAreaM2 { get; set; } = 4.0;

        /// <summary>
        /// Área máxima (m²) para considerar o Room como prioridade baixa.
        /// </summary>
        [JsonPropertyName("lowPriorityMaxAreaM2")]
        public double LowPriorityMaxAreaM2 { get; set; } = 1.5;

        /// <summary>
        /// Se true, inclui informação do Space mais próximo (mesmo que
        /// fora do raio de matching) para referência.
        /// </summary>
        [JsonPropertyName("includeNearestSpace")]
        public bool IncludeNearestSpace { get; set; } = true;
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DA DETECÇÃO
    // ══════════════════════════════════════════════════════════════

    public class OrphanDetectionResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalRoomsProcessed")]
        public int TotalRoomsProcessed { get; set; }

        [JsonPropertyName("orphanCount")]
        public int OrphanCount { get; set; }

        [JsonPropertyName("matchedCount")]
        public int MatchedCount { get; set; }

        [JsonPropertyName("orphanPercentage")]
        public double OrphanPercentage { get; set; }

        [JsonPropertyName("alertTriggered")]
        public bool AlertTriggered { get; set; }

        [JsonPropertyName("alertMessage")]
        public string AlertMessage { get; set; } = "";

        [JsonPropertyName("orphanRooms")]
        public List<OrphanRoom> OrphanRooms { get; set; } = new();

        [JsonPropertyName("prioritySummary")]
        public Dictionary<string, int> PrioritySummary { get; set; } = new();

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, OrphanLevelSummary> LevelSummary { get; set; } = new();

        [JsonPropertyName("reasonSummary")]
        public Dictionary<string, int> ReasonSummary { get; set; } = new();

        [JsonPropertyName("totalOrphanAreaM2")]
        public double TotalOrphanAreaM2 { get; set; }

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public List<OrphanRoom> GetByLevel(string levelName) =>
            OrphanRooms.Where(o =>
                string.Equals(o.LevelName, levelName,
                    StringComparison.OrdinalIgnoreCase)).ToList();

        public List<OrphanRoom> GetByPriority(OrphanPriority priority) =>
            OrphanRooms.Where(o => o.PriorityEnum == priority).ToList();

        public List<OrphanRoom> GetHighPriority() =>
            GetByPriority(OrphanPriority.Alta);

        public OrphanRoom? GetByRoomId(long roomId) =>
            OrphanRooms.FirstOrDefault(o => o.RoomId == roomId);
    }

    public class OrphanLevelSummary
    {
        [JsonPropertyName("totalOrphans")]
        public int TotalOrphans { get; set; }

        [JsonPropertyName("highPriority")]
        public int HighPriority { get; set; }

        [JsonPropertyName("normalPriority")]
        public int NormalPriority { get; set; }

        [JsonPropertyName("lowPriority")]
        public int LowPriority { get; set; }

        [JsonPropertyName("totalAreaM2")]
        public double TotalAreaM2 { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: DETECÇÃO DE ROOMS ÓRFÃOS
    // ══════════════════════════════════════════════════════════════

    public interface IOrphanRoomDetectorService
    {
        /// <summary>
        /// Detecta Rooms órfãos a partir do resultado de matching.
        /// </summary>
        OrphanDetectionResult Detectar(
            List<ValidRoom> rooms,
            RoomSpaceMatchResult matchResult,
            List<ValidSpace> spaces,
            OrphanDetectionConfig? config = null);

        /// <summary>
        /// Detecta Rooms órfãos diretamente a partir de listas,
        /// executando matching interno se necessário.
        /// </summary>
        OrphanDetectionResult Detectar(
            List<ValidRoom> rooms,
            List<ValidSpace> spaces,
            OrphanDetectionConfig? config = null);
    }

    public class OrphanRoomDetectorService : IOrphanRoomDetectorService
    {
        public event Action<string>? OnProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  DETECTAR COM MATCHING AUTOMÁTICO
        // ══════════════════════════════════════════════════════════

        public OrphanDetectionResult Detectar(
            List<ValidRoom> rooms,
            List<ValidSpace> spaces,
            OrphanDetectionConfig? config = null)
        {
            EmitProgress("Executando matching Room↔Space antes da detecção...");

            var matcher = new RoomSpaceMatcherService();
            var matchResult = matcher.Correlacionar(rooms, spaces);

            return Detectar(rooms, matchResult, spaces, config);
        }

        // ══════════════════════════════════════════════════════════
        //  DETECÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        public OrphanDetectionResult Detectar(
            List<ValidRoom> rooms,
            RoomSpaceMatchResult matchResult,
            List<ValidSpace> spaces,
            OrphanDetectionConfig? config = null)
        {
            config ??= new OrphanDetectionConfig();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = new OrphanDetectionResult
            {
                TotalRoomsProcessed = rooms.Count
            };

            EmitProgress($"Detectando Rooms órfãos em {rooms.Count} Rooms...");

            // ── 1. Indexar Rooms pareados ─────────────────────────
            var roomsPareados = matchResult.Matches
                .Where(m => m.StatusEnum == MatchStatus.Matched &&
                            m.SpaceId.HasValue)
                .Select(m => m.RoomId)
                .ToHashSet();

            result.MatchedCount = roomsPareados.Count;

            EmitProgress($"{roomsPareados.Count} Rooms já pareados. " +
                         "Processando restantes...");

            // ── 2. Processar cada Room ────────────────────────────
            foreach (var room in rooms)
            {
                if (roomsPareados.Contains(room.ElementId))
                    continue;

                // Room não pareado → criar OrphanRoom
                var orphan = CriarOrphanRoom(
                    room, matchResult, spaces, config);

                result.OrphanRooms.Add(orphan);
            }

            // ── 3. Estatísticas ───────────────────────────────────
            result.OrphanCount = result.OrphanRooms.Count;
            result.OrphanPercentage = rooms.Count > 0
                ? Math.Round(
                    (double)result.OrphanCount / rooms.Count * 100, 2)
                : 0;
            result.TotalOrphanAreaM2 = Math.Round(
                result.OrphanRooms.Sum(o => o.AreaM2), 2);
            result.Success = true;

            // ── 4. Resumo por prioridade ──────────────────────────
            foreach (var group in result.OrphanRooms
                         .GroupBy(o => o.Priority))
            {
                result.PrioritySummary[group.Key] = group.Count();
            }

            // ── 5. Resumo por Level ──────────────────────────────
            GerarResumoLevel(result);

            // ── 6. Resumo por motivo ─────────────────────────────
            foreach (var group in result.OrphanRooms
                         .GroupBy(o => o.OrphanReasonCode))
            {
                result.ReasonSummary[group.Key] = group.Count();
            }

            // ── 7. Verificar alerta ──────────────────────────────
            VerificarAlerta(result, config);

            // ── 8. Finalizar ─────────────────────────────────────
            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            // ── 9. Persistir ─────────────────────────────────────
            PersistirResultado(result);

            EmitProgress(
                $"Detecção concluída ({result.ExecutionTimeMs}ms): " +
                $"{result.OrphanCount} órfãos de {result.TotalRoomsProcessed} " +
                $"({result.OrphanPercentage:F1}%) | " +
                $"Área total: {result.TotalOrphanAreaM2:F2}m²" +
                (result.AlertTriggered
                    ? $" | ⚠ ALERTA: {result.AlertMessage}"
                    : ""));

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR ORPHAN ROOM
        // ══════════════════════════════════════════════════════════

        private static OrphanRoom CriarOrphanRoom(
            ValidRoom room,
            RoomSpaceMatchResult matchResult,
            List<ValidSpace> spaces,
            OrphanDetectionConfig config)
        {
            var orphan = new OrphanRoom
            {
                RoomId = room.ElementId,
                Name = room.Name,
                Number = room.Number,
                LevelId = room.LevelId,
                LevelName = room.LevelName,
                LevelElevationM = room.LevelElevationM,
                AreaM2 = room.AreaM2,
                PerimeterM = room.PerimeterM,
                Department = room.Department,
                Phase = room.Phase,
                IsEnclosed = room.IsEnclosed
            };

            // ── Centroid ─────────────────────────────────────────
            if (room.Centroid != null && room.Centroid.Length >= 3)
            {
                orphan.Centroid = new OrphanCentroid(room.Centroid);
            }

            // ── Determinar motivo ────────────────────────────────
            DeterminarMotivo(orphan, room, matchResult, spaces);

            // ── Determinar prioridade ────────────────────────────
            DeterminarPrioridade(orphan, config);

            // ── Space mais próximo (referência) ──────────────────
            if (config.IncludeNearestSpace)
            {
                EncontrarSpaceMaisProximo(orphan, room, spaces);
            }

            return orphan;
        }

        // ══════════════════════════════════════════════════════════
        //  DETERMINAR MOTIVO DO ORPHAN
        // ══════════════════════════════════════════════════════════

        private static void DeterminarMotivo(
            OrphanRoom orphan,
            ValidRoom room,
            RoomSpaceMatchResult matchResult,
            List<ValidSpace> spaces)
        {
            // Verificar se o centroid é inválido
            if (room.Centroid == null || room.Centroid.Length < 3)
            {
                orphan.OrphanReason = "Room sem centroid válido para matching";
                orphan.OrphanReasonCode = nameof(OrphanReason.CentroidInvalido);
                return;
            }

            // Verificar se existem Spaces no mesmo Level
            var spacesNoLevel = spaces
                .Where(s => s.LevelId == room.LevelId)
                .ToList();

            if (spacesNoLevel.Count == 0)
            {
                orphan.OrphanReason =
                    $"Nenhum Space MEP no Level '{room.LevelName}'";
                orphan.OrphanReasonCode =
                    nameof(OrphanReason.SemSpaceNoLevel);
                return;
            }

            // Verificar se todos os Spaces do Level já foram usados
            var spacesUsadosNoLevel = matchResult.Matches
                .Where(m => m.StatusEnum == MatchStatus.Matched &&
                            m.SpaceId.HasValue &&
                            string.Equals(m.RoomLevelName, room.LevelName,
                                StringComparison.OrdinalIgnoreCase))
                .Select(m => m.SpaceId!.Value)
                .ToHashSet();

            var spacesDisponiveis = spacesNoLevel
                .Where(s => !spacesUsadosNoLevel.Contains(s.ElementId))
                .ToList();

            if (spacesDisponiveis.Count == 0)
            {
                orphan.OrphanReason =
                    $"Todos os {spacesNoLevel.Count} Spaces do Level " +
                    $"'{room.LevelName}' já estão pareados";
                orphan.OrphanReasonCode =
                    nameof(OrphanReason.SpacesEsgotados);
                return;
            }

            // Verificar se a correspondência no matchResult indica distância
            var matchEntry = matchResult.GetByRoomId(room.ElementId);
            if (matchEntry != null && matchEntry.Distance.HasValue)
            {
                orphan.OrphanReason =
                    $"Space mais próximo a {matchEntry.Distance:F3}m " +
                    "excede distância máxima configurada";
                orphan.OrphanReasonCode =
                    nameof(OrphanReason.DistanciaExcedida);
                return;
            }

            // Fallback genérico
            orphan.OrphanReason = "Sem correspondência Room↔Space";
            orphan.OrphanReasonCode =
                nameof(OrphanReason.SemCorrespondencia);
        }

        // ══════════════════════════════════════════════════════════
        //  DETERMINAR PRIORIDADE
        // ══════════════════════════════════════════════════════════

        private static void DeterminarPrioridade(
            OrphanRoom orphan, OrphanDetectionConfig config)
        {
            // Alta: área significativa + enclosed + department definido
            if (orphan.AreaM2 >= config.HighPriorityMinAreaM2 &&
                orphan.IsEnclosed &&
                !string.IsNullOrWhiteSpace(orphan.Department))
            {
                orphan.PriorityEnum = OrphanPriority.Alta;
                orphan.Priority = "ALTA";
                return;
            }

            // Alta: área grande mesmo sem department
            if (orphan.AreaM2 >= config.HighPriorityMinAreaM2 * 2 &&
                orphan.IsEnclosed)
            {
                orphan.PriorityEnum = OrphanPriority.Alta;
                orphan.Priority = "ALTA";
                return;
            }

            // Baixa: área muito pequena ou não enclosed
            if (orphan.AreaM2 <= config.LowPriorityMaxAreaM2 ||
                !orphan.IsEnclosed)
            {
                orphan.PriorityEnum = OrphanPriority.Baixa;
                orphan.Priority = "BAIXA";
                return;
            }

            // Normal: todos os demais
            orphan.PriorityEnum = OrphanPriority.Normal;
            orphan.Priority = "NORMAL";
        }

        // ══════════════════════════════════════════════════════════
        //  SPACE MAIS PRÓXIMO (REFERÊNCIA)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Encontra o Space mais próximo do Room órfão, independente
        /// de Level ou exclusividade. Serve como referência para o
        /// operador e para scripts de criação automática.
        /// </summary>
        private static void EncontrarSpaceMaisProximo(
            OrphanRoom orphan, ValidRoom room, List<ValidSpace> spaces)
        {
            if (room.Centroid == null || room.Centroid.Length < 3)
                return;

            if (spaces.Count == 0)
                return;

            ValidSpace? melhor = null;
            double melhorDist = double.MaxValue;

            foreach (var space in spaces)
            {
                if (space.Centroid == null || space.Centroid.Length < 3)
                    continue;

                var dist = DistanciaEuclidiana(room.Centroid, space.Centroid);
                if (dist < melhorDist)
                {
                    melhorDist = dist;
                    melhor = space;
                }
            }

            if (melhor != null)
            {
                orphan.NearestSpaceId = melhor.ElementId;
                orphan.NearestSpaceDistanceM = Math.Round(melhorDist, 4);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  DISTÂNCIA EUCLIDIANA
        // ══════════════════════════════════════════════════════════

        private static double DistanciaEuclidiana(double[] a, double[] b)
        {
            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            var dz = a[2] - b[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO POR LEVEL
        // ══════════════════════════════════════════════════════════

        private static void GerarResumoLevel(OrphanDetectionResult result)
        {
            var levelGroups = result.OrphanRooms
                .GroupBy(o => o.LevelName)
                .ToList();

            foreach (var group in levelGroups)
            {
                var levelName = string.IsNullOrWhiteSpace(group.Key)
                    ? "(sem nível)"
                    : group.Key;

                result.LevelSummary[levelName] = new OrphanLevelSummary
                {
                    TotalOrphans = group.Count(),
                    HighPriority = group.Count(o =>
                        o.PriorityEnum == OrphanPriority.Alta),
                    NormalPriority = group.Count(o =>
                        o.PriorityEnum == OrphanPriority.Normal),
                    LowPriority = group.Count(o =>
                        o.PriorityEnum == OrphanPriority.Baixa),
                    TotalAreaM2 = Math.Round(
                        group.Sum(o => o.AreaM2), 2)
                };
            }
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAR ALERTA
        // ══════════════════════════════════════════════════════════

        private static void VerificarAlerta(
            OrphanDetectionResult result,
            OrphanDetectionConfig config)
        {
            var messages = new List<string>();

            // Alerta por percentual
            if (result.TotalRoomsProcessed > 0)
            {
                var pct = (double)result.OrphanCount /
                          result.TotalRoomsProcessed;

                if (pct > config.OrphanAlertThreshold)
                {
                    messages.Add(
                        $"{result.OrphanPercentage:F1}% de Rooms são órfãos " +
                        $"(limite: {config.OrphanAlertThreshold * 100:F0}%)");
                }
            }

            // Alerta por quantidade absoluta
            if (result.OrphanCount > config.OrphanAlertAbsolute)
            {
                messages.Add(
                    $"{result.OrphanCount} Rooms órfãos " +
                    $"(limite: {config.OrphanAlertAbsolute})");
            }

            // Alerta por Levels inteiros sem Space
            var levelsInteiramenteOrfaos = result.LevelSummary
                .Where(kvp => kvp.Value.TotalOrphans > 0 &&
                              kvp.Value.HighPriority > 0)
                .Select(kvp => kvp.Key)
                .ToList();

            if (levelsInteiramenteOrfaos.Count > 0)
            {
                messages.Add(
                    $"Levels com órfãos de alta prioridade: " +
                    string.Join(", ", levelsInteiramenteOrfaos));
            }

            if (messages.Count > 0)
            {
                result.AlertTriggered = true;
                result.AlertMessage = string.Join(" | ", messages);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PERSISTÊNCIA JSON
        // ══════════════════════════════════════════════════════════

        private void PersistirResultado(OrphanDetectionResult result)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Matching");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"orphan_rooms_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(result, JsonOpts);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                EmitProgress($"Resultado salvo: {filePath}");
            }
            catch
            {
                // não quebrar fluxo
            }
        }

        private void EmitProgress(string message)
        {
            OnProgress?.Invoke(message);
        }
    }
}
