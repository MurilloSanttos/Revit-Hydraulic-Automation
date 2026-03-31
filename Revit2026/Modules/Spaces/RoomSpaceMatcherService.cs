using System.Text.Json;
using System.Text.Json.Serialization;
using Revit2026.Modules.Rooms;

namespace Revit2026.Modules.Spaces
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO — CORRESPONDÊNCIA ROOM → SPACE
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Representa a correspondência entre um Room e um Space MEP.
    /// </summary>
    public class RoomSpaceMatch
    {
        [JsonPropertyName("roomId")]
        public long RoomId { get; set; }

        [JsonPropertyName("roomName")]
        public string RoomName { get; set; } = "";

        [JsonPropertyName("roomNumber")]
        public string RoomNumber { get; set; } = "";

        [JsonPropertyName("roomLevelName")]
        public string RoomLevelName { get; set; } = "";

        [JsonPropertyName("spaceId")]
        public long? SpaceId { get; set; }

        [JsonPropertyName("spaceName")]
        public string SpaceName { get; set; } = "";

        [JsonPropertyName("spaceNumber")]
        public string SpaceNumber { get; set; } = "";

        [JsonPropertyName("distance")]
        public double? Distance { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("statusEnum")]
        [JsonIgnore]
        public MatchStatus StatusEnum { get; set; } = MatchStatus.Orphan;
    }

    public enum MatchStatus
    {
        /// Room pareado com Space pelo centroid
        Matched,

        /// Room sem Space correspondente — criação necessária
        Orphan,

        /// Room descartado — dados insuficientes para matching
        Invalid
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO DO MATCHER
    // ══════════════════════════════════════════════════════════════

    public class RoomSpaceMatchConfig
    {
        /// <summary>
        /// Distância máxima (em metros) para aceitar correspondência.
        /// Acima disso, o Room é marcado como órfão.
        /// </summary>
        [JsonPropertyName("maxDistanceM")]
        public double MaxDistanceM { get; set; } = 5.0;

        /// <summary>
        /// Se true, exige que Room e Space estejam no mesmo Level
        /// para aceitar a correspondência.
        /// </summary>
        [JsonPropertyName("requireSameLevel")]
        public bool RequireSameLevel { get; set; } = true;

        /// <summary>
        /// Se true, permite que um Space seja pareado com no máximo 1 Room.
        /// Garante correspondência 1:1.
        /// </summary>
        [JsonPropertyName("exclusiveMatch")]
        public bool ExclusiveMatch { get; set; } = true;

        /// <summary>
        /// Tolerância de elevação (m) para considerar Levels compatíveis
        /// quando os IDs são diferentes mas a elevação é similar.
        /// </summary>
        [JsonPropertyName("levelElevationToleranceM")]
        public double LevelElevationToleranceM { get; set; } = 0.50;
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DO MATCHING
    // ══════════════════════════════════════════════════════════════

    public class RoomSpaceMatchResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonPropertyName("totalSpaces")]
        public int TotalSpaces { get; set; }

        [JsonPropertyName("matchedCount")]
        public int MatchedCount { get; set; }

        [JsonPropertyName("orphanCount")]
        public int OrphanCount { get; set; }

        [JsonPropertyName("invalidCount")]
        public int InvalidCount { get; set; }

        [JsonPropertyName("avgDistanceM")]
        public double AvgDistanceM { get; set; }

        [JsonPropertyName("maxDistanceM")]
        public double MaxDistanceM { get; set; }

        [JsonPropertyName("matches")]
        public List<RoomSpaceMatch> Matches { get; set; } = new();

        [JsonPropertyName("orphanRoomIds")]
        public List<long> OrphanRoomIds { get; set; } = new();

        [JsonPropertyName("unpairedSpaceIds")]
        public List<long> UnpairedSpaceIds { get; set; } = new();

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, LevelMatchSummary> LevelSummary { get; set; } = new();

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public List<RoomSpaceMatch> GetMatched() =>
            Matches.Where(m => m.StatusEnum == MatchStatus.Matched).ToList();

        public List<RoomSpaceMatch> GetOrphans() =>
            Matches.Where(m => m.StatusEnum == MatchStatus.Orphan).ToList();

        public RoomSpaceMatch? GetByRoomId(long roomId) =>
            Matches.FirstOrDefault(m => m.RoomId == roomId);

        public RoomSpaceMatch? GetBySpaceId(long spaceId) =>
            Matches.FirstOrDefault(m => m.SpaceId == spaceId);
    }

    public class LevelMatchSummary
    {
        [JsonPropertyName("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonPropertyName("matched")]
        public int Matched { get; set; }

        [JsonPropertyName("orphans")]
        public int Orphans { get; set; }

        [JsonPropertyName("spacesAvailable")]
        public int SpacesAvailable { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: CORRESPONDÊNCIA ROOM ↔ SPACE
    // ══════════════════════════════════════════════════════════════

    public interface IRoomSpaceMatcherService
    {
        /// <summary>
        /// Correlaciona Rooms válidos com Spaces existentes por proximidade.
        /// </summary>
        RoomSpaceMatchResult Correlacionar(
            List<ValidRoom> rooms,
            List<ValidSpace> spaces,
            RoomSpaceMatchConfig? config = null);

        /// <summary>
        /// Correlaciona a partir dos resultados de coleta já existentes.
        /// </summary>
        RoomSpaceMatchResult Correlacionar(
            RoomCollectionResult roomResult,
            SpaceCollectionResult spaceResult,
            RoomSpaceMatchConfig? config = null);
    }

    public class RoomSpaceMatcherService : IRoomSpaceMatcherService
    {
        public event Action<string>? OnProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  CORRELACIONAR A PARTIR DE RESULTADOS DE COLETA
        // ══════════════════════════════════════════════════════════

        public RoomSpaceMatchResult Correlacionar(
            RoomCollectionResult roomResult,
            SpaceCollectionResult spaceResult,
            RoomSpaceMatchConfig? config = null)
        {
            return Correlacionar(
                roomResult.ValidRooms,
                spaceResult.ValidSpaces,
                config);
        }

        // ══════════════════════════════════════════════════════════
        //  CORRELAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        public RoomSpaceMatchResult Correlacionar(
            List<ValidRoom> rooms,
            List<ValidSpace> spaces,
            RoomSpaceMatchConfig? config = null)
        {
            config ??= new RoomSpaceMatchConfig();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = new RoomSpaceMatchResult
            {
                TotalRooms = rooms.Count,
                TotalSpaces = spaces.Count
            };

            EmitProgress($"Iniciando correlação: {rooms.Count} Rooms × " +
                         $"{spaces.Count} Spaces...");

            if (rooms.Count == 0)
            {
                result.Success = false;
                EmitProgress("Nenhum Room para correlacionar.");
                sw.Stop();
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            // ── 1. Indexar Spaces por Level ───────────────────────
            var spacesByLevel = IndexarSpacesPorLevel(spaces);
            EmitProgress($"Spaces indexados em {spacesByLevel.Count} Levels.");

            // ── 2. Controle de exclusividade 1:1 ──────────────────
            var spacesUsados = new HashSet<long>();

            // ── 3. Processar cada Room ────────────────────────────
            foreach (var room in rooms)
            {
                var match = ProcessarRoom(
                    room, spaces, spacesByLevel, spacesUsados, config);

                result.Matches.Add(match);

                if (match.StatusEnum == MatchStatus.Matched)
                {
                    result.MatchedCount++;

                    if (config.ExclusiveMatch && match.SpaceId.HasValue)
                        spacesUsados.Add(match.SpaceId.Value);
                }
                else if (match.StatusEnum == MatchStatus.Orphan)
                {
                    result.OrphanCount++;
                    result.OrphanRoomIds.Add(match.RoomId);
                }
                else
                {
                    result.InvalidCount++;
                }
            }

            // ── 4. Identificar Spaces não pareados ────────────────
            var pairedSpaceIds = result.Matches
                .Where(m => m.SpaceId.HasValue)
                .Select(m => m.SpaceId!.Value)
                .ToHashSet();

            result.UnpairedSpaceIds = spaces
                .Select(s => s.ElementId)
                .Where(id => !pairedSpaceIds.Contains(id))
                .ToList();

            // ── 5. Estatísticas de distância ──────────────────────
            var matchedDistances = result.Matches
                .Where(m => m.Distance.HasValue && m.StatusEnum == MatchStatus.Matched)
                .Select(m => m.Distance!.Value)
                .ToList();

            if (matchedDistances.Count > 0)
            {
                result.AvgDistanceM = Math.Round(matchedDistances.Average(), 4);
                result.MaxDistanceM = Math.Round(matchedDistances.Max(), 4);
            }

            // ── 6. Resumo por Level ───────────────────────────────
            GerarResumoLevel(result, spacesByLevel);

            // ── 7. Finalizar ──────────────────────────────────────
            result.Success = result.MatchedCount > 0;

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            // ── 8. Persistir ──────────────────────────────────────
            PersistirResultado(result);

            EmitProgress(
                $"Correlação concluída ({result.ExecutionTimeMs}ms): " +
                $"{result.MatchedCount} pareados, " +
                $"{result.OrphanCount} órfãos, " +
                $"{result.UnpairedSpaceIds.Count} Spaces sem Room | " +
                $"Dist. média: {result.AvgDistanceM:F3}m");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  INDEXAÇÃO DE SPACES POR LEVEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Agrupa Spaces por LevelId e, como fallback, por elevação.
        /// </summary>
        private static Dictionary<long, List<ValidSpace>> IndexarSpacesPorLevel(
            List<ValidSpace> spaces)
        {
            var index = new Dictionary<long, List<ValidSpace>>();

            foreach (var space in spaces)
            {
                if (!index.ContainsKey(space.LevelId))
                    index[space.LevelId] = new List<ValidSpace>();

                index[space.LevelId].Add(space);
            }

            return index;
        }

        // ══════════════════════════════════════════════════════════
        //  PROCESSAMENTO DE ROOM INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        private static RoomSpaceMatch ProcessarRoom(
            ValidRoom room,
            List<ValidSpace> allSpaces,
            Dictionary<long, List<ValidSpace>> spacesByLevel,
            HashSet<long> spacesUsados,
            RoomSpaceMatchConfig config)
        {
            var match = new RoomSpaceMatch
            {
                RoomId = room.ElementId,
                RoomName = room.Name,
                RoomNumber = room.Number,
                RoomLevelName = room.LevelName
            };

            // ── Validar centroid do Room ──────────────────────────
            if (room.Centroid == null || room.Centroid.Length < 3)
            {
                match.StatusEnum = MatchStatus.Invalid;
                match.Status = "INVALID";
                return match;
            }

            // ── 1. Obter candidatos por Level ────────────────────
            var candidatos = ObterCandidatos(
                room, allSpaces, spacesByLevel, spacesUsados, config);

            if (candidatos.Count == 0)
            {
                match.StatusEnum = MatchStatus.Orphan;
                match.Status = "ORPHAN";
                return match;
            }

            // ── 2. Calcular distâncias ───────────────────────────
            var ranking = candidatos
                .Select(space => new
                {
                    Space = space,
                    Dist = DistanciaEuclidiana(room.Centroid, space.Centroid)
                })
                .OrderBy(x => x.Dist)
                .ToList();

            // ── 3. Selecionar melhor correspondência ─────────────
            var melhor = ranking.First();

            if (melhor.Dist > config.MaxDistanceM)
            {
                // Melhor candidato está além da distância máxima
                match.StatusEnum = MatchStatus.Orphan;
                match.Status = "ORPHAN";
                match.Distance = Math.Round(melhor.Dist, 4);
                return match;
            }

            // ── 4. Registrar correspondência ─────────────────────
            match.SpaceId = melhor.Space.ElementId;
            match.SpaceName = melhor.Space.Name;
            match.SpaceNumber = melhor.Space.Number;
            match.Distance = Math.Round(melhor.Dist, 4);
            match.StatusEnum = MatchStatus.Matched;
            match.Status = "MATCHED";

            return match;
        }

        // ══════════════════════════════════════════════════════════
        //  OBTER CANDIDATOS POR LEVEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna Spaces candidatos filtrados por:
        /// 1. Mesmo LevelId
        /// 2. Elevação compatível (fallback)
        /// 3. Exclusividade (se configurado)
        /// </summary>
        private static List<ValidSpace> ObterCandidatos(
            ValidRoom room,
            List<ValidSpace> allSpaces,
            Dictionary<long, List<ValidSpace>> spacesByLevel,
            HashSet<long> spacesUsados,
            RoomSpaceMatchConfig config)
        {
            var candidatos = new List<ValidSpace>();

            if (config.RequireSameLevel)
            {
                // ── Tentativa 1: Mesmo LevelId ───────────────────
                if (spacesByLevel.TryGetValue(room.LevelId, out var sameLevelSpaces))
                {
                    candidatos.AddRange(sameLevelSpaces);
                }

                // ── Tentativa 2: Elevação compatível ─────────────
                if (candidatos.Count == 0)
                {
                    foreach (var kvp in spacesByLevel)
                    {
                        if (kvp.Key == room.LevelId)
                            continue;

                        var sampleSpace = kvp.Value.FirstOrDefault();
                        if (sampleSpace == null)
                            continue;

                        var elevDiff = Math.Abs(
                            room.LevelElevationM - sampleSpace.LevelElevationM);

                        if (elevDiff <= config.LevelElevationToleranceM)
                        {
                            candidatos.AddRange(kvp.Value);
                        }
                    }
                }
            }
            else
            {
                // Sem restrição de Level — todos são candidatos
                candidatos.AddRange(allSpaces);
            }

            // ── Filtrar por centroid válido ───────────────────────
            candidatos = candidatos
                .Where(s => s.Centroid != null && s.Centroid.Length >= 3)
                .ToList();

            // ── Filtrar por exclusividade ─────────────────────────
            if (config.ExclusiveMatch)
            {
                candidatos = candidatos
                    .Where(s => !spacesUsados.Contains(s.ElementId))
                    .ToList();
            }

            return candidatos;
        }

        // ══════════════════════════════════════════════════════════
        //  DISTÂNCIA EUCLIDIANA 3D
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula distância euclidiana 3D entre dois pontos em metros.
        /// </summary>
        private static double DistanciaEuclidiana(
            double[] a, double[] b)
        {
            if (a.Length < 3 || b.Length < 3)
                return double.MaxValue;

            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            var dz = a[2] - b[2];

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO POR LEVEL
        // ══════════════════════════════════════════════════════════

        private static void GerarResumoLevel(
            RoomSpaceMatchResult result,
            Dictionary<long, List<ValidSpace>> spacesByLevel)
        {
            var levelGroups = result.Matches
                .GroupBy(m => m.RoomLevelName)
                .ToList();

            foreach (var group in levelGroups)
            {
                var levelName = group.Key;
                if (string.IsNullOrWhiteSpace(levelName))
                    levelName = "(sem nível)";

                var matched = group.Count(m =>
                    m.StatusEnum == MatchStatus.Matched);
                var orphans = group.Count(m =>
                    m.StatusEnum == MatchStatus.Orphan);

                // Contar Spaces disponíveis neste nível
                var spacesAvailable = spacesByLevel
                    .Where(kvp => kvp.Value.Any(s =>
                        string.Equals(s.LevelName, levelName,
                            StringComparison.OrdinalIgnoreCase)))
                    .SelectMany(kvp => kvp.Value)
                    .Count();

                result.LevelSummary[levelName] = new LevelMatchSummary
                {
                    TotalRooms = group.Count(),
                    Matched = matched,
                    Orphans = orphans,
                    SpacesAvailable = spacesAvailable
                };
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PERSISTÊNCIA JSON
        // ══════════════════════════════════════════════════════════

        private void PersistirResultado(RoomSpaceMatchResult result)
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
                    $"room_space_match_{DateTime.Now:yyyyMMdd_HHmmss}.json";
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
