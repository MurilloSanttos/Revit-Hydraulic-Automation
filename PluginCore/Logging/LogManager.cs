using Newtonsoft.Json;
using PluginCore.Interfaces;

namespace PluginCore.Logging
{
    /// <summary>
    /// Gerenciador centralizado de logs do sistema.
    /// Thread-safe, acumula em memória e exporta para JSON.
    /// Suporta Singleton (global) e instâncias por escopo.
    /// </summary>
    public class LogManager : ILogService
    {
        // ══════════════════════════════════════════════════════════
        //  SINGLETON
        // ══════════════════════════════════════════════════════════

        private static readonly Lazy<LogManager> _instance = new(() =>
            new LogManager(DefaultLogDirectory));

        /// <summary>Instância global (Singleton).</summary>
        public static LogManager Instance => _instance.Value;

        /// <summary>Diretório padrão para logs.</summary>
        private static string DefaultLogDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HidraulicaRevit", "Logs");

        // ══════════════════════════════════════════════════════════
        //  CAMPOS PRIVADOS
        // ══════════════════════════════════════════════════════════

        private readonly List<LogEntry> _entries = new();
        private readonly object _lock = new();
        private readonly string _logDirectory;

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria instância com diretório específico para logs.
        /// </summary>
        public LogManager(string logDirectory)
        {
            _logDirectory = logDirectory;

            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        // ══════════════════════════════════════════════════════════
        //  PROPRIEDADES
        // ══════════════════════════════════════════════════════════

        /// <summary>Todas as entradas de log (somente leitura).</summary>
        public IReadOnlyList<LogEntry> Entries
        {
            get { lock (_lock) { return _entries.ToList().AsReadOnly(); } }
        }

        /// <summary>Indica se há algum erro crítico bloqueante.</summary>
        public bool TemBloqueio
        {
            get { lock (_lock) { return _entries.Any(e => e.Level == LogLevel.Critico); } }
        }

        /// <summary>Verifica se existem erros críticos (alias EN).</summary>
        public bool HasCriticalErrors()
        {
            lock (_lock) { return _entries.Any(e => e.Level == LogLevel.Critico); }
        }

        /// <summary>Retorna todos os erros críticos.</summary>
        public IEnumerable<LogEntry> GetCriticalErrors()
        {
            lock (_lock) { return _entries.Where(e => e.Level == LogLevel.Critico).ToList(); }
        }

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS DE REGISTRO (ATALHOS)
        // ══════════════════════════════════════════════════════════

        /// <summary>Registra informação geral.</summary>
        public void Info(string etapa, string componente, string mensagem,
            long? elementId = null)
            => AddLog(LogLevel.Info, etapa, componente, mensagem, elementId);

        /// <summary>Registra alerta leve.</summary>
        public void Leve(string etapa, string componente, string mensagem,
            long? elementId = null)
            => AddLog(LogLevel.Leve, etapa, componente, mensagem, elementId);

        /// <summary>Registra alerta médio.</summary>
        public void Medio(string etapa, string componente, string mensagem,
            long? elementId = null)
            => AddLog(LogLevel.Medio, etapa, componente, mensagem, elementId);

        /// <summary>Registra erro crítico.</summary>
        public void Critico(string etapa, string componente, string mensagem,
            long? elementId = null, string? detalhes = null)
            => AddLog(LogLevel.Critico, etapa, componente, mensagem, elementId, detalhes);

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS PRINCIPAIS
        // ══════════════════════════════════════════════════════════

        /// <summary>Adiciona uma entrada ao log.</summary>
        public void AddLog(LogEntry entry)
        {
            lock (_lock) { _entries.Add(entry); }
        }

        /// <summary>Adiciona uma entrada ao log por parâmetros.</summary>
        public void AddLog(LogLevel level, string etapa, string componente,
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

            lock (_lock) { _entries.Add(entry); }
        }

        /// <summary>Retorna todos os logs (cópia thread-safe).</summary>
        public IReadOnlyList<LogEntry> GetLogs()
        {
            lock (_lock) { return _entries.ToList().AsReadOnly(); }
        }

        /// <summary>Filtra logs por nível.</summary>
        public IEnumerable<LogEntry> GetByLevel(LogLevel level)
        {
            lock (_lock) { return _entries.Where(e => e.Level == level).ToList(); }
        }

        /// <summary>Filtra logs por etapa.</summary>
        public IEnumerable<LogEntry> FiltrarPorEtapa(string etapa)
        {
            lock (_lock)
            {
                return _entries
                    .Where(e => e.Etapa.Equals(etapa, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>Filtra logs por etapa (alias em inglês).</summary>
        public IEnumerable<LogEntry> GetByEtapa(string etapa)
            => FiltrarPorEtapa(etapa);

        /// <summary>Filtra logs por nível E etapa combinados.</summary>
        public IEnumerable<LogEntry> GetByLevelAndEtapa(LogLevel level, string etapa)
        {
            lock (_lock)
            {
                return _entries
                    .Where(e => e.Level == level &&
                                e.Etapa.Equals(etapa, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>Quantidade de entradas por nível.</summary>
        public int ContarPorNivel(LogLevel level)
        {
            lock (_lock) { return _entries.Count(e => e.Level == level); }
        }

        /// <summary>Limpa todos os logs.</summary>
        public void Clear()
        {
            lock (_lock) { _entries.Clear(); }
        }

        // ══════════════════════════════════════════════════════════
        //  EXPORTAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>Exporta logs para arquivo JSON.</summary>
        public string ExportarParaJson(string? nomeArquivo = null)
        {
            List<LogEntry> snapshot;
            lock (_lock) { snapshot = _entries.ToList(); }

            nomeArquivo ??= $"log_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_logDirectory, nomeArquivo);

            var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            File.WriteAllText(filePath, json);

            return filePath;
        }

        /// <summary>Gera resumo textual dos logs.</summary>
        public string GerarResumo()
        {
            lock (_lock)
            {
                var total = _entries.Count;
                var criticos = _entries.Count(e => e.Level == LogLevel.Critico);
                var medios = _entries.Count(e => e.Level == LogLevel.Medio);
                var leves = _entries.Count(e => e.Level == LogLevel.Leve);
                var infos = _entries.Count(e => e.Level == LogLevel.Info);
                var status = criticos > 0 ? "❌ BLOQUEADO" : "✅ OK";

                return $"=== RESUMO DO LOG ===\n" +
                       $"Status: {status}\n" +
                       $"Total: {total} entradas\n" +
                       $"  Críticos: {criticos}\n" +
                       $"  Médios: {medios}\n" +
                       $"  Leves: {leves}\n" +
                       $"  Info: {infos}\n" +
                       $"====================";
            }
        }

        // Exemplo de uso:
        // LogManager.Instance.Info("E01", "RoomReader", "5 rooms detectados.");
        // LogManager.Instance.Critico("E07", "RedeService", "Falha na conexão", 12345);
        // var logs = LogManager.Instance.GetLogs();
        // var criticos = LogManager.Instance.GetByLevel(LogLevel.Critico);
    }
}
