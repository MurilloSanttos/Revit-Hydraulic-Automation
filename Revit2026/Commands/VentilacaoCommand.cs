using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E08 — Gerar rede de ventilação.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class VentilacaoCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E08 - Ventilação",
                "Geração automática da rede de ventilação.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Criará tubos de ventilação conforme NBR 8160.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
