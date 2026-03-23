using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Revit2026.Events;

/// <summary>
/// Handler central de eventos do Revit.
/// Gerencia IdlingEvent para executar operações na thread principal.
/// </summary>
public class RevitEventHandler : IExternalEventHandler
{
    private Action<UIApplication>? _action;

    /// <summary>
    /// Agenda uma ação para execução na thread do Revit.
    /// </summary>
    public void SetAction(Action<UIApplication> action)
    {
        _action = action;
    }

    public void Execute(UIApplication app)
    {
        try
        {
            _action?.Invoke(app);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Erro - Event Handler",
                $"Erro na execução do evento:\n{ex.Message}");
        }
        finally
        {
            _action = null;
        }
    }

    public string GetName() => "HydraulicAutomationEventHandler";
}
