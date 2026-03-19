using Newtonsoft.Json;
using PluginCore.Interfaces;

namespace PluginCore.Logging
{
    /// <summary>
    /// Gerenciador centralizado de logs do sistema.
    /// Acumula entradas durante a execução e permite exportação para JSON.
    /// </summary>
    public class LogManager : ILogService
    {
        private readonly List<LogEntry> _entries = new();
        private readonly string _logDirectory;

        public LogManager(string logDirectory)
        {
            _logDirectory = logDirectory;

            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// Todas as entradas de log acumuladas.
        /// </summary>
        public IReadOnlyList<LogEntry> Entries => _entries.AsReadOnly();

        /// <summary>
        /// Indica se há algum erro crítico que bloqueia o avanço.
        /// </summary>
        public bool TemBloqueio => _entries.Any(e => e.Level == LogLevel.Critico);

        /// <summary>
        /// Quantidade de erros por nível.
        /// </summary>
        public int ContarPorNivel(LogLevel level)
            => _entries.Count(e => e.Level == level);

        /// <summary>
        /// Registra uma entrada informativa.
        /// </summary>
        public void Info(string etapa, string componente, string mensagem, long? elementId = null)
        {
            Registrar(LogLevel.Info, etapa, componente, mensagem, elementId);
        }

        /// <summary>
        /// Registra um alerta leve.
        /// </summary>
        public void Leve(string etapa, string componente, string mensagem, long? elementId = null)
        {
            Registrar(LogLevel.Leve, etapa, componente, mensagem, elementId);
        }

        /// <summary>
        /// Registra um alerta médio.
        /// </summary>
        public void Medio(string etapa, string componente, string mensagem, long? elementId = null)
        {
            Registrar(LogLevel.Medio, etapa, componente, mensagem, elementId);
        }

        /// <summary>
        /// Registra um erro crítico.
        /// </summary>
        public void Critico(string etapa, string componente, string mensagem, long? elementId = null,
            string? detalhes = null)
        {
            Registrar(LogLevel.Critico, etapa, componente, mensagem, elementId, detalhes);
        }

        /// <summary>
        /// Registra uma entrada de log.
        /// </summary>
        private void Registrar(LogLevel level, string etapa, string componente,
            string mensagem, long? elementId = null, string? detalhes = null)
        {
            var entry = new LogEntry
            {
                Level = level,
                Etapa = etapa,
                Componente = componente,
                Mensagem = mensagem,
                ElementId = elementId,
                Detalhes = detalhes
            };

            _entries.Add(entry);
        }

        /// <summary>
        /// Exporta todos os logs para um arquivo JSON.
        /// </summary>
        public string ExportarParaJson(string? nomeArquivo = null)
        {
            nomeArquivo ??= $"log_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_logDirectory, nomeArquivo);

            var json = JsonConvert.SerializeObject(_entries, Formatting.Indented);
            File.WriteAllText(filePath, json);

            return filePath;
        }

        /// <summary>
        /// Retorna um resumo textual dos logs acumulados.
        /// </summary>
        public string GerarResumo()
        {
            var total = _entries.Count;
            var criticos = ContarPorNivel(LogLevel.Critico);
            var medios = ContarPorNivel(LogLevel.Medio);
            var leves = ContarPorNivel(LogLevel.Leve);
            var infos = ContarPorNivel(LogLevel.Info);

            var status = TemBloqueio ? "❌ BLOQUEADO" : "✅ OK";

            return $"=== RESUMO DO LOG ===\n" +
                   $"Status: {status}\n" +
                   $"Total: {total} entradas\n" +
                   $"  Críticos: {criticos}\n" +
                   $"  Médios: {medios}\n" +
                   $"  Leves: {leves}\n" +
                   $"  Info: {infos}\n" +
                   $"====================";
        }

        /// <summary>
        /// Limpa todos os logs acumulados.
        /// </summary>
        public void Limpar() => _entries.Clear();

        /// <summary>
        /// Filtra logs por etapa.
        /// </summary>
        public IEnumerable<LogEntry> FiltrarPorEtapa(string etapa)
            => _entries.Where(e => e.Etapa.Equals(etapa, StringComparison.OrdinalIgnoreCase));
    }
}
