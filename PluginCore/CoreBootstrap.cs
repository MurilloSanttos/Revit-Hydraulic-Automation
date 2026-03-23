namespace PluginCore;

/// <summary>
/// Ponto de entrada de inicialização do PluginCore.
/// Responsável por registrar serviços, configurar logging
/// e preparar o pipeline.
/// </summary>
public static class CoreBootstrap
{
    private static bool _initialized;

    /// <summary>
    /// Inicializa o Core com configurações padrão.
    /// Deve ser chamado uma vez na startup do plugin.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // Futuro: registrar serviços no container DI
        // Futuro: carregar configurações de normas
        // Futuro: inicializar EventBus e Observers

        _initialized = true;
    }

    /// <summary>
    /// Verifica se o Core foi inicializado.
    /// </summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// Reseta o estado (para testes).
    /// </summary>
    public static void Reset() => _initialized = false;
}
