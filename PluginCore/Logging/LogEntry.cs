namespace PluginCore.Logging
{
    /// <summary>
    /// Representa uma entrada individual no log do sistema.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Timestamp da entrada.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Nível de severidade.
        /// </summary>
        public LogLevel Level { get; set; } = LogLevel.Info;

        /// <summary>
        /// Etapa do pipeline em que o log foi gerado.
        /// </summary>
        public string Etapa { get; set; } = string.Empty;

        /// <summary>
        /// Componente ou serviço que gerou o log.
        /// </summary>
        public string Componente { get; set; } = string.Empty;

        /// <summary>
        /// Mensagem descritiva.
        /// </summary>
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>
        /// ID do elemento do Revit relacionado (se aplicável).
        /// </summary>
        public long? ElementId { get; set; }

        /// <summary>
        /// Detalhes adicionais ou stack trace.
        /// </summary>
        public string? Detalhes { get; set; }

        /// <summary>
        /// Indica se este log representa um bloqueio.
        /// </summary>
        public bool EhBloqueante => Level == LogLevel.Critico;

        public override string ToString()
        {
            var prefix = Level switch
            {
                LogLevel.Critico => "❌ CRÍTICO",
                LogLevel.Medio => "⚠️ MÉDIO",
                LogLevel.Leve => "ℹ️ LEVE",
                LogLevel.Info => "✅ INFO",
                _ => "?"
            };

            return $"[{Timestamp:HH:mm:ss}] {prefix} | {Etapa}/{Componente}: {Mensagem}";
        }
    }
}
