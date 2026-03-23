using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HidraulicoPlugin.UI.ViewModels;

/// <summary>
/// ViewModel principal da aplicação.
/// Controla navegação e estado geral.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty]
    private PipelineViewModel _pipelineVM;

    [ObservableProperty]
    private string _statusBarText = "Pronto";

    [ObservableProperty]
    private string _versionText = "v0.1.0";

    public MainViewModel()
    {
        Title = "Hydraulic Automation";
        _pipelineVM = new PipelineViewModel();
    }
}
