using CommunityToolkit.Mvvm.ComponentModel;

namespace HidraulicoPlugin.UI.ViewModels;

/// <summary>
/// ViewModel base com propriedades comuns a todos os ViewModels.
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    protected void ClearError() => ErrorMessage = string.Empty;

    protected void SetError(string message) => ErrorMessage = message;
}
