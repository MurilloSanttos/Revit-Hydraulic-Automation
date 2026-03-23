using Newtonsoft.Json;

namespace PluginCore.Logging
{
    /// <summary>
    /// Exportador estático de logs para arquivo JSON.
    /// Utiliza LogManager.Instance para obter os dados.
    /// </summary>
    public static class LogExporter
    {
        /// <summary>
        /// Exporta todos os logs acumulados para um arquivo JSON.
        /// </summary>
        /// <param name="filePath">Caminho completo do arquivo de saída.</param>
        public static void ExportToJsonFile(string filePath)
        {
            try
            {
                var logs = LogManager.Instance.GetLogs();

                var json = JsonConvert.SerializeObject(logs, Formatting.Indented);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Falha ao exportar logs para '{filePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporta logs filtrados por nível para um arquivo JSON.
        /// </summary>
        public static void ExportByLevel(string filePath, LogLevel level)
        {
            try
            {
                var logs = LogManager.Instance.GetByLevel(level);

                var json = JsonConvert.SerializeObject(logs, Formatting.Indented);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Falha ao exportar logs nível {level} para '{filePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporta logs filtrados por etapa para um arquivo JSON.
        /// </summary>
        public static void ExportByEtapa(string filePath, string etapa)
        {
            try
            {
                var logs = LogManager.Instance.FiltrarPorEtapa(etapa);

                var json = JsonConvert.SerializeObject(logs, Formatting.Indented);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Falha ao exportar logs da etapa '{etapa}' para '{filePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gera caminho padrão com timestamp para o arquivo de log.
        /// </summary>
        public static string GerarCaminhoPadrao(string diretorio, string? prefixo = null)
        {
            prefixo ??= "log";
            var nome = $"{prefixo}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            return Path.Combine(diretorio, nome);
        }

        // Exemplo de uso:
        // LogExporter.ExportToJsonFile(@"C:\logs\log.json");
        // LogExporter.ExportByLevel(@"C:\logs\criticos.json", LogLevel.Critico);
        // LogExporter.ExportByEtapa(@"C:\logs\etapa01.json", "E01");
        // var path = LogExporter.GerarCaminhoPadrao(@"C:\logs");
    }
}
