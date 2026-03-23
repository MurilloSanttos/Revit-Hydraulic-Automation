using Autodesk.Revit.DB;
using PluginCore.Models;
using PluginCore.Domain.Enums;

namespace Revit2026.Services;

/// <summary>
/// Mapeia elementos do Revit para modelos do PluginCore.
/// Ponte entre a API do Revit e o domínio agnóstico.
/// </summary>
public class RevitElementMapper
{
    private readonly Document _doc;

    public RevitElementMapper(Document doc)
    {
        _doc = doc;
    }

    /// <summary>
    /// Converte um FamilyInstance (Plumbing Fixture) para EquipamentoHidraulico.
    /// </summary>
    public EquipamentoHidraulico? MapearEquipamento(FamilyInstance instance)
    {
        if (instance == null) return null;

        var familyName = instance.Symbol.Family.Name;
        var typeName = instance.Symbol.Name;
        var location = instance.Location as LocationPoint;

        var equipamento = new EquipamentoHidraulico
        {
            RevitElementId = instance.Id.Value,
            FamilyName = familyName,
            TypeName = typeName,
            Mark = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
            Tipo = ClassificarEquipamento(familyName),
        };

        if (location != null)
        {
            equipamento.PosX = location.Point.X;
            equipamento.PosY = location.Point.Y;
            equipamento.PosZ = location.Point.Z;
        }

        // Room
        var room = instance.Room;
        if (room != null)
        {
            equipamento.AmbienteId = room.Id.Value.ToString();
            equipamento.AmbienteNome = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
        }

        // Level
        var level = _doc.GetElement(instance.LevelId) as Level;
        equipamento.Nivel = level?.Name ?? "";

        return equipamento;
    }

    /// <summary>
    /// Classifica o tipo de equipamento a partir do nome da família.
    /// </summary>
    private static EquipmentType ClassificarEquipamento(string familyName)
    {
        var name = familyName.ToLowerInvariant();

        if (name.Contains("toilet")) return EquipmentType.Toilet;
        if (name.Contains("lavatory") || name.Contains("lav")) return EquipmentType.Sink;
        if (name.Contains("shower")) return EquipmentType.Shower;
        if (name.Contains("bathtub") || name.Contains("bath")) return EquipmentType.Bathtub;
        if (name.Contains("kitchensink") || name.Contains("kitchen sink")) return EquipmentType.KitchenSink;
        if (name.Contains("laundrytub") || name.Contains("utility sink")) return EquipmentType.LaundryTub;
        if (name.Contains("washingmachine") || name.Contains("washing")) return EquipmentType.WashingMachine;
        if (name.Contains("dishwasher")) return EquipmentType.Dishwasher;
        if (name.Contains("floordrain") || name.Contains("floor drain")) return EquipmentType.FloorDrain;
        if (name.Contains("bidet")) return EquipmentType.Bidet;
        if (name.Contains("urinal")) return EquipmentType.Urinal;

        return EquipmentType.Toilet; // fallback
    }
}
