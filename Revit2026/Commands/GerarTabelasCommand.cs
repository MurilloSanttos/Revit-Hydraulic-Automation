using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E10 — Gerar tabelas hidráulicas e quantitativos.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class GerarTabelasCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E10 - Gerar Tabelas",
                "Geração de tabelas hidráulicas.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Criará tabelas de dimensionamento AF/ES e quantitativos.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
