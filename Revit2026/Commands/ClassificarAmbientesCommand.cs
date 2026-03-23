using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E02 — Classificar ambientes detectados por tipo funcional.
/// </summary>
[Transaction(TransactionMode.ReadOnly)]
[Regeneration(RegenerationOption.Manual)]
public class ClassificarAmbientesCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            // TODO: Integrar com ClassificadorAmbientes do PluginCore
            TaskDialog.Show("E02 - Classificar Ambientes",
                "Classificação automática de ambientes.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Será integrado com ClassificadorAmbientes do PluginCore.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
