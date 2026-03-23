using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E05 — Criar prumadas verticais conectando os pavimentos.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class CriarPrumadasCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E05 - Criar Prumadas",
                "Criação automática de prumadas verticais.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Conectará pavimentos via colunas verticais AF/ES/VE.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
