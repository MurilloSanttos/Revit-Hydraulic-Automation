using System.Text.Json;
using System.Text.Json.Serialization;

namespace Revit2026.Modules.Rooms
{
    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO DO FILTRO
    // ══════════════════════════════════════════════════════════════

    public class RoomFilterConfig
    {
        /// Área mínima em m² para considerar Room válido
        [JsonPropertyName("areaMinM2")]
        public double AreaMinM2 { get; set; } = 0.50;

        /// Área máxima em m² (acima disso → warning, não descartado)
        [JsonPropertyName("areaMaxM2")]
        public double AreaMaxM2 { get; set; } = 500.0;

        /// Distância máxima entre centroids (m) para considerar duplicata
        [JsonPropertyName("duplicateDistanceM")]
        public double DuplicateDistanceM { get; set; } = 0.50;

        /// Exigir nome preenchido
        [JsonPropertyName("requireName")]
        public bool RequireName { get; set; } = true;

        /// Exigir número preenchido
        [JsonPropertyName("requireNumber")]
        public bool RequireNumber { get; set; } = false;

        /// Exigir que Room esteja enclosed (com boundaries)
        [JsonPropertyName("requireEnclosed")]
        public bool RequireEnclosed { get; set; } = true;

        /// Exigir perímetro > 0
        [JsonPropertyName("requirePerimeter")]
        public bool RequirePerimeter { get; set; } = true;

        /// Nomes de Rooms a ignorar (case-insensitive)
        [JsonPropertyName("excludedNames")]
        public HashSet<string> ExcludedNames { get; set; } = new(
            StringComparer.OrdinalIgnoreCase)
        {
            "Room",
            "Unnamed",
            "Sem Nome",
            "N/A",
            "Temp",
            "Temporary",
            "Lixo",
            "Teste",
            "Test"
        };

