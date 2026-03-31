using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Revit2026.Modules.Core;

namespace Revit2026.Modules.Spaces
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO DE DADOS — SPACE MEP
    // ══════════════════════════════════════════════════════════════

    public class ValidSpace
    {
        [JsonPropertyName("elementId")]
        public long ElementId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        [JsonPropertyName("levelId")]
        public long LevelId { get; set; }

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("levelElevationM")]
        public double LevelElevationM { get; set; }

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("perimeterM")]
        public double PerimeterM { get; set; }

        [JsonPropertyName("volumeM3")]
        public double VolumeM3 { get; set; }

        [JsonPropertyName("heightM")]
        public double HeightM { get; set; }

        [JsonPropertyName("centroid")]
        public double[] Centroid { get; set; } = Array.Empty<double>();

        [JsonPropertyName("department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("phase")]
        public string Phase { get; set; } = "";

        [JsonPropertyName("spaceType")]
        public string SpaceType { get; set; } = "";

        [JsonPropertyName("conditionType")]
        public string ConditionType { get; set; } = "";

        [JsonPropertyName("isEnclosed")]
        public bool IsEnclosed { get; set; }

        [JsonPropertyName("occupancy")]
        public int Occupancy { get; set; }

        [JsonPropertyName("associatedRoomId")]
        public long AssociatedRoomId { get; set; }

        [JsonPropertyName("associatedRoomName")]
        public string AssociatedRoomName { get; set; } = "";

        [JsonPropertyName("customParameters")]
        public Dictionary<string, string> CustomParameters { get; set; } = new();

        /// Referência ao objeto Revit (não serializado)
        [JsonIgnore]
        public Space? RevitSpace { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  DESCARTADO
    // ══════════════════════════════════════════════════════════════

    public class DiscardedSpace
    {
        [JsonPropertyName("elementId")]
        public long ElementId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DA COLETA
    // ══════════════════════════════════════════════════════════════

    public class SpaceCollectionResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalFound")]
        public int TotalFound { get; set; }

        [JsonPropertyName("validCount")]
        public int ValidCount { get; set; }

        [JsonPropertyName("discardedCount")]
        public int DiscardedCount { get; set; }

        [JsonPropertyName("validSpaces")]
        public List<ValidSpace> ValidSpaces { get; set; } = new();

        [JsonPropertyName("discardedSpaces")]
        public List<DiscardedSpace> DiscardedSpaces { get; set; } = new();

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, int> LevelSummary { get; set; } = new();

        [JsonPropertyName("totalAreaM2")]
        public double TotalAreaM2 { get; set; }

        [JsonPropertyName("spacesWithRoom")]
        public int SpacesWithRoom { get; set; }

        [JsonPropertyName("spacesWithoutRoom")]
        public int SpacesWithoutRoom { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public ValidSpace? GetById(long elementId) =>
            ValidSpaces.FirstOrDefault(s => s.ElementId == elementId);

        public ValidSpace? GetByName(string name) =>
            ValidSpaces.FirstOrDefault(s =>
                string.Equals(s.Name, name,
                    StringComparison.OrdinalIgnoreCase));

        public ValidSpace? GetByNumber(string number) =>
            ValidSpaces.FirstOrDefault(s =>
                string.Equals(s.Number, number,
                    StringComparison.OrdinalIgnoreCase));

        public List<ValidSpace> GetByLevel(string levelName) =>
            ValidSpaces.Where(s =>
                string.Equals(s.LevelName, levelName,
                    StringComparison.OrdinalIgnoreCase)).ToList();

        public List<ValidSpace> GetByMinArea(double minAreaM2) =>
            ValidSpaces.Where(s => s.AreaM2 >= minAreaM2).ToList();

        public ValidSpace? GetByRoomId(long roomId) =>
            ValidSpaces.FirstOrDefault(s =>
                s.AssociatedRoomId == roomId);

        public List<ValidSpace> GetUnassociated() =>
            ValidSpaces.Where(s => s.AssociatedRoomId <= 0).ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: LEITURA DE SPACES MEP
    // ══════════════════════════════════════════════════════════════

    public interface ISpaceReaderService
    {
        SpaceCollectionResult ColetarSpacesValidos(Document doc);
    }

    public class SpaceReaderService : ISpaceReaderService
    {
        public event Action<string>? OnProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  COLETA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        public SpaceCollectionResult ColetarSpacesValidos(Document doc)
        {
            var result = new SpaceCollectionResult();

            EmitProgress("Coletando Spaces MEP do modelo...");

            // ── 1. Coletar todos os Spaces ──
            var allSpaces = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .ToElements()
                .Cast<Space>()
                .ToList();

            result.TotalFound = allSpaces.Count;

            if (allSpaces.Count == 0)
            {
                result.Success = false;
                EmitProgress("Nenhum Space MEP encontrado no modelo.");
                return result;
            }

            EmitProgress($"Encontrados {allSpaces.Count} Spaces. " +
                         "Validando e extraindo...");

            // ── 2. Validar e extrair ──
            foreach (var space in allSpaces)
            {
                var discardReason = ValidarSpace(space);

                if (discardReason != null)
                {
                    result.DiscardedSpaces.Add(new DiscardedSpace
                    {
                        ElementId = space.Id.Value,
                        Name = space.Name ?? "(sem nome)",
                        Reason = discardReason
                    });
                    continue;
                }

                var validSpace = ExtrairDados(doc, space);
                result.ValidSpaces.Add(validSpace);
            }

            // ── 3. Estatísticas ──
            result.ValidCount = result.ValidSpaces.Count;
            result.DiscardedCount = result.DiscardedSpaces.Count;
            result.TotalAreaM2 = Math.Round(
                result.ValidSpaces.Sum(s => s.AreaM2), 2);
            result.SpacesWithRoom = result.ValidSpaces
                .Count(s => s.AssociatedRoomId > 0);
            result.SpacesWithoutRoom = result.ValidCount -
                                       result.SpacesWithRoom;
            result.Success = result.ValidCount > 0;

            // Resumo por Level
            foreach (var group in result.ValidSpaces
                         .GroupBy(s => s.LevelName))
            {
                result.LevelSummary[group.Key] = group.Count();
            }

            // ── 4. Log ──
            PersistirLog(result);

            EmitProgress($"Coleta concluída: {result.ValidCount} válidos, " +
                         $"{result.DiscardedCount} descartados, " +
                         $"área total: {result.TotalAreaM2:F2}m², " +
                         $"{result.SpacesWithRoom} com Room associado");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        private static string? ValidarSpace(Space space)
        {
            // 1 — Location
            if (space.Location == null)
                return "Sem Location (Space não colocado)";

            if (space.Location is not LocationPoint)
                return "Location inválida (não é LocationPoint)";

            // 2 — Área
            var areaFt2 = space.get_Parameter(
                BuiltInParameter.ROOM_AREA)?.AsDouble() ?? 0;
            if (areaFt2 <= 0)
                return "Área zero ou negativa";

            // 3 — Level
            if (space.LevelId == ElementId.InvalidElementId)
                return "Sem Level atribuído";

            // 4 — Boundaries (enclosed)
            try
            {
                var segments = space.GetBoundarySegments(
                    new SpatialElementBoundaryOptions());
                if (segments == null || segments.Count == 0)
                    return "Space sem boundary segments (não enclosed)";
            }
            catch
            {
                return "Erro ao obter boundary segments";
            }

            return null; // válido
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRAÇÃO DE DADOS
        // ══════════════════════════════════════════════════════════

        private static ValidSpace ExtrairDados(Document doc, Space space)
        {
            var vs = new ValidSpace
            {
                ElementId = space.Id.Value,
                RevitSpace = space
            };

            // ── Nome e Number ──
            vs.Name = space.get_Parameter(BuiltInParameter.ROOM_NAME)
                          ?.AsString() ?? space.Name ?? "";
            vs.Number = space.get_Parameter(BuiltInParameter.ROOM_NUMBER)
                            ?.AsString() ?? "";

            // ── Level ──
            var level = doc.GetElement(space.LevelId) as Level;
            vs.LevelId = space.LevelId.Value;
            vs.LevelName = level?.Name ?? "";
            vs.LevelElevationM = UnitConverter.GetLevelElevationM(level!);

            // ── Área ──
            vs.AreaM2 = UnitConverter.SqFtToSqM(
                space.get_Parameter(BuiltInParameter.ROOM_AREA)
                    ?.AsDouble() ?? 0);

            // ── Perímetro ──
            vs.PerimeterM = UnitConverter.FtToM(
                space.get_Parameter(BuiltInParameter.ROOM_PERIMETER)
                    ?.AsDouble() ?? 0);

            // ── Volume ──
            vs.VolumeM3 = UnitConverter.CuFtToCuM(
                space.get_Parameter(BuiltInParameter.ROOM_VOLUME)
                    ?.AsDouble() ?? 0);

            // ── Altura ──
            vs.HeightM = UnitConverter.FtToM(
                space.get_Parameter(BuiltInParameter.ROOM_HEIGHT)
                    ?.AsDouble() ?? 0);

            // ── Centroid ──
            if (space.Location is LocationPoint lp)
            {
                vs.Centroid = UnitConverter.XyzToMeters(lp.Point);
            }

            // ── Department ──
            vs.Department = space.get_Parameter(
                BuiltInParameter.ROOM_DEPARTMENT)?.AsString() ?? "";

            // ── Phase ──
            try
            {
                var phase = doc.GetElement(space.CreatedPhaseId);
                vs.Phase = phase?.Name ?? "";
            }
            catch
            {
                vs.Phase = "";
            }

            // ── Space Type ──
            try
            {
                var spaceTypeParam = space.LookupParameter("Space Type");
                if (spaceTypeParam != null)
                {
                    vs.SpaceType = spaceTypeParam.AsValueString() ?? "";
                }
            }
            catch
            {
                vs.SpaceType = "";
            }

            // ── Condition Type ──
            try
            {
                var condParam = space.LookupParameter("Condition Type");
                if (condParam != null)
                {
                    vs.ConditionType = condParam.AsValueString() ?? "";
                }
            }
            catch
            {
                vs.ConditionType = "";
            }

            // ── Occupancy ──
            try
            {
                var occParam = space.LookupParameter("Actual Occupancy")
                            ?? space.LookupParameter("Number of People")
                            ?? space.LookupParameter("Occupancy");
                if (occParam != null)
                {
                    vs.Occupancy = occParam.StorageType == StorageType.Integer
                        ? occParam.AsInteger()
                        : (int)occParam.AsDouble();
                }
            }
            catch
            {
                vs.Occupancy = 0;
            }

            // ── IsEnclosed ──
            try
            {
                var segments = space.GetBoundarySegments(
                    new SpatialElementBoundaryOptions());
                vs.IsEnclosed = segments != null && segments.Count > 0;
            }
            catch
            {
                vs.IsEnclosed = false;
            }

            // ── Room associado ──
            ExtrairRoomAssociado(doc, space, vs);

            // ── Parâmetros customizados ──
            ExtrairParametrosCustom(space, vs);

            return vs;
        }

        // ══════════════════════════════════════════════════════════
        //  ROOM ASSOCIADO
        // ══════════════════════════════════════════════════════════

        private static void ExtrairRoomAssociado(
            Document doc, Space space, ValidSpace vs)
        {
            try
            {
                // Tentativa 1: Mesmo ponto → Room naquele ponto
                if (space.Location is LocationPoint lp)
                {
                    // Buscar phase do Space
                    Phase? phase = null;
                    try
                    {
                        phase = doc.GetElement(space.CreatedPhaseId) as Phase;
                    }
                    catch { }

                    if (phase != null)
                    {
                        var room = doc.GetRoomAtPoint(lp.Point, phase);
                        if (room != null)
                        {
                            vs.AssociatedRoomId = room.Id.Value;
                            var roomName = room.get_Parameter(
                                BuiltInParameter.ROOM_NAME)?.AsString();
                            vs.AssociatedRoomName = roomName ?? room.Name ?? "";
                            return;
                        }
                    }
                }

                // Tentativa 2: Room com mesmo nome e nível
                var rooms = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var elem in rooms)
                {
                    var roomName = elem.get_Parameter(
                        BuiltInParameter.ROOM_NAME)?.AsString() ?? "";

                    if (string.Equals(roomName, vs.Name,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var roomLevel = elem.get_Parameter(
                            BuiltInParameter.ROOM_LEVEL_ID);
                        if (roomLevel != null &&
                            roomLevel.AsElementId().Value ==
                            vs.LevelId)
                        {
                            vs.AssociatedRoomId = elem.Id.Value;
                            vs.AssociatedRoomName = roomName;
                            return;
                        }
                    }
                }
            }
            catch
            {
                // associação é best-effort
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PARÂMETROS CUSTOMIZADOS
        // ══════════════════════════════════════════════════════════

        private static void ExtrairParametrosCustom(
            Space space, ValidSpace vs)
        {
            try
            {
                foreach (Parameter param in space.Parameters)
                {
                    if (!param.IsShared && !param.IsReadOnly)
                        continue;

                    if (param.Definition == null || !param.HasValue)
                        continue;

                    var name = param.Definition.Name;

                    // Pular já extraídos
                    if (name is "Name" or "Number" or "Area" or
                        "Perimeter" or "Volume" or "Department" or
                        "Level")
                        continue;

                    string value;
                    switch (param.StorageType)
                    {
                        case StorageType.String:
                            value = param.AsString() ?? "";
                            break;
                        case StorageType.Double:
                            value = param.AsDouble().ToString("F4");
                            break;
                        case StorageType.Integer:
                            value = param.AsInteger().ToString();
                            break;
                        case StorageType.ElementId:
                            value = param.AsElementId().Value
                                .ToString();
                            break;
                        default:
                            continue;
                    }

                    if (!string.IsNullOrWhiteSpace(value) &&
                        value != "0" && value != "0.0000" &&
                        value != "-1")
                    {
                        vs.CustomParameters[name] = value;
                    }
                }
            }
            catch
            {
                // best-effort
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PERSISTÊNCIA
        // ══════════════════════════════════════════════════════════

        private void PersistirLog(SpaceCollectionResult result)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Spaces");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"spaces_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(result, JsonOpts);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                EmitProgress($"Log salvo: {filePath}");
            }
            catch
            {
                // não quebrar fluxo
            }
        }

        private void EmitProgress(string message)
        {
            OnProgress?.Invoke(message);
        }
    }
}
