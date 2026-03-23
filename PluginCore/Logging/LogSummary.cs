using System.Text;

namespace PluginCore.Logging
{
    /// <summary>
    /// Gera resumos consolidados dos logs acumulados.
    /// </summary>
    public static class LogSummary
    {
        /// <summary>
        /// Gera resumo geral com contagem por nível.
        /// </summary>
        public static string GenerateSummary()
        {
            var logs = LogManager.Instance.GetLogs();

            var criticos = logs.Count(e => e.Level == LogLevel.Critico);
            var medios   = logs.Count(e => e.Level == LogLevel.Medio);
            var leves    = logs.Count(e => e.Level == LogLevel.Leve);
            var infos    = logs.Count(e => e.Level == LogLevel.Info);
            var total    = logs.Count;

            var status = criticos > 0 ? "❌ BLOQUEADO" : "✅ OK";

            var sb = new StringBuilder();
            sb.AppendLine("══════════════════════════");
            sb.AppendLine("  Resumo de Logs");
            sb.AppendLine("══════════════════════════");
            sb.AppendLine($"  Status:   {status}");
            sb.AppendLine($"  Critico:  {criticos}");
            sb.AppendLine($"  Medio:    {medios}");
            sb.AppendLine($"  Leve:     {leves}");
            sb.AppendLine($"  Info:     {infos}");
            sb.AppendLine($"  Total:    {total}");
            sb.AppendLine("══════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Gera resumo por etapa do pipeline.
        /// </summary>
        public static string GenerateSummaryByEtapa()
        {
            var logs = LogManager.Instance.GetLogs();

            var grupos = logs
                .GroupBy(e => e.Etapa)
                .OrderBy(g => g.Key);

            var sb = new StringBuilder();
            sb.AppendLine("══════════════════════════");
            sb.AppendLine("  Resumo por Etapa");
            sb.AppendLine("══════════════════════════");

            foreach (var grupo in grupos)
            {
                var c = grupo.Count(e => e.Level == LogLevel.Critico);
                var m = grupo.Count(e => e.Level == LogLevel.Medio);
                var l = grupo.Count(e => e.Level == LogLevel.Leve);
                var i = grupo.Count(e => e.Level == LogLevel.Info);
                var icon = c > 0 ? "❌" : m > 0 ? "⚠️" : "✅";

                sb.AppendLine($"  {icon} {grupo.Key,-20} C:{c} M:{m} L:{l} I:{i}");
            }

            sb.AppendLine("══════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Retorna contadores em formato de dicionário.
        /// </summary>
        public static Dictionary<LogLevel, int> GetCounts()
        {
            var logs = LogManager.Instance.GetLogs();

            return new Dictionary<LogLevel, int>
            {
                [LogLevel.Critico] = logs.Count(e => e.Level == LogLevel.Critico),
                [LogLevel.Medio]   = logs.Count(e => e.Level == LogLevel.Medio),
                [LogLevel.Leve]    = logs.Count(e => e.Level == LogLevel.Leve),
                [LogLevel.Info]    = logs.Count(e => e.Level == LogLevel.Info)
            };
        }

        // Exemplo de uso:
        // string resumo = LogSummary.GenerateSummary();
        // string porEtapa = LogSummary.GenerateSummaryByEtapa();
        // var counts = LogSummary.GetCounts();
    }
}