        /// Levels a ignorar (case-insensitive)
        [JsonPropertyName("excludedLevels")]
        public HashSet<string> ExcludedLevels { get; set; } = new(
            StringComparer.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════
    //  MOTIVO DETALHADO DE DESCARTE
    // ══════════════════════════════════════════════════════════════

    public enum DiscardReason
    {
        SemLocation,
        AreaZero,
        AreaAbaixoMinima,
        SemNome,
        NomeExcluido,
        SemNumero,
        NaoEnclosed,
        SemPerimetro,
        SemLevel,
        LevelExcluido,
        Duplicado,
        PhaseInvalida,
        SemBoundary
    }

    public class FilterDiscardEntry
    {
        [JsonPropertyName("elementId")]
        public int ElementId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";

        [JsonPropertyName("reasonCode")]
        public string ReasonCode { get; set; } = "";

        [JsonPropertyName("duplicateOfId")]
        public int? DuplicateOfId { get; set; }

        [JsonPropertyName("distanceM")]
        public double? DistanceM { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DA FILTRAGEM
    // ══════════════════════════════════════════════════════════════

    public class RoomFilterResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("inputCount")]
        public int InputCount { get; set; }

        [JsonPropertyName("validCount")]
        public int ValidCount { get; set; }

        [JsonPropertyName("discardedCount")]
        public int DiscardedCount { get; set; }

        [JsonPropertyName("duplicateCount")]
        public int DuplicateCount { get; set; }

        [JsonPropertyName("validRooms")]
        public List<ValidRoom> ValidRooms { get; set; } = new();

        [JsonPropertyName("discardedEntries")]
        public List<FilterDiscardEntry> DiscardedEntries { get; set; } = new();

        [JsonPropertyName("discardReasonSummary")]
        public Dictionary<string, int> DiscardReasonSummary { get; set; } = new();

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, int> LevelSummary { get; set; } = new();

        [JsonPropertyName("totalValidAreaM2")]
        public double TotalValidAreaM2 { get; set; }

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public ValidRoom? GetById(int elementId) =>
            ValidRooms.FirstOrDefault(r => r.ElementId == elementId);

        public List<ValidRoom> GetByLevel(string levelName) =>
            ValidRooms.Where(r =>
                string.Equals(r.LevelName, levelName,
                    StringComparison.OrdinalIgnoreCase)).ToList();

        public List<ValidRoom> GetByMinArea(double minM2) =>
            ValidRooms.Where(r => r.AreaM2 >= minM2).ToList();

        public List<ValidRoom> GetByDepartment(string dept) =>
            ValidRooms.Where(r =>
                string.Equals(r.Department, dept,
                    StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: FILTRAGEM DE ROOMS INVÁLIDOS
    // ══════════════════════════════════════════════════════════════

    public interface IRoomFilterService
    {
        RoomFilterResult Filtrar(
            RoomCollectionResult coleta,
            RoomFilterConfig? config = null);

        RoomFilterResult Filtrar(
            List<ValidRoom> rooms,
            RoomFilterConfig? config = null);
    }

    public class RoomFilterService : IRoomFilterService
    {
        public event Action<string>? OnProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  FILTRAR A PARTIR DE RoomCollectionResult
        // ══════════════════════════════════════════════════════════

        public RoomFilterResult Filtrar(
            RoomCollectionResult coleta,
            RoomFilterConfig? config = null)
        {
            return Filtrar(coleta.ValidRooms, config);
        }

        // ══════════════════════════════════════════════════════════
        //  FILTRAR A PARTIR DE LISTA
        // ══════════════════════════════════════════════════════════

        public RoomFilterResult Filtrar(
            List<ValidRoom> rooms,
            RoomFilterConfig? config = null)
        {
            config ??= new RoomFilterConfig();

            var result = new RoomFilterResult
            {
                InputCount = rooms.Count
            };

            EmitProgress($"Filtrando {rooms.Count} Rooms...");

            // ── Fase 1: Filtros individuais ──
            var candidatos = new List<ValidRoom>();

            foreach (var room in rooms)
            {
                var discard = AvaliarRoom(room, config);

                if (discard != null)
                {
                    result.DiscardedEntries.Add(discard);
                    continue;
                }

                candidatos.Add(room);
            }

            EmitProgress($"Filtro individual: {candidatos.Count} aprovados, " +
                         $"{result.DiscardedEntries.Count} descartados");

            // ── Fase 2: Detecção de duplicatas ──
            var unicos = DetectarDuplicatas(
                candidatos, config, result);

            result.ValidRooms = unicos;

            // ── Fase 3: Warnings ──
            foreach (var room in result.ValidRooms)
            {
                if (room.AreaM2 > config.AreaMaxM2)
                {
                    result.Warnings.Add(
                        $"Room '{room.Name}' ({room.Number}) tem " +
                        $"área {room.AreaM2:F2}m² > {config.AreaMaxM2}m² " +
                        "(verificar se não é área externa)");
                }

                if (string.IsNullOrWhiteSpace(room.Number))
                {
                    result.Warnings.Add(
                        $"Room '{room.Name}' (Id:{room.ElementId}) " +
                        "sem número atribuído");
                }

                if (string.IsNullOrWhiteSpace(room.Department))
                {
                    result.Warnings.Add(
                        $"Room '{room.Name}' ({room.Number}) " +
                        "sem departamento definido");
                }
            }

            // ── Fase 4: Estatísticas ──
            result.ValidCount = result.ValidRooms.Count;
            result.DiscardedCount = result.DiscardedEntries.Count;
            result.DuplicateCount = result.DiscardedEntries
                .Count(e => e.ReasonCode == nameof(DiscardReason.Duplicado));
            result.TotalValidAreaM2 = Math.Round(
                result.ValidRooms.Sum(r => r.AreaM2), 2);
            result.Success = result.ValidCount > 0;

            // Resumo por motivo de descarte
            foreach (var group in result.DiscardedEntries
                         .GroupBy(e => e.ReasonCode))
            {
                result.DiscardReasonSummary[group.Key] = group.Count();
            }

            // Resumo por Level
            foreach (var group in result.ValidRooms
                         .GroupBy(r => r.LevelName))
            {
                result.LevelSummary[group.Key] = group.Count();
            }

            // ── Persistir ──
            PersistirResultado(result);

            EmitProgress($"Filtragem concluída: {result.ValidCount} válidos, " +
                         $"{result.DiscardedCount} descartados " +
                         $"({result.DuplicateCount} duplicatas), " +
                         $"{result.Warnings.Count} warnings");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  AVALIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        private static FilterDiscardEntry? AvaliarRoom(
            ValidRoom room,
            RoomFilterConfig config)
        {
            // 1 — Centroid (proxy para Location)
            if (room.Centroid == null || room.Centroid.Length < 3)
            {
                return CriarDescarte(room,
                    DiscardReason.SemLocation,
                    "Sem Location/Centroid definido");
            }

            // Verificar se centroid é origin (0,0,0) — possível placeholder
            if (room.Centroid[0] == 0 &&
                room.Centroid[1] == 0 &&
                room.Centroid[2] == 0 &&
                room.AreaM2 > 0)
            {
                // Centroid na origin com área > 0 → possivelmente um bug,
                // mas não descartamos — apenas warning tratado externamente
            }

            // 2 — Área zero
            if (room.AreaM2 <= 0)
            {
                return CriarDescarte(room,
                    DiscardReason.AreaZero,
                    "Área zero ou negativa");
            }

            // 3 — Área abaixo do mínimo
            if (room.AreaM2 < config.AreaMinM2)
            {
                return CriarDescarte(room,
                    DiscardReason.AreaAbaixoMinima,
                    $"Área {room.AreaM2:F4}m² < mínimo {config.AreaMinM2}m²");
            }

            // 4 — Nome
            if (config.RequireName &&
                string.IsNullOrWhiteSpace(room.Name))
            {
                return CriarDescarte(room,
                    DiscardReason.SemNome,
                    "Nome não definido");
            }

            // 5 — Nome excluído
            if (!string.IsNullOrWhiteSpace(room.Name) &&
                config.ExcludedNames.Contains(room.Name.Trim()))
            {
                return CriarDescarte(room,
                    DiscardReason.NomeExcluido,
                    $"Nome '{room.Name}' está na lista de exclusão");
            }

            // 6 — Número
            if (config.RequireNumber &&
                string.IsNullOrWhiteSpace(room.Number))
            {
                return CriarDescarte(room,
                    DiscardReason.SemNumero,
                    "Número não definido");
            }

            // 7 — IsEnclosed
            if (config.RequireEnclosed && !room.IsEnclosed)
            {
                return CriarDescarte(room,
                    DiscardReason.NaoEnclosed,
                    "Room não está fechado por boundaries");
            }

            // 8 — Perímetro
            if (config.RequirePerimeter && room.PerimeterM <= 0)
            {
                return CriarDescarte(room,
                    DiscardReason.SemPerimetro,
                    "Perímetro zero");
            }

            // 9 — Level
            if (string.IsNullOrWhiteSpace(room.LevelName))
            {
                return CriarDescarte(room,
                    DiscardReason.SemLevel,
                    "Sem Level atribuído");
            }

            // 10 — Level excluído
            if (config.ExcludedLevels.Contains(room.LevelName.Trim()))
            {
                return CriarDescarte(room,
                    DiscardReason.LevelExcluido,
                    $"Level '{room.LevelName}' está na lista de exclusão");
            }

            return null; // aprovado
        }

        // ══════════════════════════════════════════════════════════
        //  DETECÇÃO DE DUPLICATAS
        // ══════════════════════════════════════════════════════════

        private static List<ValidRoom> DetectarDuplicatas(
            List<ValidRoom> candidatos,
            RoomFilterConfig config,
            RoomFilterResult result)
        {
            var unicos = new List<ValidRoom>();
            var processados = new HashSet<int>();

            // Agrupar por Level — duplicatas só existem no mesmo nível
            var porLevel = candidatos
                .GroupBy(r => r.LevelName)
                .ToList();

            foreach (var levelGroup in porLevel)
            {
                var levelRooms = levelGroup.ToList();

                for (int i = 0; i < levelRooms.Count; i++)
                {
                    if (processados.Contains(levelRooms[i].ElementId))
                        continue;

                    var current = levelRooms[i];
                    var isDuplicate = false;

                    // Comparar com rooms já aceitos neste nível
                    for (int j = 0; j < i; j++)
                    {
                        if (processados.Contains(levelRooms[j].ElementId))
                            continue;

                        var other = levelRooms[j];

                        // Mesmo nome + centroid próximo = duplicata
                        if (SaoRoomsDuplicados(current, other, config))
                        {
                            isDuplicate = true;

                            // Manter o que tem maior área
                            var mantido = current.AreaM2 >= other.AreaM2
                                ? current : other;
                            var descartado = current.AreaM2 >= other.AreaM2
                                ? other : current;

                            var dist = DistanciaCentroid(current, other);

                            result.DiscardedEntries.Add(new FilterDiscardEntry
                            {
                                ElementId = descartado.ElementId,
                                Name = descartado.Name,
                                Number = descartado.Number,
                                LevelName = descartado.LevelName,
                                AreaM2 = descartado.AreaM2,
                                Reason = $"Duplicata de '{mantido.Name}' " +
                                         $"(Id:{mantido.ElementId}) — " +
                                         $"distância: {dist:F3}m",
                                ReasonCode = nameof(DiscardReason.Duplicado),
                                DuplicateOfId = mantido.ElementId,
                                DistanceM = Math.Round(dist, 4)
                            });

                            processados.Add(descartado.ElementId);

                            // Garantir que o mantido seja adicionado
                            if (!unicos.Any(u =>
                                    u.ElementId == mantido.ElementId))
                                unicos.Add(mantido);

                            processados.Add(mantido.ElementId);
                            break;
                        }
                    }

                    if (!isDuplicate &&
                        !processados.Contains(current.ElementId))
                    {
                        unicos.Add(current);
                        processados.Add(current.ElementId);
                    }
                }
            }

            return unicos;
        }

        /// Dois Rooms são considerados duplicados quando:
        /// - Estão no mesmo Level (garantido pelo agrupamento)
        /// - Têm o mesmo nome (case-insensitive)
        /// - Centroids estão a menos de [config.DuplicateDistanceM] metros
        private static bool SaoRoomsDuplicados(
            ValidRoom a, ValidRoom b, RoomFilterConfig config)
        {
            // Mesmo nome?
            if (!string.Equals(a.Name, b.Name,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            // Centroid próximo?
            var dist = DistanciaCentroid(a, b);
            return dist <= config.DuplicateDistanceM;
        }

        private static double DistanciaCentroid(ValidRoom a, ValidRoom b)
        {
            if (a.Centroid.Length < 3 || b.Centroid.Length < 3)
                return double.MaxValue;

            var dx = a.Centroid[0] - b.Centroid[0];
            var dy = a.Centroid[1] - b.Centroid[1];
            var dz = a.Centroid[2] - b.Centroid[2];

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private static FilterDiscardEntry CriarDescarte(
            ValidRoom room,
            DiscardReason reason,
            string message)
        {
            return new FilterDiscardEntry
            {
                ElementId = room.ElementId,
                Name = room.Name,
                Number = room.Number,
                LevelName = room.LevelName,
                AreaM2 = room.AreaM2,
                Reason = message,
                ReasonCode = reason.ToString()
            };
        }

        private void PersistirResultado(RoomFilterResult result)
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
                    $"rooms_filtered_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(result, JsonOpts);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                EmitProgress($"Resultado salvo: {filePath}");
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
