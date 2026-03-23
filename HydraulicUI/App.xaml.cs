using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using HidraulicoPlugin.UI.ViewModels;

namespace HidraulicoPlugin.UI;

/// <summary>
/// Ponto de entrada da aplicação WPF.
/// Configura Dependency Injection e registra serviços.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<PipelineViewModel>();

        Services = services.BuildServiceProvider();
    }
}
