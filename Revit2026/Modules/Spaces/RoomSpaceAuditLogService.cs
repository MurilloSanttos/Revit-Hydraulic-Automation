using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Revit2026.Modules.Rooms;

namespace Revit2026.Modules.Spaces
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO — LOG CONSOLIDADO
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Log consolidado de toda a pipeline Room↔Space.
    /// Agrega matching, detecção de órfãos e criação automática.
    /// </summary>
    public class RoomSpaceAuditLog
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("pluginName")]
        public string PluginName { get; set; } = "HermesMEP";

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = "";

        [JsonPropertyName("projectPath")]
        public string ProjectPath { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("timestampLocal")]
        public string TimestampLocal { get; set; } =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // ── Dados de entrada ─────────────────────────────────────

        [JsonPropertyName("inputRooms")]
        public List<RoomLogEntry> InputRooms { get; set; } = new();

        [JsonPropertyName("inputSpaces")]
        public List<SpaceLogEntry> InputSpaces { get; set; } = new();

        // ── Resultados de matching ───────────────────────────────

        [JsonPropertyName("matchedPairs")]
        public List<MatchedPairEntry> MatchedPairs { get; set; } = new();

        // ── Órfãos ───────────────────────────────────────────────

        [JsonPropertyName("orphanRooms")]
        public List<OrphanRoomLogEntry> OrphanRooms { get; set; } = new();

        [JsonPropertyName("orphanSpaces")]
        public List<OrphanSpaceLogEntry> OrphanSpaces { get; set; } = new();

        // ── Criação automática ───────────────────────────────────

        [JsonPropertyName("autoCreatedSpaces")]
        public List<CreatedSpaceLogEntry> AutoCreatedSpaces { get; set; } = new();

        // ── Resumo ───────────────────────────────────────────────

        [JsonPropertyName("summary")]
        public AuditSummary Summary { get; set; } = new();

        // ── Eventos de timeline ──────────────────────────────────

        [JsonPropertyName("timeline")]
        public List<TimelineEvent> Timeline { get; set; } = new();

        // ── Metadados de execução ────────────────────────────────

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  SUB-MODELOS DO LOG
    // ══════════════════════════════════════════════════════════════

    public class RoomLogEntry
    {
        [JsonPropertyName("elementId")]
        public long ElementId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("centroid")]
        public double[] Centroid { get; set; } = Array.Empty<double>();

        [JsonPropertyName("matchedSpaceId")]
        public long? MatchedSpaceId { get; set; }

        [JsonPropertyName("matchDistance")]
        public double? MatchDistance { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";
    }

    public class SpaceLogEntry
    {
        [JsonPropertyName("elementId")]
        public long ElementId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("centroid")]
        public double[] Centroid { get; set; } = Array.Empty<double>();

        [JsonPropertyName("matchedRoomId")]
        public long? MatchedRoomId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";
    }

    public class MatchedPairEntry
    {
        [JsonPropertyName("roomId")]
        public long RoomId { get; set; }

        [JsonPropertyName("roomName")]
        public string RoomName { get; set; } = "";

        [JsonPropertyName("spaceId")]
        public long SpaceId { get; set; }

        [JsonPropertyName("spaceName")]
        public string SpaceName { get; set; } = "";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }

    public class OrphanRoomLogEntry
    {
        [JsonPropertyName("roomId")]
        public long RoomId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("centroid")]
        public double[] Centroid { get; set; } = Array.Empty<double>();

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "";

        [JsonPropertyName("nearestSpaceId")]
        public long? NearestSpaceId { get; set; }

        [JsonPropertyName("nearestSpaceDistanceM")]
        public double? NearestSpaceDistanceM { get; set; }
    }

    public class OrphanSpaceLogEntry
    {
        [JsonPropertyName("spaceId")]
        public long SpaceId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("centroid")]
        public double[] Centroid { get; set; } = Array.Empty<double>();

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = "";

        [JsonPropertyName("nearestRoomId")]
        public long? NearestRoomId { get; set; }

        [JsonPropertyName("nearestRoomDistanceM")]
        public double? NearestRoomDistanceM { get; set; }
    }

    public class CreatedSpaceLogEntry
    {
        [JsonPropertyName("roomId")]
        public long RoomId { get; set; }

        [JsonPropertyName("roomName")]
        public string RoomName { get; set; } = "";

        [JsonPropertyName("spaceId")]
        public long SpaceId { get; set; }

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    public class AuditSummary
    {
        [JsonPropertyName("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonPropertyName("totalSpaces")]
        public int TotalSpaces { get; set; }

        [JsonPropertyName("totalMatched")]
        public int TotalMatched { get; set; }

        [JsonPropertyName("totalOrphanRooms")]
        public int TotalOrphanRooms { get; set; }

        [JsonPropertyName("totalOrphanSpaces")]
        public int TotalOrphanSpaces { get; set; }

        [JsonPropertyName("totalAutoCreated")]
        public int TotalAutoCreated { get; set; }

        [JsonPropertyName("totalAutoCreatedFailed")]
        public int TotalAutoCreatedFailed { get; set; }

        [JsonPropertyName("matchPercentage")]
        public double MatchPercentage { get; set; }

        [JsonPropertyName("orphanRoomPercentage")]
        public double OrphanRoomPercentage { get; set; }

        [JsonPropertyName("orphanSpacePercentage")]
        public double OrphanSpacePercentage { get; set; }

        [JsonPropertyName("avgMatchDistanceM")]
        public double AvgMatchDistanceM { get; set; }

        [JsonPropertyName("maxMatchDistanceM")]
        public double MaxMatchDistanceM { get; set; }

        [JsonPropertyName("totalMatchedAreaM2")]
        public double TotalMatchedAreaM2 { get; set; }

        [JsonPropertyName("totalOrphanRoomAreaM2")]
        public double TotalOrphanRoomAreaM2 { get; set; }

        [JsonPropertyName("totalCreatedAreaM2")]
        public double TotalCreatedAreaM2 { get; set; }

        [JsonPropertyName("levelBreakdown")]
        public Dictionary<string, LevelBreakdown> LevelBreakdown { get; set; } = new();
    }

    public class LevelBreakdown
    {
        [JsonPropertyName("rooms")]
        public int Rooms { get; set; }

        [JsonPropertyName("spaces")]
        public int Spaces { get; set; }

        [JsonPropertyName("matched")]
        public int Matched { get; set; }

        [JsonPropertyName("orphanRooms")]
        public int OrphanRooms { get; set; }

        [JsonPropertyName("orphanSpaces")]
        public int OrphanSpaces { get; set; }

        [JsonPropertyName("created")]
        public int Created { get; set; }
    }

    public class TimelineEvent
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";

        [JsonPropertyName("elapsedMs")]
        public long ElapsedMs { get; set; }

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO DO LOGGER
    // ══════════════════════════════════════════════════════════════

    public class AuditLogConfig
    {
        /// <summary>
        /// Exportar JSON detalhado.
        /// </summary>
        public bool ExportJson { get; set; } = true;

        /// <summary>
        /// Exportar CSV com dados tabulares.
        /// </summary>
        public bool ExportCsv { get; set; } = true;

        /// <summary>
        /// Exportar relatório em texto legível.
        /// </summary>
        public bool ExportText { get; set; } = true;

        /// <summary>
        /// Diretório de saída. Se vazio, usa %APPDATA%/HermesMEP/Audit.
        /// </summary>
        public string OutputDirectory { get; set; } = "";

        /// <summary>
        /// Prefixo dos arquivos gerados.
        /// </summary>
        public string FilePrefix { get; set; } = "room_space_audit";

        /// <summary>
        /// Se true, inclui centroid nos CSVs.
        /// </summary>
        public bool IncludeCentroidInCsv { get; set; } = true;
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: AUDIT LOG
    // ══════════════════════════════════════════════════════════════

    public interface IRoomSpaceAuditLogService
    {
        /// <summary>
        /// Gera log consolidado a partir dos resultados de cada etapa.
        /// </summary>
        RoomSpaceAuditLog GerarLog(
            List<ValidRoom> rooms,
            List<ValidSpace> spaces,
            RoomSpaceMatchResult? matchResult,
            OrphanDetectionResult? orphanRoomResult,
            OrphanSpaceDetectionResult? orphanSpaceResult,
            AutoSpaceCreationResult? creationResult,
            string projectName = "",
            string projectPath = "");

        /// <summary>
        /// Exporta o log em todos os formatos configurados.
        /// </summary>
        List<string> Exportar(
            RoomSpaceAuditLog log,
            AuditLogConfig? config = null);
    }

    public class RoomSpaceAuditLogService : IRoomSpaceAuditLogService
    {
        public event Action<string>? OnProgress;

        private readonly object _lock = new();
        private readonly System.Diagnostics.Stopwatch _sw = new();

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  GERAÇÃO DO LOG CONSOLIDADO
        // ══════════════════════════════════════════════════════════

        public RoomSpaceAuditLog GerarLog(
            List<ValidRoom> rooms,
            List<ValidSpace> spaces,
            RoomSpaceMatchResult? matchResult,
            OrphanDetectionResult? orphanRoomResult,
            OrphanSpaceDetectionResult? orphanSpaceResult,
            AutoSpaceCreationResult? creationResult,
            string projectName = "",
            string projectPath = "")
        {
            _sw.Restart();

            var log = new RoomSpaceAuditLog
            {
                ProjectName = projectName,
                ProjectPath = projectPath
            };

            EmitProgress("Gerando log de auditoria Room↔Space...");

            // ── 1. Registrar Rooms de entrada ────────────────────
            AddTimeline(log, "INPUT", "Registrando Rooms de entrada",
                rooms.Count);
            RegistrarRooms(log, rooms, matchResult);

            // ── 2. Registrar Spaces de entrada ───────────────────
            AddTimeline(log, "INPUT", "Registrando Spaces de entrada",
                spaces.Count);
            RegistrarSpaces(log, spaces, matchResult);

            // ── 3. Registrar correspondências ────────────────────
            if (matchResult != null)
            {
                AddTimeline(log, "MATCHING",
                    "Registrando correspondências",
                    matchResult.MatchedCount);
                RegistrarCorrespondencias(log, matchResult);
            }

            // ── 4. Registrar Rooms órfãos ────────────────────────
            if (orphanRoomResult != null)
            {
                AddTimeline(log, "ORPHAN_ROOMS",
                    "Registrando Rooms órfãos",
                    orphanRoomResult.OrphanCount);
                RegistrarOrphanRooms(log, orphanRoomResult);
            }

            // ── 5. Registrar Spaces órfãos ───────────────────────
            if (orphanSpaceResult != null)
            {
                AddTimeline(log, "ORPHAN_SPACES",
                    "Registrando Spaces órfãos",
                    orphanSpaceResult.OrphanCount);
                RegistrarOrphanSpaces(log, orphanSpaceResult);
            }

            // ── 6. Registrar criações automáticas ────────────────
            if (creationResult != null)
            {
                AddTimeline(log, "AUTO_CREATE",
                    "Registrando Spaces criados",
                    creationResult.TotalCreated);
                RegistrarCriacoes(log, creationResult);
            }

            // ── 7. Gerar resumo ──────────────────────────────────
            AddTimeline(log, "SUMMARY", "Calculando resumo");
            GerarResumo(log, rooms, spaces, matchResult,
                orphanRoomResult, orphanSpaceResult, creationResult);

            _sw.Stop();
            log.ExecutionTimeMs = _sw.ElapsedMilliseconds;

            AddTimeline(log, "DONE",
                $"Log gerado em {log.ExecutionTimeMs}ms");

            EmitProgress(
                $"Log gerado: {log.Summary.TotalRooms} Rooms, " +
                $"{log.Summary.TotalMatched} pareados, " +
                $"{log.Summary.TotalOrphanRooms} órfãos Room, " +
                $"{log.Summary.TotalOrphanSpaces} órfãos Space, " +
                $"{log.Summary.TotalAutoCreated} criados.");

            return log;
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRAR ROOMS
        // ══════════════════════════════════════════════════════════

        private static void RegistrarRooms(
            RoomSpaceAuditLog log,
            List<ValidRoom> rooms,
            RoomSpaceMatchResult? matchResult)
        {
            foreach (var room in rooms)
            {
                var entry = new RoomLogEntry
                {
                    ElementId = room.ElementId,
                    Name = room.Name,
                    Number = room.Number,
                    LevelName = room.LevelName,
                    AreaM2 = room.AreaM2,
                    Centroid = room.Centroid
                };

                // Enriquecer com dados de matching
                if (matchResult != null)
                {
                    var match = matchResult.GetByRoomId(room.ElementId);
                    if (match != null)
                    {
                        entry.MatchedSpaceId = match.SpaceId;
                        entry.MatchDistance = match.Distance;
                        entry.Status = match.Status;
                    }
                    else
                    {
                        entry.Status = "NOT_PROCESSED";
                    }
                }
                else
                {
                    entry.Status = "PENDING";
                }

                log.InputRooms.Add(entry);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRAR SPACES
        // ══════════════════════════════════════════════════════════

        private static void RegistrarSpaces(
            RoomSpaceAuditLog log,
            List<ValidSpace> spaces,
            RoomSpaceMatchResult? matchResult)
        {
            // Indexar Spaces pareados
            var pairedSpaces = new Dictionary<long, RoomSpaceMatch>();

            if (matchResult != null)
            {
                foreach (var match in matchResult.Matches)
                {
                    if (match.SpaceId.HasValue &&
                        match.StatusEnum == MatchStatus.Matched)
                    {
                        pairedSpaces[match.SpaceId.Value] = match;
                    }
                }
            }

            foreach (var space in spaces)
            {
                var entry = new SpaceLogEntry
                {
                    ElementId = space.ElementId,
                    Name = space.Name,
                    Number = space.Number,
                    LevelName = space.LevelName,
                    AreaM2 = space.AreaM2,
                    Centroid = space.Centroid
                };

                if (pairedSpaces.TryGetValue(
                    space.ElementId, out var match))
                {
                    entry.MatchedRoomId = match.RoomId;
                    entry.Status = "MATCHED";
                }
                else
                {
                    entry.Status = matchResult != null
                        ? "ORPHAN" : "PENDING";
                }

                log.InputSpaces.Add(entry);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRAR CORRESPONDÊNCIAS
        // ══════════════════════════════════════════════════════════

        private static void RegistrarCorrespondencias(
            RoomSpaceAuditLog log,
            RoomSpaceMatchResult matchResult)
        {
            foreach (var match in matchResult.GetMatched())
            {
                if (!match.SpaceId.HasValue) continue;

                log.MatchedPairs.Add(new MatchedPairEntry
                {
                    RoomId = match.RoomId,
                    RoomName = match.RoomName,
                    SpaceId = match.SpaceId.Value,
                    SpaceName = match.SpaceName,
                    LevelName = match.RoomLevelName,
                    Distance = match.Distance ?? 0
                });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRAR ROOMS ÓRFÃOS
        // ══════════════════════════════════════════════════════════

        private static void RegistrarOrphanRooms(
            RoomSpaceAuditLog log,
            OrphanDetectionResult orphanResult)
        {
            foreach (var orphan in orphanResult.OrphanRooms)
            {
                log.OrphanRooms.Add(new OrphanRoomLogEntry
                {
                    RoomId = orphan.RoomId,
                    Name = orphan.Name,
                    Number = orphan.Number,
                    LevelName = orphan.LevelName,
                    Centroid = new[]
                    {
                        orphan.Centroid.X,
                        orphan.Centroid.Y,
                        orphan.Centroid.Z
                    },
                    AreaM2 = orphan.AreaM2,
                    Reason = orphan.OrphanReason,
                    Priority = orphan.Priority,
                    NearestSpaceId = orphan.NearestSpaceId,
                    NearestSpaceDistanceM = orphan.NearestSpaceDistanceM
                });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRAR SPACES ÓRFÃOS
        // ══════════════════════════════════════════════════════════

        private static void RegistrarOrphanSpaces(
            RoomSpaceAuditLog log,
            OrphanSpaceDetectionResult orphanResult)
        {
            foreach (var orphan in orphanResult.OrphanSpaces)
            {
                log.OrphanSpaces.Add(new OrphanSpaceLogEntry
                {
                    SpaceId = orphan.SpaceId,
                    Name = orphan.Name,
                    Number = orphan.Number,
                    LevelName = orphan.LevelName,
                    Centroid = new[]
                    {
                        orphan.Centroid.X,
                        orphan.Centroid.Y,
                        orphan.Centroid.Z
                    },
                    AreaM2 = orphan.AreaM2,
                    Reason = orphan.OrphanReason,
                    Recommendation = orphan.Recommendation,
                    NearestRoomId = orphan.NearestRoomId,
                    NearestRoomDistanceM = orphan.NearestRoomDistanceM
                });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRAR CRIAÇÕES AUTOMÁTICAS
        // ══════════════════════════════════════════════════════════

        private static void RegistrarCriacoes(
            RoomSpaceAuditLog log,
            AutoSpaceCreationResult creationResult)
        {
            foreach (var entry in creationResult.Entries)
            {
                log.AutoCreatedSpaces.Add(new CreatedSpaceLogEntry
                {
                    RoomId = entry.RoomId,
                    RoomName = entry.RoomName,
                    SpaceId = entry.SpaceId,
                    LevelName = entry.LevelName,
                    AreaM2 = entry.AreaM2,
                    Status = entry.Status,
                    ErrorMessage = entry.ErrorMessage
                });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  GERAR RESUMO
        // ══════════════════════════════════════════════════════════

        private static void GerarResumo(
            RoomSpaceAuditLog log,
            List<ValidRoom> rooms,
            List<ValidSpace> spaces,
            RoomSpaceMatchResult? matchResult,
            OrphanDetectionResult? orphanRoomResult,
            OrphanSpaceDetectionResult? orphanSpaceResult,
            AutoSpaceCreationResult? creationResult)
        {
            var s = log.Summary;

            s.TotalRooms = rooms.Count;
            s.TotalSpaces = spaces.Count;
            s.TotalMatched = matchResult?.MatchedCount ?? 0;
            s.TotalOrphanRooms = orphanRoomResult?.OrphanCount ?? 0;
            s.TotalOrphanSpaces = orphanSpaceResult?.OrphanCount ?? 0;
            s.TotalAutoCreated = creationResult?.TotalCreated ?? 0;
            s.TotalAutoCreatedFailed = creationResult?.TotalFailed ?? 0;

            // Percentuais
            s.MatchPercentage = rooms.Count > 0
                ? Math.Round((double)s.TotalMatched / rooms.Count * 100, 2)
                : 0;
            s.OrphanRoomPercentage = rooms.Count > 0
                ? Math.Round((double)s.TotalOrphanRooms / rooms.Count * 100, 2)
                : 0;
            s.OrphanSpacePercentage = spaces.Count > 0
                ? Math.Round((double)s.TotalOrphanSpaces / spaces.Count * 100, 2)
                : 0;

            // Distâncias
            if (matchResult != null)
            {
                s.AvgMatchDistanceM = matchResult.AvgDistanceM;
                s.MaxMatchDistanceM = matchResult.MaxDistanceM;
            }

            // Áreas
            s.TotalMatchedAreaM2 = Math.Round(
                log.MatchedPairs.Sum(p =>
                    rooms.FirstOrDefault(r =>
                        r.ElementId == p.RoomId)?.AreaM2 ?? 0), 2);
            s.TotalOrphanRoomAreaM2 = Math.Round(
                log.OrphanRooms.Sum(o => o.AreaM2), 2);
            s.TotalCreatedAreaM2 = creationResult?.CreatedAreaM2 ?? 0;

            // Breakdown por Level
            var allLevels = rooms.Select(r => r.LevelName)
                .Concat(spaces.Select(s2 => s2.LevelName))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var level in allLevels)
            {
                var lb = new LevelBreakdown
                {
                    Rooms = rooms.Count(r =>
                        string.Equals(r.LevelName, level,
                            StringComparison.OrdinalIgnoreCase)),
                    Spaces = spaces.Count(sp =>
                        string.Equals(sp.LevelName, level,
                            StringComparison.OrdinalIgnoreCase)),
                    Matched = log.MatchedPairs.Count(p =>
                        string.Equals(p.LevelName, level,
                            StringComparison.OrdinalIgnoreCase)),
                    OrphanRooms = log.OrphanRooms.Count(o =>
                        string.Equals(o.LevelName, level,
                            StringComparison.OrdinalIgnoreCase)),
                    OrphanSpaces = log.OrphanSpaces.Count(o =>
                        string.Equals(o.LevelName, level,
                            StringComparison.OrdinalIgnoreCase)),
                    Created = log.AutoCreatedSpaces.Count(c =>
                        string.Equals(c.LevelName, level,
                            StringComparison.OrdinalIgnoreCase) &&
                        c.Status == "CRIADO")
                };

                s.LevelBreakdown[level] = lb;
            }

            // Warnings
            if (s.OrphanRoomPercentage > 30)
            {
                log.Warnings.Add(
                    $"⚠ {s.OrphanRoomPercentage:F1}% de Rooms são órfãos");
            }

            if (s.OrphanSpacePercentage > 40)
            {
                log.Warnings.Add(
                    $"⚠ {s.OrphanSpacePercentage:F1}% de Spaces são órfãos");
            }

            if (s.TotalAutoCreatedFailed > 0)
            {
                log.Warnings.Add(
                    $"⚠ {s.TotalAutoCreatedFailed} Spaces falharam na criação");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  TIMELINE
        // ══════════════════════════════════════════════════════════

        private void AddTimeline(
            RoomSpaceAuditLog log, string stage, string message,
            int? count = null)
        {
            lock (_lock)
            {
                log.Timeline.Add(new TimelineEvent
                {
                    Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                    ElapsedMs = _sw.ElapsedMilliseconds,
                    Stage = stage,
                    Message = message,
                    Count = count
                });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  EXPORTAÇÃO
        // ══════════════════════════════════════════════════════════

        public List<string> Exportar(
            RoomSpaceAuditLog log,
            AuditLogConfig? config = null)
        {
            config ??= new AuditLogConfig();
            var arquivosGerados = new List<string>();

            var dir = !string.IsNullOrWhiteSpace(config.OutputDirectory)
                ? config.OutputDirectory
                : Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Audit");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // ── JSON ─────────────────────────────────────────────
            if (config.ExportJson)
            {
                var path = Path.Combine(dir,
                    $"{config.FilePrefix}_{ts}.json");
                try
                {
                    var json = JsonSerializer.Serialize(log, JsonOpts);
                    File.WriteAllText(path, json, Encoding.UTF8);
                    arquivosGerados.Add(path);
                    EmitProgress($"JSON: {path}");
                }
                catch (Exception ex)
                {
                    log.Errors.Add($"Erro ao exportar JSON: {ex.Message}");
                }
            }

            // ── CSV ──────────────────────────────────────────────
            if (config.ExportCsv)
            {
                try
                {
                    var csvFiles = ExportarCsvs(log, dir,
                        config.FilePrefix, ts, config);
                    arquivosGerados.AddRange(csvFiles);
                }
                catch (Exception ex)
                {
                    log.Errors.Add($"Erro ao exportar CSV: {ex.Message}");
                }
            }

            // ── Texto legível ────────────────────────────────────
            if (config.ExportText)
            {
                var path = Path.Combine(dir,
                    $"{config.FilePrefix}_{ts}.txt");
                try
                {
                    var text = GerarTextoLegivel(log);
                    File.WriteAllText(path, text, Encoding.UTF8);
                    arquivosGerados.Add(path);
                    EmitProgress($"TXT: {path}");
                }
                catch (Exception ex)
                {
                    log.Errors.Add($"Erro ao exportar TXT: {ex.Message}");
                }
            }

            EmitProgress($"Exportação: {arquivosGerados.Count} arquivos gerados.");
            return arquivosGerados;
        }

        // ══════════════════════════════════════════════════════════
        //  EXPORTAR CSVs
        // ══════════════════════════════════════════════════════════

        private List<string> ExportarCsvs(
            RoomSpaceAuditLog log, string dir,
            string prefix, string ts, AuditLogConfig config)
        {
            var files = new List<string>();
            var ci = CultureInfo.InvariantCulture;

            // ── Rooms ────────────────────────────────────────────
            if (log.InputRooms.Count > 0)
            {
                var path = Path.Combine(dir,
                    $"{prefix}_rooms_{ts}.csv");
                var sb = new StringBuilder();

                sb.AppendLine(config.IncludeCentroidInCsv
                    ? "ElementId;Name;Number;Level;AreaM2;CentroidX;CentroidY;CentroidZ;MatchedSpaceId;MatchDistance;Status"
                    : "ElementId;Name;Number;Level;AreaM2;MatchedSpaceId;MatchDistance;Status");

                foreach (var r in log.InputRooms)
                {
                    if (config.IncludeCentroidInCsv)
                    {
                        var cx = r.Centroid.Length >= 3
                            ? r.Centroid[0].ToString("F4", ci) : "";
                        var cy = r.Centroid.Length >= 3
                            ? r.Centroid[1].ToString("F4", ci) : "";
                        var cz = r.Centroid.Length >= 3
                            ? r.Centroid[2].ToString("F4", ci) : "";

                        sb.AppendLine(
                            $"{r.ElementId};{Esc(r.Name)};{Esc(r.Number)};" +
                            $"{Esc(r.LevelName)};{r.AreaM2.ToString("F4", ci)};" +
                            $"{cx};{cy};{cz};" +
                            $"{r.MatchedSpaceId?.ToString() ?? ""};" +
                            $"{r.MatchDistance?.ToString("F4", ci) ?? ""};" +
                            $"{r.Status}");
                    }
                    else
                    {
                        sb.AppendLine(
                            $"{r.ElementId};{Esc(r.Name)};{Esc(r.Number)};" +
                            $"{Esc(r.LevelName)};{r.AreaM2.ToString("F4", ci)};" +
                            $"{r.MatchedSpaceId?.ToString() ?? ""};" +
                            $"{r.MatchDistance?.ToString("F4", ci) ?? ""};" +
                            $"{r.Status}");
                    }
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                files.Add(path);
                EmitProgress($"CSV Rooms: {path}");
            }

            // ── Correspondências ─────────────────────────────────
            if (log.MatchedPairs.Count > 0)
            {
                var path = Path.Combine(dir,
                    $"{prefix}_matched_{ts}.csv");
                var sb = new StringBuilder();

                sb.AppendLine(
                    "RoomId;RoomName;SpaceId;SpaceName;Level;DistanceM");

                foreach (var p in log.MatchedPairs)
                {
                    sb.AppendLine(
                        $"{p.RoomId};{Esc(p.RoomName)};{p.SpaceId};" +
                        $"{Esc(p.SpaceName)};{Esc(p.LevelName)};" +
                        $"{p.Distance.ToString("F4", ci)}");
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                files.Add(path);
                EmitProgress($"CSV Matched: {path}");
            }

            // ── Rooms Órfãos ─────────────────────────────────────
            if (log.OrphanRooms.Count > 0)
            {
                var path = Path.Combine(dir,
                    $"{prefix}_orphan_rooms_{ts}.csv");
                var sb = new StringBuilder();

                sb.AppendLine(
                    "RoomId;Name;Number;Level;AreaM2;Priority;Reason;" +
                    "NearestSpaceId;NearestDistanceM");

                foreach (var o in log.OrphanRooms)
                {
                    sb.AppendLine(
                        $"{o.RoomId};{Esc(o.Name)};{Esc(o.Number)};" +
                        $"{Esc(o.LevelName)};{o.AreaM2.ToString("F4", ci)};" +
                        $"{o.Priority};{Esc(o.Reason)};" +
                        $"{o.NearestSpaceId?.ToString() ?? ""};" +
                        $"{o.NearestSpaceDistanceM?.ToString("F4", ci) ?? ""}");
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                files.Add(path);
                EmitProgress($"CSV Orphan Rooms: {path}");
            }

            // ── Spaces Órfãos ────────────────────────────────────
            if (log.OrphanSpaces.Count > 0)
            {
                var path = Path.Combine(dir,
                    $"{prefix}_orphan_spaces_{ts}.csv");
                var sb = new StringBuilder();

                sb.AppendLine(
                    "SpaceId;Name;Number;Level;AreaM2;Recommendation;" +
                    "Reason;NearestRoomId;NearestDistanceM");

                foreach (var o in log.OrphanSpaces)
                {
                    sb.AppendLine(
                        $"{o.SpaceId};{Esc(o.Name)};{Esc(o.Number)};" +
                        $"{Esc(o.LevelName)};{o.AreaM2.ToString("F4", ci)};" +
                        $"{o.Recommendation};{Esc(o.Reason)};" +
                        $"{o.NearestRoomId?.ToString() ?? ""};" +
                        $"{o.NearestRoomDistanceM?.ToString("F4", ci) ?? ""}");
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                files.Add(path);
                EmitProgress($"CSV Orphan Spaces: {path}");
            }

            return files;
        }

        // ══════════════════════════════════════════════════════════
        //  TEXTO LEGÍVEL
        // ══════════════════════════════════════════════════════════

        private static string GerarTextoLegivel(RoomSpaceAuditLog log)
        {
            var sb = new StringBuilder();
            var s = log.Summary;
            var sep = new string('═', 64);

            sb.AppendLine(sep);
            sb.AppendLine("  RELATÓRIO DE AUDITORIA — Room ↔ Space");
            sb.AppendLine($"  {log.PluginName} v{log.Version}");
            sb.AppendLine($"  Projeto: {log.ProjectName}");
            sb.AppendLine($"  Data: {log.TimestampLocal}");
            sb.AppendLine(sep);
            sb.AppendLine();

            // ── RESUMO ───────────────────────────────────────────
            sb.AppendLine("  RESUMO GERAL");
            sb.AppendLine(new string('─', 40));
            sb.AppendLine($"  Rooms no modelo:      {s.TotalRooms,6}");
            sb.AppendLine($"  Spaces no modelo:     {s.TotalSpaces,6}");
            sb.AppendLine($"  Correspondências:     {s.TotalMatched,6}  ({s.MatchPercentage:F1}%)");
            sb.AppendLine($"  Rooms órfãos:         {s.TotalOrphanRooms,6}  ({s.OrphanRoomPercentage:F1}%)");
            sb.AppendLine($"  Spaces órfãos:        {s.TotalOrphanSpaces,6}  ({s.OrphanSpacePercentage:F1}%)");
            sb.AppendLine($"  Spaces criados:       {s.TotalAutoCreated,6}");
            sb.AppendLine($"  Falhas na criação:    {s.TotalAutoCreatedFailed,6}");
            sb.AppendLine($"  Distância média:      {s.AvgMatchDistanceM,8:F4} m");
            sb.AppendLine($"  Distância máxima:     {s.MaxMatchDistanceM,8:F4} m");
            sb.AppendLine();

            // ── POR LEVEL ────────────────────────────────────────
            if (s.LevelBreakdown.Count > 0)
            {
                sb.AppendLine("  DETALHAMENTO POR NÍVEL");
                sb.AppendLine(new string('─', 40));
                sb.AppendLine(
                    "  Level                  Rooms  Spaces  Match  ÓrfR  ÓrfS  Criados");

                foreach (var kvp in s.LevelBreakdown
                             .OrderBy(k => k.Key))
                {
                    var l = kvp.Value;
                    sb.AppendLine(
                        $"  {kvp.Key,-22} {l.Rooms,5}  {l.Spaces,6}" +
                        $"  {l.Matched,5}  {l.OrphanRooms,4}" +
                        $"  {l.OrphanSpaces,4}  {l.Created,7}");
                }
                sb.AppendLine();
            }

            // ── CORRESPONDÊNCIAS ─────────────────────────────────
            if (log.MatchedPairs.Count > 0)
            {
                sb.AppendLine($"  CORRESPONDÊNCIAS ({log.MatchedPairs.Count})");
                sb.AppendLine(new string('─', 40));

                foreach (var p in log.MatchedPairs.Take(50))
                {
                    sb.AppendLine(
                        $"  Room {p.RoomId} '{p.RoomName}' ↔ " +
                        $"Space {p.SpaceId} '{p.SpaceName}' " +
                        $"[{p.LevelName}] dist={p.Distance:F3}m");
                }

                if (log.MatchedPairs.Count > 50)
                    sb.AppendLine(
                        $"  ... e mais {log.MatchedPairs.Count - 50} pares");

                sb.AppendLine();
            }

            // ── ROOMS ÓRFÃOS ─────────────────────────────────────
            if (log.OrphanRooms.Count > 0)
            {
                sb.AppendLine($"  ROOMS ÓRFÃOS ({log.OrphanRooms.Count})");
                sb.AppendLine(new string('─', 40));

                foreach (var o in log.OrphanRooms)
                {
                    sb.AppendLine(
                        $"  [{o.Priority}] Room {o.RoomId} '{o.Name}' " +
                        $"[{o.LevelName}] {o.AreaM2:F2}m² — {o.Reason}");
                }
                sb.AppendLine();
            }

            // ── SPACES ÓRFÃOS ────────────────────────────────────
            if (log.OrphanSpaces.Count > 0)
            {
                sb.AppendLine($"  SPACES ÓRFÃOS ({log.OrphanSpaces.Count})");
                sb.AppendLine(new string('─', 40));

                foreach (var o in log.OrphanSpaces)
                {
                    sb.AppendLine(
                        $"  [{o.Recommendation}] Space {o.SpaceId} '{o.Name}' " +
                        $"[{o.LevelName}] {o.AreaM2:F2}m² — {o.Reason}");
                }
                sb.AppendLine();
            }

            // ── CRIAÇÕES ─────────────────────────────────────────
            if (log.AutoCreatedSpaces.Count > 0)
            {
                sb.AppendLine($"  SPACES CRIADOS ({log.AutoCreatedSpaces.Count})");
                sb.AppendLine(new string('─', 40));

                foreach (var c in log.AutoCreatedSpaces)
                {
                    var icon = c.Status == "CRIADO" ? "✓" : "✗";
                    sb.AppendLine(
                        $"  {icon} Room {c.RoomId} '{c.RoomName}' → " +
                        $"Space {c.SpaceId} [{c.LevelName}] " +
                        $"{c.AreaM2:F2}m² {c.Status}" +
                        (c.ErrorMessage != null
                            ? $" ({c.ErrorMessage})" : ""));
                }
                sb.AppendLine();
            }

            // ── WARNINGS ─────────────────────────────────────────
            if (log.Warnings.Count > 0)
            {
                sb.AppendLine("  AVISOS");
                sb.AppendLine(new string('─', 40));
                foreach (var w in log.Warnings)
                    sb.AppendLine($"  {w}");
                sb.AppendLine();
            }

            // ── TIMELINE ─────────────────────────────────────────
            sb.AppendLine("  TIMELINE");
            sb.AppendLine(new string('─', 40));
            foreach (var t in log.Timeline)
            {
                sb.AppendLine(
                    $"  [{t.Timestamp}] +{t.ElapsedMs}ms " +
                    $"{t.Stage}: {t.Message}" +
                    (t.Count.HasValue ? $" ({t.Count})" : ""));
            }

            sb.AppendLine();
            sb.AppendLine(sep);
            sb.AppendLine($"  Tempo total: {log.ExecutionTimeMs}ms");
            sb.AppendLine(sep);

            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Escapa ponto-e-vírgula em campos CSV.
        /// </summary>
        private static string Esc(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains(';')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        private void EmitProgress(string message)
        {
            OnProgress?.Invoke(message);
        }
    }
}
