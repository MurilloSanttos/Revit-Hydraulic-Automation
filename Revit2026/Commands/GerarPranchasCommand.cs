using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E11 — Gerar pranchas finais do projeto hidráulico.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class GerarPranchasCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E11 - Gerar Pranchas",
                "Geração de pranchas finais.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Criará pranchas com vistas, tabelas e carimbo.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
