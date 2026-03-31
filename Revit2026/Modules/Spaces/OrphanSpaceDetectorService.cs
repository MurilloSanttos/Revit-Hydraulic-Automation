using System.Text.Json;
using System.Text.Json.Serialization;
using Revit2026.Modules.Rooms;

namespace Revit2026.Modules.Spaces
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO — SPACE ÓRFÃO
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Representa um Space MEP existente que não possui Room correspondente.
    /// Candidato a exclusão, auditoria ou associação manual.
    /// </summary>
    public class OrphanSpace
    {
        [JsonPropertyName("spaceId")]
        public long SpaceId { get; set; }

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
        public OrphanCentroid Centroid { get; set; } = new();

        [JsonPropertyName("spaceType")]
        public string SpaceType { get; set; } = "";

        [JsonPropertyName("conditionType")]
        public string ConditionType { get; set; } = "";

        [JsonPropertyName("department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("phase")]
        public string Phase { get; set; } = "";

        [JsonPropertyName("isEnclosed")]
        public bool IsEnclosed { get; set; }

        [JsonPropertyName("orphanReason")]
        public string OrphanReason { get; set; } = "";

        [JsonPropertyName("orphanReasonCode")]
        public string OrphanReasonCode { get; set; } = "";

        [JsonPropertyName("nearestRoomId")]
        public long? NearestRoomId { get; set; }

        [JsonPropertyName("nearestRoomName")]
        public string NearestRoomName { get; set; } = "";

        [JsonPropertyName("nearestRoomDistanceM")]
        public double? NearestRoomDistanceM { get; set; }

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = "";

        [JsonPropertyName("recommendationEnum")]
        [JsonIgnore]
        public SpaceOrphanAction RecommendationEnum { get; set; } =
            SpaceOrphanAction.Revisar;
    }

    // ══════════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════════

    public enum SpaceOrphanReason
    {
        /// Nenhum Room no mesmo Level
        SemRoomNoLevel,

        /// Rooms existem no Level mas todos já estão pareados
        RoomsEsgotados,

        /// Room mais próximo está além da distância aceitável
        DistanciaExcedida,

        /// Space sem centroid válido
        CentroidInvalido,

        /// Correspondência anterior não encontrou match
        SemCorrespondencia
    }

    public enum SpaceOrphanAction
    {
        /// Excluir — Space sem função, área zero ou dados inválidos
        Excluir,

        /// Revisar — verificação manual necessária
        Revisar,

        /// Manter — Space com dados válidos, pode ter uso futuro
        Manter,

        /// Associar — Room próximo detectado, possível associação manual
        AssociarManualmente
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class OrphanSpaceDetectionConfig
    {
        /// <summary>
        /// Percentual máximo de Spaces órfãos antes de emitir alerta.
        /// </summary>
        [JsonPropertyName("orphanAlertThreshold")]
        public double OrphanAlertThreshold { get; set; } = 0.40;

        /// <summary>
        /// Quantidade absoluta máxima de órfãos antes de alertar.
        /// </summary>
        [JsonPropertyName("orphanAlertAbsolute")]
        public int OrphanAlertAbsolute { get; set; } = 15;

        /// <summary>
        /// Área mínima (m²) abaixo da qual o Space é recomendado para exclusão.
        /// </summary>
        [JsonPropertyName("deleteThresholdAreaM2")]
        public double DeleteThresholdAreaM2 { get; set; } = 0.50;

        /// <summary>
        /// Distância (m) até o Room mais próximo para recomendar
        /// associação manual.
        /// </summary>
        [JsonPropertyName("manualAssociationDistanceM")]
        public double ManualAssociationDistanceM { get; set; } = 3.0;

        /// <summary>
        /// Se true, inclui informação do Room mais próximo para referência.
        /// </summary>
        [JsonPropertyName("includeNearestRoom")]
        public bool IncludeNearestRoom { get; set; } = true;
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DA DETECÇÃO
    // ══════════════════════════════════════════════════════════════

    public class OrphanSpaceDetectionResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalSpacesProcessed")]
        public int TotalSpacesProcessed { get; set; }

        [JsonPropertyName("orphanCount")]
        public int OrphanCount { get; set; }

        [JsonPropertyName("pairedCount")]
        public int PairedCount { get; set; }

        [JsonPropertyName("orphanPercentage")]
        public double OrphanPercentage { get; set; }

        [JsonPropertyName("alertTriggered")]
        public bool AlertTriggered { get; set; }

        [JsonPropertyName("alertMessage")]
        public string AlertMessage { get; set; } = "";

        [JsonPropertyName("orphanSpaces")]
        public List<OrphanSpace> OrphanSpaces { get; set; } = new();

        [JsonPropertyName("recommendationSummary")]
        public Dictionary<string, int> RecommendationSummary { get; set; } = new();

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, OrphanSpaceLevelSummary> LevelSummary { get; set; } = new();

        [JsonPropertyName("reasonSummary")]
        public Dictionary<string, int> ReasonSummary { get; set; } = new();

        [JsonPropertyName("totalOrphanAreaM2")]
        public double TotalOrphanAreaM2 { get; set; }

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public List<OrphanSpace> GetByLevel(string levelName) =>
            OrphanSpaces.Where(o =>
                string.Equals(o.LevelName, levelName,
                    StringComparison.OrdinalIgnoreCase)).ToList();

        public List<OrphanSpace> GetByRecommendation(SpaceOrphanAction action) =>
            OrphanSpaces.Where(o =>
                o.RecommendationEnum == action).ToList();

        public List<OrphanSpace> GetForDeletion() =>
            GetByRecommendation(SpaceOrphanAction.Excluir);

        public List<OrphanSpace> GetForManualAssociation() =>
            GetByRecommendation(SpaceOrphanAction.AssociarManualmente);

        public OrphanSpace? GetBySpaceId(long spaceId) =>
            OrphanSpaces.FirstOrDefault(o => o.SpaceId == spaceId);
    }

    public class OrphanSpaceLevelSummary
    {
        [JsonPropertyName("totalOrphans")]
        public int TotalOrphans { get; set; }

        [JsonPropertyName("forDeletion")]
        public int ForDeletion { get; set; }

        [JsonPropertyName("forReview")]
        public int ForReview { get; set; }

        [JsonPropertyName("forKeep")]
        public int ForKeep { get; set; }

        [JsonPropertyName("forManualAssociation")]
        public int ForManualAssociation { get; set; }

        [JsonPropertyName("totalAreaM2")]
        public double TotalAreaM2 { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: DETECÇÃO DE SPACES ÓRFÃOS
    // ══════════════════════════════════════════════════════════════

    public interface IOrphanSpaceDetectorService
    {
        /// <summary>
        /// Detecta Spaces órfãos a partir do resultado de matching.
        /// </summary>
        OrphanSpaceDetectionResult Detectar(
            List<ValidSpace> spaces,
            RoomSpaceMatchResult matchResult,
            List<ValidRoom> rooms,
            OrphanSpaceDetectionConfig? config = null);

        /// <summary>
        /// Detecta Spaces órfãos executando matching interno.
        /// </summary>
        OrphanSpaceDetectionResult Detectar(
            List<ValidSpace> spaces,
            List<ValidRoom> rooms,
            OrphanSpaceDetectionConfig? config = null);
    }

    public class OrphanSpaceDetectorService : IOrphanSpaceDetectorService
    {
        public event Action<string>? OnProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  DETECTAR COM MATCHING AUTOMÁTICO
        // ══════════════════════════════════════════════════════════

        public OrphanSpaceDetectionResult Detectar(
            List<ValidSpace> spaces,
            List<ValidRoom> rooms,
            OrphanSpaceDetectionConfig? config = null)
        {
            EmitProgress("Executando matching Room↔Space antes da detecção...");

            var matcher = new RoomSpaceMatcherService();
            var matchResult = matcher.Correlacionar(rooms, spaces);

            return Detectar(spaces, matchResult, rooms, config);
        }

        // ══════════════════════════════════════════════════════════
        //  DETECÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        public OrphanSpaceDetectionResult Detectar(
            List<ValidSpace> spaces,
            RoomSpaceMatchResult matchResult,
            List<ValidRoom> rooms,
            OrphanSpaceDetectionConfig? config = null)
        {
            config ??= new OrphanSpaceDetectionConfig();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = new OrphanSpaceDetectionResult
            {
                TotalSpacesProcessed = spaces.Count
            };

            EmitProgress($"Detectando Spaces órfãos em {spaces.Count} Spaces...");

            // ── 1. Indexar Spaces pareados ────────────────────────
            var spacesPareados = matchResult.Matches
                .Where(m => m.StatusEnum == MatchStatus.Matched &&
                            m.SpaceId.HasValue)
                .Select(m => m.SpaceId!.Value)
                .ToHashSet();

            result.PairedCount = spacesPareados.Count;

            EmitProgress($"{spacesPareados.Count} Spaces já pareados. " +
                         "Processando restantes...");

            // ── 2. Processar cada Space ───────────────────────────
            foreach (var space in spaces)
            {
                if (spacesPareados.Contains(space.ElementId))
                    continue;

                var orphan = CriarOrphanSpace(
                    space, matchResult, rooms, config);

                result.OrphanSpaces.Add(orphan);
            }

            // ── 3. Estatísticas ───────────────────────────────────
            result.OrphanCount = result.OrphanSpaces.Count;
            result.OrphanPercentage = spaces.Count > 0
                ? Math.Round(
                    (double)result.OrphanCount / spaces.Count * 100, 2)
                : 0;
            result.TotalOrphanAreaM2 = Math.Round(
                result.OrphanSpaces.Sum(o => o.AreaM2), 2);
            result.Success = true;

            // ── 4. Resumo por recomendação ────────────────────────
            foreach (var group in result.OrphanSpaces
                         .GroupBy(o => o.Recommendation))
            {
                result.RecommendationSummary[group.Key] = group.Count();
            }

            // ── 5. Resumo por Level ──────────────────────────────
            GerarResumoLevel(result);

            // ── 6. Resumo por motivo ─────────────────────────────
            foreach (var group in result.OrphanSpaces
                         .GroupBy(o => o.OrphanReasonCode))
            {
                result.ReasonSummary[group.Key] = group.Count();
            }

            // ── 7. Verificar alerta ──────────────────────────────
            VerificarAlerta(result, config);

            // ── 8. Finalizar ─────────────────────────────────────
            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            // ── 9. Persistir ─────────────────────────────────────
            PersistirResultado(result);

            var deleteCount = result.OrphanSpaces
                .Count(o => o.RecommendationEnum == SpaceOrphanAction.Excluir);
            var manualCount = result.OrphanSpaces
                .Count(o => o.RecommendationEnum == SpaceOrphanAction.AssociarManualmente);

            EmitProgress(
                $"Detecção concluída ({result.ExecutionTimeMs}ms): " +
                $"{result.OrphanCount} Spaces órfãos de " +
                $"{result.TotalSpacesProcessed} ({result.OrphanPercentage:F1}%) | " +
                $"Excluir: {deleteCount}, Associar: {manualCount}, " +
                $"Área: {result.TotalOrphanAreaM2:F2}m²" +
                (result.AlertTriggered
                    ? $" | ⚠ ALERTA: {result.AlertMessage}"
                    : ""));

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR ORPHAN SPACE
        // ══════════════════════════════════════════════════════════

        private static OrphanSpace CriarOrphanSpace(
            ValidSpace space,
            RoomSpaceMatchResult matchResult,
            List<ValidRoom> rooms,
            OrphanSpaceDetectionConfig config)
        {
            var orphan = new OrphanSpace
            {
                SpaceId = space.ElementId,
                Name = space.Name,
                Number = space.Number,
                LevelId = space.LevelId,
                LevelName = space.LevelName,
                LevelElevationM = space.LevelElevationM,
                AreaM2 = space.AreaM2,
                PerimeterM = space.PerimeterM,
                VolumeM3 = space.VolumeM3,
                SpaceType = space.SpaceType,
                ConditionType = space.ConditionType,
                Department = space.Department,
                Phase = space.Phase,
                IsEnclosed = space.IsEnclosed
            };

            // ── Centroid ─────────────────────────────────────────
            if (space.Centroid != null && space.Centroid.Length >= 3)
            {
                orphan.Centroid = new OrphanCentroid(space.Centroid);
            }

            // ── Determinar motivo ────────────────────────────────
            DeterminarMotivo(orphan, space, rooms);

            // ── Room mais próximo (referência) ───────────────────
            if (config.IncludeNearestRoom)
            {
                EncontrarRoomMaisProximo(orphan, space, rooms);
            }

            // ── Determinar recomendação ──────────────────────────
            DeterminarRecomendacao(orphan, config);

            return orphan;
        }

        // ══════════════════════════════════════════════════════════
        //  DETERMINAR MOTIVO DO ORPHAN
        // ══════════════════════════════════════════════════════════

        private static void DeterminarMotivo(
            OrphanSpace orphan,
            ValidSpace space,
            List<ValidRoom> rooms)
        {
            // Centroid inválido
            if (space.Centroid == null || space.Centroid.Length < 3)
            {
                orphan.OrphanReason =
                    "Space sem centroid válido para matching";
                orphan.OrphanReasonCode =
                    nameof(SpaceOrphanReason.CentroidInvalido);
                return;
            }

            // Sem Rooms no mesmo Level
            var roomsNoLevel = rooms
                .Where(r => r.LevelId == space.LevelId)
                .ToList();

            if (roomsNoLevel.Count == 0)
            {
                orphan.OrphanReason =
                    $"Nenhum Room no Level '{space.LevelName}'";
                orphan.OrphanReasonCode =
                    nameof(SpaceOrphanReason.SemRoomNoLevel);
                return;
            }

            // Todos os Rooms do Level já estão pareados
            var roomsComSpace = rooms
                .Where(r => r.LevelId == space.LevelId)
                .Select(r => r.ElementId)
                .ToHashSet();

            // Se há Rooms não pareados mas nenhum foi associado a este Space
            if (roomsNoLevel.Count > 0)
            {
                // Verificar distância ao Room mais próximo
                double minDist = double.MaxValue;
                foreach (var room in roomsNoLevel)
                {
                    if (room.Centroid == null || room.Centroid.Length < 3)
                        continue;

                    var dist = DistanciaEuclidiana(
                        space.Centroid, room.Centroid);
                    if (dist < minDist)
                        minDist = dist;
                }

                if (minDist > 5.0) // Threshold padrão do matcher
                {
                    orphan.OrphanReason =
                        $"Room mais próximo a {minDist:F3}m excede " +
                        "distância máxima de matching";
                    orphan.OrphanReasonCode =
                        nameof(SpaceOrphanReason.DistanciaExcedida);
                    return;
                }

                orphan.OrphanReason =
                    $"Rooms disponíveis no Level '{space.LevelName}' " +
                    "já estão pareados com outros Spaces";
                orphan.OrphanReasonCode =
                    nameof(SpaceOrphanReason.RoomsEsgotados);
                return;
            }

            // Fallback
            orphan.OrphanReason = "Sem correspondência Space↔Room";
            orphan.OrphanReasonCode =
                nameof(SpaceOrphanReason.SemCorrespondencia);
        }

        // ══════════════════════════════════════════════════════════
        //  ENCONTRAR ROOM MAIS PRÓXIMO
        // ══════════════════════════════════════════════════════════

        private static void EncontrarRoomMaisProximo(
            OrphanSpace orphan, ValidSpace space, List<ValidRoom> rooms)
        {
            if (space.Centroid == null || space.Centroid.Length < 3)
                return;

            if (rooms.Count == 0)
                return;

            ValidRoom? melhor = null;
            double melhorDist = double.MaxValue;

            foreach (var room in rooms)
            {
                if (room.Centroid == null || room.Centroid.Length < 3)
                    continue;

                var dist = DistanciaEuclidiana(
                    space.Centroid, room.Centroid);

                if (dist < melhorDist)
                {
                    melhorDist = dist;
                    melhor = room;
                }
            }

            if (melhor != null)
            {
                orphan.NearestRoomId = melhor.ElementId;
                orphan.NearestRoomName = melhor.Name;
                orphan.NearestRoomDistanceM = Math.Round(melhorDist, 4);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  DETERMINAR RECOMENDAÇÃO
        // ══════════════════════════════════════════════════════════

        private static void DeterminarRecomendacao(
            OrphanSpace orphan, OrphanSpaceDetectionConfig config)
        {
            // EXCLUIR: área minúscula ou não enclosed sem centroid
            if (orphan.AreaM2 <= config.DeleteThresholdAreaM2 ||
                (!orphan.IsEnclosed && orphan.AreaM2 <= 0))
            {
                orphan.RecommendationEnum = SpaceOrphanAction.Excluir;
                orphan.Recommendation = "EXCLUIR";
                return;
            }

            // ASSOCIAR MANUALMENTE: Room próximo detectado
            if (orphan.NearestRoomDistanceM.HasValue &&
                orphan.NearestRoomDistanceM.Value <=
                    config.ManualAssociationDistanceM &&
                orphan.IsEnclosed)
            {
                orphan.RecommendationEnum = SpaceOrphanAction.AssociarManualmente;
                orphan.Recommendation = "ASSOCIAR_MANUALMENTE";
                return;
            }

            // MANTER: Space com dados completos, sem Room próximo
            if (orphan.IsEnclosed &&
                orphan.AreaM2 > config.DeleteThresholdAreaM2 &&
                !string.IsNullOrWhiteSpace(orphan.Department))
            {
                orphan.RecommendationEnum = SpaceOrphanAction.Manter;
                orphan.Recommendation = "MANTER";
                return;
            }

            // REVISAR: todos os demais
            orphan.RecommendationEnum = SpaceOrphanAction.Revisar;
            orphan.Recommendation = "REVISAR";
        }

        // ══════════════════════════════════════════════════════════
        //  DISTÂNCIA EUCLIDIANA
        // ══════════════════════════════════════════════════════════

        private static double DistanciaEuclidiana(double[] a, double[] b)
        {
            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            var dz = a[2] - b[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO POR LEVEL
        // ══════════════════════════════════════════════════════════

        private static void GerarResumoLevel(
            OrphanSpaceDetectionResult result)
        {
            var levelGroups = result.OrphanSpaces
                .GroupBy(o => o.LevelName)
                .ToList();

            foreach (var group in levelGroups)
            {
                var levelName = string.IsNullOrWhiteSpace(group.Key)
                    ? "(sem nível)"
                    : group.Key;

                result.LevelSummary[levelName] = new OrphanSpaceLevelSummary
                {
                    TotalOrphans = group.Count(),
                    ForDeletion = group.Count(o =>
                        o.RecommendationEnum == SpaceOrphanAction.Excluir),
                    ForReview = group.Count(o =>
                        o.RecommendationEnum == SpaceOrphanAction.Revisar),
                    ForKeep = group.Count(o =>
                        o.RecommendationEnum == SpaceOrphanAction.Manter),
                    ForManualAssociation = group.Count(o =>
                        o.RecommendationEnum ==
                            SpaceOrphanAction.AssociarManualmente),
                    TotalAreaM2 = Math.Round(
                        group.Sum(o => o.AreaM2), 2)
                };
            }
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAR ALERTA
        // ══════════════════════════════════════════════════════════

        private static void VerificarAlerta(
            OrphanSpaceDetectionResult result,
            OrphanSpaceDetectionConfig config)
        {
            var messages = new List<string>();

            // Alerta por percentual
            if (result.TotalSpacesProcessed > 0)
            {
                var pct = (double)result.OrphanCount /
                          result.TotalSpacesProcessed;

                if (pct > config.OrphanAlertThreshold)
                {
                    messages.Add(
                        $"{result.OrphanPercentage:F1}% de Spaces são órfãos " +
                        $"(limite: {config.OrphanAlertThreshold * 100:F0}%)");
                }
            }

            // Alerta por quantidade absoluta
            if (result.OrphanCount > config.OrphanAlertAbsolute)
            {
                messages.Add(
                    $"{result.OrphanCount} Spaces órfãos " +
                    $"(limite: {config.OrphanAlertAbsolute})");
            }

            // Alerta por candidatos a exclusão
            var forDeletion = result.OrphanSpaces
                .Count(o => o.RecommendationEnum == SpaceOrphanAction.Excluir);

            if (forDeletion > 0)
            {
                messages.Add(
                    $"{forDeletion} Spaces recomendados para exclusão");
            }

            if (messages.Count > 0)
            {
                result.AlertTriggered = true;
                result.AlertMessage = string.Join(" | ", messages);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PERSISTÊNCIA JSON
        // ══════════════════════════════════════════════════════════

        private void PersistirResultado(OrphanSpaceDetectionResult result)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Matching");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"orphan_spaces_{DateTime.Now:yyyyMMdd_HHmmss}.json";
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
