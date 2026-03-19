using PluginCore.Logging;

namespace PluginCore.Interfaces
{
    /// <summary>
    /// Contrato para o serviço de logging.
    /// </summary>
    public interface ILogService
    {
        IReadOnlyList<LogEntry> Entries { get; }
        bool TemBloqueio { get; }

        void Info(string etapa, string componente, string mensagem, long? elementId = null);
        void Leve(string etapa, string componente, string mensagem, long? elementId = null);
        void Medio(string etapa, string componente, string mensagem, long? elementId = null);
        void Critico(string etapa, string componente, string mensagem, long? elementId = null,
            string? detalhes = null);

        string ExportarParaJson(string? nomeArquivo = null);
        string GerarResumo();
        void Limpar();
    }
}
