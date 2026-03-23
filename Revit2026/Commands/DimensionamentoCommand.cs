using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E09 — Dimensionar redes hidráulicas (vazão, diâmetro, pressão).
/// </summary>
[Transaction(TransactionMode.ReadOnly)]
[Regeneration(RegenerationOption.Manual)]
public class DimensionamentoCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E09 - Dimensionamento",
                "Dimensionamento hidráulico das redes.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Calculará vazão, diâmetro, velocidade e pressão " +
                "conforme NBR 5626 e NBR 8160.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
