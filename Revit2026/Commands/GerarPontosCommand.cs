using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E04 — Gerar pontos hidráulicos a partir dos equipamentos.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class GerarPontosCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E04 - Gerar Pontos",
                "Geração automática de pontos hidráulicos.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Criará pontos AF/ES/VE em cada equipamento.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
