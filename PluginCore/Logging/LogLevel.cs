namespace PluginCore.Logging
{
    /// <summary>
    /// Níveis de severidade de log.
    /// Determinam o comportamento do sistema quando um problema é encontrado.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Informação geral sobre o progresso da execução.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Alerta leve — apenas informativo, não impede execução.
        /// </summary>
        Leve = 1,

        /// <summary>
        /// Alerta médio — permite continuar, mas requer atenção.
        /// </summary>
        Medio = 2,

        /// <summary>
        /// Erro crítico — bloqueia o avanço para a próxima etapa.
        /// </summary>
        Critico = 3
    }
}
