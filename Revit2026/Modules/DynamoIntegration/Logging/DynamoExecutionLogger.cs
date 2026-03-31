using System.Text.Json;
using System.Text.Json.Serialization;

namespace Revit2026.Modules.DynamoIntegration.Logging
{
    public class DynamoExecutionLog
    {
        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("durationMs")]
        public long DurationMs { get; set; }

        [JsonPropertyName("scriptPath")]
        public string ScriptPath { get; set; } = "";

        [JsonPropertyName("inputJson")]
        public string? InputJson { get; set; }

        [JsonPropertyName("outputJson")]
        public string? OutputJson { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("timeoutOccurred")]
        public bool TimeoutOccurred { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        public void Recalculate()
        {
            DurationMs = (long)(EndTime - StartTime).TotalMilliseconds;
        }

        public override string ToString() =>
            $"[{(Success ? "OK" : "FAIL")}] " +
            $"{Path.GetFileNameWithoutExtension(ScriptPath)} " +
            $"({DurationMs}ms)" +
            (TimeoutOccurred ? " [TIMEOUT]" : "");
    }

    public class DynamoLogQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool? OnlySuccess { get; set; }
        public bool? OnlyFailed { get; set; }
        public bool? OnlyTimeouts { get; set; }
        public string? ScriptNameContains { get; set; }
        public int? MaxResults { get; set; }
    }

    public class DynamoLogSummary
    {
        [JsonPropertyName("totalExecutions")]
        public int TotalExecutions { get; set; }

        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        [JsonPropertyName("timeoutCount")]
        public int TimeoutCount { get; set; }

        [JsonPropertyName("averageDurationMs")]
        public double AverageDurationMs { get; set; }

        [JsonPropertyName("maxDurationMs")]
        public long MaxDurationMs { get; set; }

        [JsonPropertyName("minDurationMs")]
        public long MinDurationMs { get; set; }

        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public override string ToString() =>
            $"{TotalExecutions} runs: {SuccessCount} ok, " +
            $"{FailedCount} failed, {TimeoutCount} timeouts " +
            $"(avg {AverageDurationMs:F0}ms)";
    }

    public interface IDynamoExecutionLogger
    {
        void WriteExecutionLog(DynamoExecutionLog log);
        List<DynamoExecutionLog> ReadLogs(DynamoLogQuery? query = null);
        DynamoLogSummary GetSummary(DynamoLogQuery? query = null);
    }

    public class DynamoExecutionLogger : IDynamoExecutionLogger
    {
        private readonly string _logDirectory;
        private readonly object _writeLock = new();
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DynamoExecutionLogger()
            : this(DefaultLogDirectory())
        {
        }

        public DynamoExecutionLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            EnsureDirectory(_logDirectory);
        }

        public void WriteExecutionLog(DynamoExecutionLog log)
        {
            if (log == null) return;

            try
            {
                log.Recalculate();

                var fileName = $"dynamo_log_{log.StartTime:yyyyMMdd_HHmmss_fff}.json";
                var filePath = Path.Combine(_logDirectory, fileName);
                var json = JsonSerializer.Serialize(log, WriteOptions);

                lock (_writeLock)
                {
                    EnsureDirectory(_logDirectory);
                    File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                }
            }
            catch
            {
                // falha no log não deve quebrar o fluxo principal
            }
        }

        public static DynamoExecutionLog Start(string scriptPath, string? inputJson = null)
        {
            return new DynamoExecutionLog
            {
                StartTime = DateTime.UtcNow,
                ScriptPath = scriptPath,
                InputJson = inputJson
            };
        }

        public static void MarkSuccess(
            DynamoExecutionLog log,
            string? outputJson = null)
        {
            log.EndTime = DateTime.UtcNow;
            log.Success = true;
            log.OutputJson = outputJson;
            log.Recalculate();
        }

        public static void MarkFailed(
            DynamoExecutionLog log,
            string errorMessage)
        {
            log.EndTime = DateTime.UtcNow;
            log.Success = false;
            log.ErrorMessage = errorMessage;
            log.Recalculate();
        }

        public static void MarkTimeout(
            DynamoExecutionLog log,
            int timeoutMs)
        {
            log.EndTime = DateTime.UtcNow;
            log.Success = false;
            log.TimeoutOccurred = true;
            log.ErrorMessage = $"Execution timed out after {timeoutMs} ms.";
            log.Recalculate();
        }

        public static void MarkException(
            DynamoExecutionLog log,
            Exception ex)
        {
            log.EndTime = DateTime.UtcNow;
            log.Success = false;
            log.ErrorMessage = FlattenException(ex);
            log.Recalculate();
        }

        public List<DynamoExecutionLog> ReadLogs(DynamoLogQuery? query = null)
        {
            var logs = new List<DynamoExecutionLog>();

            try
            {
                if (!Directory.Exists(_logDirectory))
                    return logs;

                var files = Directory.GetFiles(_logDirectory, "dynamo_log_*.json")
                    .OrderByDescending(f => f);

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file, System.Text.Encoding.UTF8);
                        var log = JsonSerializer.Deserialize<DynamoExecutionLog>(
                            json, ReadOptions);

                        if (log == null) continue;
                        if (!MatchesQuery(log, query)) continue;

                        logs.Add(log);

                        if (query?.MaxResults > 0 && logs.Count >= query.MaxResults)
                            break;
                    }
                    catch
                    {
                        // arquivo corrompido
                    }
                }
            }
            catch
            {
                // diretório inacessível
            }

            return logs;
        }

        public DynamoExecutionLog? ReadLast()
        {
            var logs = ReadLogs(new DynamoLogQuery { MaxResults = 1 });
            return logs.Count > 0 ? logs[0] : null;
        }

        public DynamoLogSummary GetSummary(DynamoLogQuery? query = null)
        {
            var logs = ReadLogs(query);
            return BuildSummary(logs);
        }

        public int PurgeBefore(DateTime cutoff)
        {
            int deleted = 0;

            try
            {
                var files = Directory.GetFiles(_logDirectory, "dynamo_log_*.json");

                foreach (var file in files)
                {
                    try
                    {
                        if (File.GetCreationTimeUtc(file) < cutoff)
                        {
                            File.Delete(file);
                            deleted++;
                        }
                    }
                    catch
                    {
                        // permissão negada
                    }
                }
            }
            catch
            {
                // diretório inacessível
            }

            return deleted;
        }

        public int PurgeKeepLast(int keepCount)
        {
            int deleted = 0;

            try
            {
                var files = Directory.GetFiles(_logDirectory, "dynamo_log_*.json")
                    .OrderByDescending(f => f)
                    .Skip(keepCount);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        deleted++;
                    }
                    catch
                    {
                        // permissão negada
                    }
                }
            }
            catch
            {
                // diretório inacessível
            }

            return deleted;
        }

        private static bool MatchesQuery(DynamoExecutionLog log, DynamoLogQuery? query)
        {
            if (query == null) return true;

            if (query.From.HasValue && log.StartTime < query.From.Value)
                return false;

            if (query.To.HasValue && log.StartTime > query.To.Value)
                return false;

            if (query.OnlySuccess == true && !log.Success)
                return false;

            if (query.OnlyFailed == true && log.Success)
                return false;

            if (query.OnlyTimeouts == true && !log.TimeoutOccurred)
                return false;

            if (!string.IsNullOrEmpty(query.ScriptNameContains) &&
                log.ScriptPath.IndexOf(query.ScriptNameContains,
                    StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return true;
        }

        private static DynamoLogSummary BuildSummary(List<DynamoExecutionLog> logs)
        {
            var summary = new DynamoLogSummary { TotalExecutions = logs.Count };

            if (logs.Count == 0)
                return summary;

            summary.SuccessCount = logs.Count(l => l.Success);
            summary.FailedCount = logs.Count(l => !l.Success);
            summary.TimeoutCount = logs.Count(l => l.TimeoutOccurred);
            summary.AverageDurationMs = logs.Average(l => l.DurationMs);
            summary.MaxDurationMs = logs.Max(l => l.DurationMs);
            summary.MinDurationMs = logs.Min(l => l.DurationMs);

            return summary;
        }

        private static string FlattenException(Exception ex)
        {
            if (ex is AggregateException agg && agg.InnerExceptions.Count == 1)
                return FlattenException(agg.InnerExceptions[0]);

            if (ex.InnerException != null)
                return $"{ex.GetType().Name}: {ex.Message} → " +
                       FlattenException(ex.InnerException);

            return $"{ex.GetType().Name}: {ex.Message}";
        }

        private static string DefaultLogDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HermesMEP", "Logs", "Dynamo");
        }

        private static void EnsureDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch
            {
                // silencioso
            }
        }
    }
}
