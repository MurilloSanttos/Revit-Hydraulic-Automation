using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit2026.Commands;

/// <summary>
/// E03 — Inserir equipamentos hidráulicos nos ambientes classificados.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class InserirEquipamentosCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message,
        ElementSet elements)
    {
        try
        {
            TaskDialog.Show("E03 - Inserir Equipamentos",
                "Inserção automática de equipamentos hidráulicos.\n\n" +
                "Status: Em desenvolvimento.\n" +
                "Será integrado com IEquipamentoService e Strategy Pattern.");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
