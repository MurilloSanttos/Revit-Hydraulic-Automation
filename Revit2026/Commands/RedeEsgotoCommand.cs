using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E07 — Gerar rede de esgoto sanitário.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class RedeEsgotoCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E07 - Rede Esgoto",
                "Geração automática da rede de esgoto.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Criará ramais de descarga com declividade NBR 8160.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
