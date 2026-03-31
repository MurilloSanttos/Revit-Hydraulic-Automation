using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace Revit2026.Modules.Rooms
{
    // ══════════════════════════════════════════════════════════════
    //  METADADOS EXTRAÍDOS POR ROOM
    // ══════════════════════════════════════════════════════════════

    public class RoomMetadata
    {
        // ── Identificação ──

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        // ── Nível ──

        [JsonPropertyName("level")]
        public string Level { get; set; } = "";

        [JsonPropertyName("levelId")]
        public long LevelId { get; set; }

        [JsonPropertyName("levelElevationM")]
        public double LevelElevationM { get; set; }

        // ── Dimensões (unidades SI) ──

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("perimeterM")]
        public double PerimeterM { get; set; }

        [JsonPropertyName("volumeM3")]
        public double VolumeM3 { get; set; }

        [JsonPropertyName("heightM")]
        public double HeightM { get; set; }

        [JsonPropertyName("estimatedWidthM")]
        public double EstimatedWidthM { get; set; }

        [JsonPropertyName("estimatedLengthM")]
        public double EstimatedLengthM { get; set; }

        // ── Centroid ──

        [JsonPropertyName("centroid")]
        public PointData Centroid { get; set; } = new();

        // ── Classificação ──

        [JsonPropertyName("department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("phase")]
        public string Phase { get; set; } = "";

        [JsonPropertyName("isEnclosed")]
        public bool IsEnclosed { get; set; }

        // ── Boundaries ──

        [JsonPropertyName("wallCount")]
        public int WallCount { get; set; }

        [JsonPropertyName("boundarySegmentCount")]
        public int BoundarySegmentCount { get; set; }

        [JsonPropertyName("boundaryLoopCount")]
        public int BoundaryLoopCount { get; set; }

        [JsonPropertyName("walls")]
        public List<WallData> Walls { get; set; } = new();

        // ── Adjacências ──

        [JsonPropertyName("adjacentRoomIds")]
        public List<long> AdjacentRoomIds { get; set; } = new();

        [JsonPropertyName("adjacentRoomNames")]
        public List<string> AdjacentRoomNames { get; set; } = new();

        // ── Fixtures existentes no Room ──

        [JsonPropertyName("fixtureCount")]
        public int FixtureCount { get; set; }

        [JsonPropertyName("fixtures")]
        public List<FixtureData> Fixtures { get; set; } = new();

        // ── Bounding Box ──

        [JsonPropertyName("boundingBoxMin")]
        public PointData BoundingBoxMin { get; set; } = new();

        [JsonPropertyName("boundingBoxMax")]
        public PointData BoundingBoxMax { get; set; } = new();

        // ── Parâmetros customizados ──

        [JsonPropertyName("customParameters")]
        public Dictionary<string, string> CustomParameters { get; set; } = new();

        // ── Referência Revit ──

        [JsonIgnore]
        public Room? RevitRoom { get; set; }
    }

    public class PointData
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("z")]
        public double Z { get; set; }
    }

    public class WallData
    {
        [JsonPropertyName("wallId")]
        public long WallId { get; set; }

        [JsonPropertyName("wallName")]
        public string WallName { get; set; } = "";

        [JsonPropertyName("wallType")]
        public string WallType { get; set; } = "";

        [JsonPropertyName("lengthM")]
        public double LengthM { get; set; }

        [JsonPropertyName("isExterior")]
        public bool IsExterior { get; set; }
    }

    public class FixtureData
    {
        [JsonPropertyName("fixtureId")]
        public long FixtureId { get; set; }

        [JsonPropertyName("familyName")]
        public string FamilyName { get; set; } = "";

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; } = "";

        [JsonPropertyName("position")]
        public PointData Position { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DA EXTRAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class MetadataExtractionResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("processedCount")]
        public int ProcessedCount { get; set; }

        [JsonPropertyName("validCount")]
        public int ValidCount { get; set; }

        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }

        [JsonPropertyName("rooms")]
        public List<RoomMetadata> Rooms { get; set; } = new();

        [JsonPropertyName("errors")]
        public List<ExtractionError> Errors { get; set; } = new();

        [JsonPropertyName("totalAreaM2")]
        public double TotalAreaM2 { get; set; }

        [JsonPropertyName("totalFixtureCount")]
        public int TotalFixtureCount { get; set; }

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, LevelMetaSummary> LevelSummary { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public RoomMetadata? GetById(long id) =>
            Rooms.FirstOrDefault(r => r.Id == id);

        public RoomMetadata? GetByName(string name) =>
            Rooms.FirstOrDefault(r =>
                string.Equals(r.Name, name,
                    StringComparison.OrdinalIgnoreCase));

        public RoomMetadata? GetByNumber(string number) =>
            Rooms.FirstOrDefault(r =>
                string.Equals(r.Number, number,
                    StringComparison.OrdinalIgnoreCase));

        public List<RoomMetadata> GetByLevel(string level) =>
            Rooms.Where(r =>
                string.Equals(r.Level, level,
                    StringComparison.OrdinalIgnoreCase)).ToList();

        public List<RoomMetadata> GetWithFixtures() =>
            Rooms.Where(r => r.FixtureCount > 0).ToList();

        public List<RoomMetadata> GetWithoutFixtures() =>
            Rooms.Where(r => r.FixtureCount == 0).ToList();
    }

    public class ExtractionError
    {
        [JsonPropertyName("roomId")]
        public long RoomId { get; set; }

        [JsonPropertyName("roomName")]
        public string RoomName { get; set; } = "";

        [JsonPropertyName("field")]
        public string Field { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    public class LevelMetaSummary
    {
        [JsonPropertyName("roomCount")]
        public int RoomCount { get; set; }

        [JsonPropertyName("totalAreaM2")]
        public double TotalAreaM2 { get; set; }

        [JsonPropertyName("fixtureCount")]
        public int FixtureCount { get; set; }

        [JsonPropertyName("wallCount")]
        public int WallCount { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: EXTRAÇÃO DE METADADOS
    // ══════════════════════════════════════════════════════════════

    public interface IRoomMetadataExtractor
    {
        MetadataExtractionResult Extrair(
            Document doc,
            RoomFilterResult filtrado);

        MetadataExtractionResult Extrair(
            Document doc,
            List<ValidRoom> rooms);
    }

    public class RoomMetadataExtractor : IRoomMetadataExtractor
    {
        private const double FtToM = 0.3048;
        private const double SqFtToSqM = 0.09290304;
        private const double CuFtToCuM = 0.028316846592;

        public event Action<string>? OnProgress;
        public event Action<int, int>? OnRoomProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  EXTRAIR A PARTIR DE FILTER RESULT
        // ══════════════════════════════════════════════════════════

        public MetadataExtractionResult Extrair(
            Document doc,
            RoomFilterResult filtrado)
        {
            return Extrair(doc, filtrado.ValidRooms);
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRAIR A PARTIR DE LISTA
        // ══════════════════════════════════════════════════════════

        public MetadataExtractionResult Extrair(
            Document doc,
            List<ValidRoom> rooms)
        {
            var result = new MetadataExtractionResult
            {
                ProcessedCount = rooms.Count
            };

            EmitProgress($"Extraindo metadados de {rooms.Count} Rooms...");

            // Pré-coletar fixtures por Room para performance
            var fixturesByRoom = PreCollectFixturesByRoom(doc);

            int idx = 0;
            foreach (var vr in rooms)
            {
                idx++;
                OnRoomProgress?.Invoke(idx, rooms.Count);

                try
                {
                    var room = vr.RevitRoom ??
                               doc.GetElement(
                                   new ElementId(vr.ElementId)) as Room;

                    if (room == null)
                    {
                        result.Errors.Add(new ExtractionError
                        {
                            RoomId = (long)vr.ElementId,
                            RoomName = vr.Name,
                            Field = "Element",
                            Message = "Room não encontrado no modelo"
                        });
                        continue;
                    }

                    var meta = ExtrairMetadados(doc, room, vr, fixturesByRoom,
                        result.Errors);
                    result.Rooms.Add(meta);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ExtractionError
                    {
                        RoomId = (long)vr.ElementId,
                        RoomName = vr.Name,
                        Field = "Global",
                        Message = ex.Message
                    });
                }
            }

            // ── Estatísticas ──
            result.ValidCount = result.Rooms.Count;
            result.ErrorCount = result.Errors.Count;
            result.TotalAreaM2 = Math.Round(
                result.Rooms.Sum(r => r.AreaM2), 2);
            result.TotalFixtureCount =
                result.Rooms.Sum(r => r.FixtureCount);
            result.Success = result.ValidCount > 0;

            // Resumo por Level
            foreach (var group in result.Rooms.GroupBy(r => r.Level))
            {
                result.LevelSummary[group.Key] = new LevelMetaSummary
                {
                    RoomCount = group.Count(),
                    TotalAreaM2 = Math.Round(group.Sum(r => r.AreaM2), 2),
                    FixtureCount = group.Sum(r => r.FixtureCount),
                    WallCount = group.Sum(r => r.WallCount)
                };
            }

            // Persistir
            PersistirResultado(result);

            EmitProgress($"Extração concluída: {result.ValidCount} rooms, " +
                         $"{result.TotalFixtureCount} fixtures, " +
                         $"{result.ErrorCount} erros");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRAÇÃO POR ROOM
        // ══════════════════════════════════════════════════════════

        private RoomMetadata ExtrairMetadados(
            Document doc,
            Room room,
            ValidRoom vr,
            Dictionary<long, List<FamilyInstance>> fixturesByRoom,
            List<ExtractionError> errors)
        {
            var meta = new RoomMetadata
            {
                Id = room.Id.Value,
                RevitRoom = room
            };

            // ── 1. Identificação ──
            meta.Name = SafeString(room, BuiltInParameter.ROOM_NAME);
            meta.Number = SafeString(room, BuiltInParameter.ROOM_NUMBER);

            // ── 2. Level ──
            var level = doc.GetElement(room.LevelId) as Level;
            meta.Level = level?.Name ?? "";
            meta.LevelId = (long)room.LevelId.Value;
            meta.LevelElevationM = Round(
                (level?.Elevation ?? 0) * FtToM);

            // ── 3. Dimensões ──
            meta.AreaM2 = Round(SafeDouble(room, BuiltInParameter.ROOM_AREA) * SqFtToSqM);
            meta.PerimeterM = Round(SafeDouble(room, BuiltInParameter.ROOM_PERIMETER) * FtToM);
            meta.VolumeM3 = Round(SafeDouble(room, BuiltInParameter.ROOM_VOLUME) * CuFtToCuM);
            meta.HeightM = Round(SafeDouble(room, BuiltInParameter.ROOM_HEIGHT) * FtToM);

            // Estimativa de largura e comprimento
            // (assume retangular: A = L×W, P = 2(L+W))
            EstimarDimensoes(meta);

            // ── 4. Centroid ──
            if (room.Location is LocationPoint lp)
            {
                meta.Centroid = new PointData
                {
                    X = Round(lp.Point.X * FtToM),
                    Y = Round(lp.Point.Y * FtToM),
                    Z = Round(lp.Point.Z * FtToM)
                };
            }

            // ── 5. Classificação ──
            meta.Department = SafeString(room, BuiltInParameter.ROOM_DEPARTMENT);
            meta.Phase = GetPhaseName(doc, room);
            meta.IsEnclosed = vr.IsEnclosed;

            // ── 6. Bounding Box ──
            var bb = room.get_BoundingBox(null);
            if (bb != null)
            {
                meta.BoundingBoxMin = new PointData
                {
                    X = Round(bb.Min.X * FtToM),
                    Y = Round(bb.Min.Y * FtToM),
                    Z = Round(bb.Min.Z * FtToM)
                };
                meta.BoundingBoxMax = new PointData
                {
                    X = Round(bb.Max.X * FtToM),
                    Y = Round(bb.Max.Y * FtToM),
                    Z = Round(bb.Max.Z * FtToM)
                };
            }

            // ── 7. Boundaries e Walls ──
            ExtrairBoundaries(doc, room, meta, errors);

            // ── 8. Adjacências ──
            ExtrairAdjacencias(doc, room, meta);

            // ── 9. Fixtures ──
            ExtrairFixtures(doc, room, meta, fixturesByRoom);

            // ── 10. Parâmetros customizados ──
            ExtrairParametrosCustom(room, meta);

            return meta;
        }

        // ══════════════════════════════════════════════════════════
        //  BOUNDARIES E WALLS
        // ══════════════════════════════════════════════════════════

        private static void ExtrairBoundaries(
            Document doc,
            Room room,
            RoomMetadata meta,
            List<ExtractionError> errors)
        {
            try
            {
                var options = new SpatialElementBoundaryOptions();
                var loops = room.GetBoundarySegments(options);

                if (loops == null || loops.Count == 0)
                    return;

                meta.BoundaryLoopCount = loops.Count;
                var wallIds = new HashSet<long>();
                int segCount = 0;

                foreach (var loop in loops)
                {
                    foreach (var segment in loop)
                    {
                        segCount++;
                        var elemId = segment.ElementId;

                        if (elemId == ElementId.InvalidElementId)
                            continue;

                        var elem = doc.GetElement(elemId);
                        if (elem is not Wall wall) continue;

                        if (wallIds.Contains((long)wall.Id.Value))
                            continue;

                        wallIds.Add((long)wall.Id.Value);

                        var wallType = doc.GetElement(
                            wall.GetTypeId()) as WallType;

                        var curve = segment.GetCurve();
                        var segLen = curve?.Length ?? 0;

                        // Verificar se é parede exterior
                        var funcParam = wall.get_Parameter(
                            BuiltInParameter.FUNCTION_PARAM);
                        var isExterior = funcParam != null &&
                                         funcParam.AsInteger() == 1;

                        meta.Walls.Add(new WallData
                        {
                            WallId = wall.Id.Value,
                            WallName = wall.Name,
                            WallType = wallType?.Name ?? "",
                            LengthM = Round(segLen * FtToM),
                            IsExterior = isExterior
                        });
                    }
                }

                meta.BoundarySegmentCount = segCount;
                meta.WallCount = wallIds.Count;
            }
            catch (Exception ex)
            {
                errors.Add(new ExtractionError
                {
                    RoomId = room.Id.Value,
                    RoomName = room.Name,
                    Field = "Boundaries",
                    Message = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ADJACÊNCIAS
        // ══════════════════════════════════════════════════════════

        private static void ExtrairAdjacencias(
            Document doc,
            Room room,
            RoomMetadata meta)
        {
            try
            {
                var options = new SpatialElementBoundaryOptions();
                var loops = room.GetBoundarySegments(options);
                if (loops == null) return;

                var adjacentIds = new HashSet<long>();

                foreach (var loop in loops)
                {
                    foreach (var segment in loop)
                    {
                        var elemId = segment.ElementId;
                        if (elemId == ElementId.InvalidElementId) continue;

                        var elem = doc.GetElement(elemId);
                        if (elem is not Wall wall) continue;

                        // Buscar Room do outro lado da parede
                        var otherRoom = FindRoomOnOtherSide(
                            doc, wall, room);

                        if (otherRoom != null &&
                            !adjacentIds.Contains(otherRoom.Id.Value))
                        {
                            adjacentIds.Add(otherRoom.Id.Value);
                            meta.AdjacentRoomIds.Add(
                                otherRoom.Id.Value);

                            var adjName = otherRoom.get_Parameter(
                                BuiltInParameter.ROOM_NAME)?.AsString();
                            meta.AdjacentRoomNames.Add(adjName ?? "");
                        }
                    }
                }
            }
            catch
            {
                // adjacências são best-effort
            }
        }

        private static Room? FindRoomOnOtherSide(
            Document doc, Wall wall, Room currentRoom)
        {
            try
            {
                var loc = wall.Location as LocationCurve;
                if (loc?.Curve == null) return null;

                var midPoint = loc.Curve.Evaluate(0.5, true);
                var direction = loc.Curve.ComputeDerivatives(0.5, true)
                    .BasisX.Normalize();

                // Normal da parede
                var normal = new XYZ(-direction.Y, direction.X, 0);
                var offset = 0.5; // ~15cm para cada lado

                var ptA = midPoint + normal * offset;
                var ptB = midPoint - normal * offset;

                var phase = doc.GetElement(currentRoom.CreatedPhaseId) as Phase;
                if (phase == null) return null;

                var roomA = doc.GetRoomAtPoint(ptA, phase);
                var roomB = doc.GetRoomAtPoint(ptB, phase);

                if (roomA != null &&
                    roomA.Id.Value != currentRoom.Id.Value)
                    return roomA;

                if (roomB != null &&
                    roomB.Id.Value != currentRoom.Id.Value)
                    return roomB;
            }
            catch
            {
                // best-effort
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  FIXTURES NO ROOM
        // ══════════════════════════════════════════════════════════

        private static Dictionary<long, List<FamilyInstance>>
            PreCollectFixturesByRoom(Document doc)
        {
            var map = new Dictionary<long, List<FamilyInstance>>();

            var fixtures = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var elem in fixtures)
            {
                var fi = elem as FamilyInstance;
                if (fi == null) continue;

                if (fi.Location is not LocationPoint lp) continue;

                // Tentar encontrar o Room que contém este fixture
                try
                {
                    var roomParam = fi.get_Parameter(
                        BuiltInParameter.ELEM_ROOM_ID);
                    if (roomParam != null)
                    {
                        var roomId = roomParam.AsElementId().Value;
                        if (roomId > 0)
                        {
                            if (!map.ContainsKey(roomId))
                                map[roomId] = new List<FamilyInstance>();
                            map[roomId].Add(fi);
                            continue;
                        }
                    }

                    // Fallback: Room by Space
                    var room = fi.Room;
                    if (room != null)
                    {
                        var rid = room.Id.Value;
                        if (!map.ContainsKey(rid))
                            map[rid] = new List<FamilyInstance>();
                        map[rid].Add(fi);
                    }
                }
                catch
                {
                    // fixture sem Room
                }
            }

            return map;
        }

        private static void ExtrairFixtures(
            Document doc,
            Room room,
            RoomMetadata meta,
            Dictionary<long, List<FamilyInstance>> fixturesByRoom)
        {
            var roomId = room.Id.Value;

            if (!fixturesByRoom.TryGetValue(roomId, out var fixtures))
                return;

            meta.FixtureCount = fixtures.Count;

            foreach (var fi in fixtures)
            {
                var fd = new FixtureData
                {
                    FixtureId = fi.Id.Value,
                    FamilyName = fi.Symbol?.FamilyName ?? "",
                    TypeName = fi.Name ?? ""
                };

                if (fi.Location is LocationPoint lp)
                {
                    fd.Position = new PointData
                    {
                        X = Round(lp.Point.X * FtToM),
                        Y = Round(lp.Point.Y * FtToM),
                        Z = Round(lp.Point.Z * FtToM)
                    };
                }

                meta.Fixtures.Add(fd);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PARÂMETROS CUSTOMIZADOS
        // ══════════════════════════════════════════════════════════

        private static void ExtrairParametrosCustom(
            Room room,
            RoomMetadata meta)
        {
            try
            {
                foreach (Parameter param in room.Parameters)
                {
                    if (param.IsShared || param.IsReadOnly)
                    {
                        if (param.Definition == null) continue;
                        if (param.HasValue &&
                            !string.IsNullOrWhiteSpace(param.Definition.Name))
                        {
                            var name = param.Definition.Name;

                            // Evitar duplicar parâmetros já extraídos
                            if (name == "Name" || name == "Number" ||
                                name == "Area" || name == "Perimeter" ||
                                name == "Volume" || name == "Department" ||
                                name == "Level")
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
                                meta.CustomParameters[name] = value;
                            }
                        }
                    }
                }
            }
            catch
            {
                // parâmetros customizados são best-effort
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ESTIMATIVA DE DIMENSÕES
        // ══════════════════════════════════════════════════════════

        /// Assume Room retangular para estimar L × W.
        /// A = L × W, P = 2(L + W)
        /// Resolve quadrática: L² - (P/2)L + A = 0
        private static void EstimarDimensoes(RoomMetadata meta)
        {
            if (meta.AreaM2 <= 0 || meta.PerimeterM <= 0) return;

            var halfP = meta.PerimeterM / 2.0;
            var discriminant = halfP * halfP - 4 * meta.AreaM2;

            if (discriminant < 0)
            {
                // Não é retangular — usar sqrt(A) como estimativa
                var side = Math.Sqrt(meta.AreaM2);
                meta.EstimatedLengthM = Round(side);
                meta.EstimatedWidthM = Round(side);
                return;
            }

            var sqrtD = Math.Sqrt(discriminant);
            var dim1 = (halfP + sqrtD) / 2.0;
            var dim2 = (halfP - sqrtD) / 2.0;

            meta.EstimatedLengthM = Round(Math.Max(dim1, dim2));
            meta.EstimatedWidthM = Round(Math.Min(dim1, dim2));
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private static string SafeString(Room room, BuiltInParameter bip)
        {
            try
            {
                return room.get_Parameter(bip)?.AsString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static double SafeDouble(Room room, BuiltInParameter bip)
        {
            try
            {
                return room.get_Parameter(bip)?.AsDouble() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetPhaseName(Document doc, Room room)
        {
            try
            {
                var phase = doc.GetElement(room.CreatedPhaseId);
                return phase?.Name ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static double Round(double value) =>
            Math.Round(value, 4);

        private void PersistirResultado(MetadataExtractionResult result)
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
                    $"rooms_metadata_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(result, JsonOpts);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                EmitProgress($"Metadados salvos: {filePath}");
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
