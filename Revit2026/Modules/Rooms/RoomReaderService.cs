using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace Revit2026.Modules.Rooms
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO DE DADOS — ROOM VÁLIDO
    // ══════════════════════════════════════════════════════════════

    public class ValidRoom
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

        [JsonPropertyName("centroid")]
        public double[] Centroid { get; set; } = Array.Empty<double>();

        [JsonPropertyName("department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("phase")]
        public string Phase { get; set; } = "";

        [JsonPropertyName("upperLimitId")]
        public long UpperLimitId { get; set; }

        [JsonPropertyName("limitOffsetM")]
        public double LimitOffsetM { get; set; }

        [JsonPropertyName("unboundedHeightM")]
        public double UnboundedHeightM { get; set; }

        [JsonPropertyName("isEnclosed")]
        public bool IsEnclosed { get; set; }

        /// Referência ao objeto Revit (não serializado)
        [JsonIgnore]
        public Room? RevitRoom { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  MOTIVO DE DESCARTE
    // ══════════════════════════════════════════════════════════════

    public class DiscardedRoom
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

    public class RoomCollectionResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalFound")]
        public int TotalFound { get; set; }

        [JsonPropertyName("validCount")]
        public int ValidCount { get; set; }

        [JsonPropertyName("discardedCount")]
        public int DiscardedCount { get; set; }

        [JsonPropertyName("validRooms")]
        public List<ValidRoom> ValidRooms { get; set; } = new();

        [JsonPropertyName("discardedRooms")]
        public List<DiscardedRoom> DiscardedRooms { get; set; } = new();

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, int> LevelSummary { get; set; } = new();

        [JsonPropertyName("totalAreaM2")]
        public double TotalAreaM2 { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public ValidRoom? GetById(int elementId) =>
            ValidRooms.FirstOrDefault(r => r.ElementId == elementId);

        public ValidRoom? GetByName(string name) =>
            ValidRooms.FirstOrDefault(r =>
                string.Equals(r.Name, name,
                    StringComparison.OrdinalIgnoreCase));

        public ValidRoom? GetByNumber(string number) =>
            ValidRooms.FirstOrDefault(r =>
                string.Equals(r.Number, number,
                    StringComparison.OrdinalIgnoreCase));

        public List<ValidRoom> GetByLevel(string levelName) =>
            ValidRooms.Where(r =>
                string.Equals(r.LevelName, levelName,
                    StringComparison.OrdinalIgnoreCase)).ToList();

        public List<ValidRoom> GetByMinArea(double minAreaM2) =>
            ValidRooms.Where(r => r.AreaM2 >= minAreaM2).ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: LEITURA DE ROOMS VÁLIDOS
    // ══════════════════════════════════════════════════════════════

    public interface IRoomReaderService
    {
        RoomCollectionResult ColetarRoomsValidos(Document doc);
    }

    public class RoomReaderService : IRoomReaderService
    {
        // Constantes de conversão
        private const double SqFtToSqM = 0.09290304;
        private const double FtToM = 0.3048;
        private const double CuFtToCuM = 0.028316846592;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public event Action<string>? OnProgress;

        // ══════════════════════════════════════════════════════════
        //  COLETA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        public RoomCollectionResult ColetarRoomsValidos(Document doc)
        {
            var result = new RoomCollectionResult();

            EmitProgress("Coletando Rooms do modelo...");

            // ── 1. Coletar todos os Rooms ──
            var allRooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .ToElements()
                .Cast<Room>()
                .ToList();

            result.TotalFound = allRooms.Count;

            if (allRooms.Count == 0)
            {
                result.Success = false;
                EmitProgress("Nenhum Room encontrado no modelo.");
                return result;
            }

            EmitProgress($"Encontrados {allRooms.Count} Rooms. Filtrando...");

            // ── 2. Filtrar e extrair ──
            foreach (var room in allRooms)
            {
                var discardReason = ValidarRoom(room);

                if (discardReason != null)
                {
                    result.DiscardedRooms.Add(new DiscardedRoom
                    {
                        ElementId = room.Id.Value,
                        Name = room.Name ?? "(sem nome)",
                        Reason = discardReason
                    });
                    continue;
                }

                var validRoom = ExtrairDados(doc, room);
                result.ValidRooms.Add(validRoom);
            }

            // ── 3. Estatísticas ──
            result.ValidCount = result.ValidRooms.Count;
            result.DiscardedCount = result.DiscardedRooms.Count;
            result.TotalAreaM2 = Math.Round(
                result.ValidRooms.Sum(r => r.AreaM2), 2);
            result.Success = result.ValidCount > 0;

            // Resumo por Level
            foreach (var group in result.ValidRooms.GroupBy(r => r.LevelName))
            {
                result.LevelSummary[group.Key] = group.Count();
            }

            // ── 4. Log ──
            PersistirLog(result);

            EmitProgress($"Coleta concluída: {result.ValidCount} válidos, " +
                         $"{result.DiscardedCount} descartados, " +
                         $"área total: {result.TotalAreaM2:F2}m²");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE ROOM INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// Retorna motivo do descarte, ou null se válido.
        private static string? ValidarRoom(Room room)
        {
            // 1 — Location
            if (room.Location == null)
                return "Sem Location (Room não colocado)";

            if (room.Location is not LocationPoint)
                return "Location inválida (não é LocationPoint)";

            // 2 — Área
            var areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
            var areaFt2 = areaParam?.AsDouble() ?? 0;
            if (areaFt2 <= 0)
                return "Área zero ou negativa";

            // 3 — Nome
            var name = room.get_Parameter(BuiltInParameter.ROOM_NAME)
                           ?.AsString();
            if (string.IsNullOrWhiteSpace(name))
                return "Sem nome definido";

            // 4 — Phase
            try
            {
                var phase = room.Document.GetElement(room.CreatedPhaseId);
                if (phase == null)
                    return "Phase inválida";
            }
            catch
            {
                // Phase não resolvível — aceitar mesmo assim
            }

            // 5 — Enclosed (room fechado por boundaries)
            // Room com área > 0 e Location != null é considerado enclosed
            // mas verificamos via bounded check
            try
            {
                var segments = room.GetBoundarySegments(
                    new SpatialElementBoundaryOptions());
                if (segments == null || segments.Count == 0)
                    return "Room sem boundary segments (não enclosed)";
            }
            catch
            {
                return "Erro ao obter boundary segments";
            }

            // 6 — Level
            if (room.LevelId == ElementId.InvalidElementId)
                return "Sem Level atribuído";

            return null; // válido
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRAÇÃO DE DADOS
        // ══════════════════════════════════════════════════════════

        private static ValidRoom ExtrairDados(Document doc, Room room)
        {
            var vr = new ValidRoom
            {
                ElementId = room.Id.Value,
                RevitRoom = room
            };

            // ── Nome e Number ──
            vr.Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)
                          ?.AsString() ?? "";
            vr.Number = room.get_Parameter(BuiltInParameter.ROOM_NUMBER)
                            ?.AsString() ?? "";

            // ── Level ──
            var level = doc.GetElement(room.LevelId) as Level;
            vr.LevelId = room.LevelId.Value;
            vr.LevelName = level?.Name ?? "";
            vr.LevelElevationM = Math.Round(
                (level?.Elevation ?? 0) * FtToM, 4);

            // ── Área (ft² → m²) ──
            var areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
            vr.AreaM2 = Math.Round(
                (areaParam?.AsDouble() ?? 0) * SqFtToSqM, 4);

            // ── Perímetro (ft → m) ──
            var perimParam = room.get_Parameter(
                BuiltInParameter.ROOM_PERIMETER);
            vr.PerimeterM = Math.Round(
                (perimParam?.AsDouble() ?? 0) * FtToM, 4);

            // ── Volume (ft³ → m³) ──
            var volParam = room.get_Parameter(BuiltInParameter.ROOM_VOLUME);
            vr.VolumeM3 = Math.Round(
                (volParam?.AsDouble() ?? 0) * CuFtToCuM, 4);

            // ── Centroid ──
            if (room.Location is LocationPoint lp)
            {
                var pt = lp.Point;
                vr.Centroid = new[]
                {
                    Math.Round(pt.X * FtToM, 4),
                    Math.Round(pt.Y * FtToM, 4),
                    Math.Round(pt.Z * FtToM, 4)
                };
            }

            // ── Department ──
            var deptParam = room.get_Parameter(
                BuiltInParameter.ROOM_DEPARTMENT);
            vr.Department = deptParam?.AsString() ?? "";

            // ── Phase ──
            try
            {
                var phase = doc.GetElement(room.CreatedPhaseId);
                vr.Phase = phase?.Name ?? "";
            }
            catch
            {
                vr.Phase = "";
            }

            // ── Upper Limit ──
            var upperParam = room.get_Parameter(
                BuiltInParameter.ROOM_UPPER_LEVEL);
            if (upperParam != null)
            {
                vr.UpperLimitId = upperParam.AsElementId().Value;
            }

            // ── Limit Offset ──
            var offsetParam = room.get_Parameter(
                BuiltInParameter.ROOM_UPPER_OFFSET);
            vr.LimitOffsetM = Math.Round(
                (offsetParam?.AsDouble() ?? 0) * FtToM, 4);

            // ── Unbounded Height ──
            var unbHeightParam = room.get_Parameter(
                BuiltInParameter.ROOM_HEIGHT);
            vr.UnboundedHeightM = Math.Round(
                (unbHeightParam?.AsDouble() ?? 0) * FtToM, 4);

            // ── IsEnclosed ──
            try
            {
                var segments = room.GetBoundarySegments(
                    new SpatialElementBoundaryOptions());
                vr.IsEnclosed = segments != null && segments.Count > 0;
            }
            catch
            {
                vr.IsEnclosed = false;
            }

            return vr;
        }

        // ══════════════════════════════════════════════════════════
        //  PERSISTÊNCIA
        // ══════════════════════════════════════════════════════════

        private void PersistirLog(RoomCollectionResult result)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Rooms");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"rooms_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                // Serializar sem RevitRoom (JsonIgnore)
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
