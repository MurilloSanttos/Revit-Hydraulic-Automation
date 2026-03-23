using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// Comando para iniciar o pipeline completo de automação hidráulica.
/// Abre a interface principal e inicia a execução.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class StartPipelineCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            // TODO: Abrir MainWindow da UI
            // TODO: Iniciar pipeline via IOrquestradorService

            TaskDialog.Show("Hydraulic Automation",
                "Pipeline de automação hidráulica iniciado.\n\n" +
                "Funcionalidade em desenvolvimento.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Erro - Hydraulic Automation",
                $"Falha ao iniciar pipeline:\n{ex.Message}");
            return Result.Failed;
        }
    }
}
