using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Revit2026.Modules.Rooms;

namespace Revit2026.Modules.Spaces
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO — RESULTADO DE CRIAÇÃO DE SPACE
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resultado individual da criação de um Space para um Room órfão.
    /// </summary>
    public class SpaceCreationEntry
    {
        [JsonPropertyName("roomId")]
        public long RoomId { get; set; }

        [JsonPropertyName("roomName")]
        public string RoomName { get; set; } = "";

        [JsonPropertyName("roomNumber")]
        public string RoomNumber { get; set; } = "";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("spaceId")]
        public long SpaceId { get; set; }

        [JsonPropertyName("spaceName")]
        public string SpaceName { get; set; } = "";

        [JsonPropertyName("areaM2")]
        public double AreaM2 { get; set; }

        [JsonPropertyName("centroid")]
        public OrphanCentroid Centroid { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("statusEnum")]
        [JsonIgnore]
        public SpaceCreationStatus StatusEnum { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    public enum SpaceCreationStatus
    {
        /// Space criado com sucesso
        Criado,

        /// Falha na criação — dados do Room insuficientes
        FalhaDadosInsuficientes,

        /// Falha na criação — Level não encontrado
        FalhaLevelNaoEncontrado,

        /// Falha na criação — Revit retornou null
        FalhaRevitNull,

        /// Falha na criação — exceção durante criação
        FalhaExcecao,

        /// Criação ignorada — já existe Space correspondente
        Ignorado
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO DE CRIAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class AutoSpaceCreationConfig
    {
        /// <summary>
        /// Se true, copia parâmetros (Name, Number, Department) do Room.
        /// </summary>
        [JsonPropertyName("copyRoomParameters")]
        public bool CopyRoomParameters { get; set; } = true;

        /// <summary>
        /// Se true, tenta atribuir Department ao Space.
        /// </summary>
        [JsonPropertyName("copyDepartment")]
        public bool CopyDepartment { get; set; } = true;

        /// <summary>
        /// Se true, apenas processa Rooms com prioridade ALTA e NORMAL.
        /// Se false, processa todos os órfãos.
        /// </summary>
        [JsonPropertyName("respectPriority")]
        public bool RespectPriority { get; set; } = false;

        /// <summary>
        /// Máximo de Spaces a criar por execução (0 = sem limite).
        /// Segurança para modelos muito grandes.
        /// </summary>
        [JsonPropertyName("maxSpacesPerRun")]
        public int MaxSpacesPerRun { get; set; } = 0;

        /// <summary>
        /// Nome da Transaction no Revit.
        /// </summary>
        [JsonPropertyName("transactionName")]
        public string TransactionName { get; set; } = "Criar Spaces Automáticos";

        /// <summary>
        /// Se true, agrupa todas as criações em uma única Transaction.
        /// Se false, cria uma Transaction por Space (mais seguro, mais lento).
        /// </summary>
        [JsonPropertyName("singleTransaction")]
        public bool SingleTransaction { get; set; } = true;
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO CONSOLIDADO
    // ══════════════════════════════════════════════════════════════

    public class AutoSpaceCreationResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalOrphans")]
        public int TotalOrphans { get; set; }

        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }

        [JsonPropertyName("totalCreated")]
        public int TotalCreated { get; set; }

        [JsonPropertyName("totalFailed")]
        public int TotalFailed { get; set; }

        [JsonPropertyName("totalIgnored")]
        public int TotalIgnored { get; set; }

        [JsonPropertyName("createdAreaM2")]
        public double CreatedAreaM2 { get; set; }

        [JsonPropertyName("entries")]
        public List<SpaceCreationEntry> Entries { get; set; } = new();

        [JsonPropertyName("levelSummary")]
        public Dictionary<string, AutoSpaceLevelSummary> LevelSummary { get; set; } = new();

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ── Accessors ──

        public List<SpaceCreationEntry> GetCreated() =>
            Entries.Where(e => e.StatusEnum == SpaceCreationStatus.Criado).ToList();

        public List<SpaceCreationEntry> GetFailed() =>
            Entries.Where(e =>
                e.StatusEnum != SpaceCreationStatus.Criado &&
                e.StatusEnum != SpaceCreationStatus.Ignorado).ToList();

        public SpaceCreationEntry? GetByRoomId(long roomId) =>
            Entries.FirstOrDefault(e => e.RoomId == roomId);

        public SpaceCreationEntry? GetBySpaceId(long spaceId) =>
            Entries.FirstOrDefault(e => e.SpaceId == spaceId);
    }

    public class AutoSpaceLevelSummary
    {
        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("totalAreaM2")]
        public double TotalAreaM2 { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO: CRIAÇÃO AUTOMÁTICA DE SPACES
    // ══════════════════════════════════════════════════════════════

    public interface IAutoSpaceCreatorService
    {
        /// <summary>
        /// Cria Spaces MEP para todos os Rooms da lista de órfãos.
        /// DEVE ser chamado dentro de um contexto com acesso ao Document.
        /// A Transaction é gerenciada internamente.
        /// </summary>
        AutoSpaceCreationResult CriarSpacesParaOrfaos(
            Document doc,
            OrphanDetectionResult orphanResult,
            AutoSpaceCreationConfig? config = null);

        /// <summary>
        /// Cria Spaces MEP a partir de lista de OrphanRoom.
        /// DEVE ser chamado dentro de uma Transaction ativa.
        /// </summary>
        AutoSpaceCreationResult CriarSpaces(
            Document doc,
            List<OrphanRoom> orphanRooms,
            AutoSpaceCreationConfig? config = null);

        /// <summary>
        /// Cria Spaces a partir de ValidRooms diretamente.
        /// Executa matching, detecção e criação em pipeline.
        /// </summary>
        AutoSpaceCreationResult CriarSpacesPipeline(
            Document doc,
            List<ValidRoom> rooms,
            List<ValidSpace> spacesExistentes,
            AutoSpaceCreationConfig? config = null);
    }

    public class AutoSpaceCreatorService : IAutoSpaceCreatorService
    {
        public event Action<string>? OnProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ══════════════════════════════════════════════════════════
        //  PIPELINE COMPLETO
        // ══════════════════════════════════════════════════════════

        public AutoSpaceCreationResult CriarSpacesPipeline(
            Document doc,
            List<ValidRoom> rooms,
            List<ValidSpace> spacesExistentes,
            AutoSpaceCreationConfig? config = null)
        {
            EmitProgress("Executando pipeline: Matching → Detecção → Criação...");

            // 1. Matching
            var matcher = new RoomSpaceMatcherService();
            var matchResult = matcher.Correlacionar(rooms, spacesExistentes);

            // 2. Detecção de órfãos
            var detector = new OrphanRoomDetectorService();
            var orphanResult = detector.Detectar(
                rooms, matchResult, spacesExistentes);

            if (orphanResult.OrphanCount == 0)
            {
                EmitProgress("Nenhum Room órfão detectado. Nada a criar.");
                return new AutoSpaceCreationResult
                {
                    Success = true,
                    TotalOrphans = 0
                };
            }

            // 3. Criação
            return CriarSpacesParaOrfaos(doc, orphanResult, config);
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR A PARTIR DE OrphanDetectionResult
        // ══════════════════════════════════════════════════════════

        public AutoSpaceCreationResult CriarSpacesParaOrfaos(
            Document doc,
            OrphanDetectionResult orphanResult,
            AutoSpaceCreationConfig? config = null)
        {
            config ??= new AutoSpaceCreationConfig();

            var orphanRooms = orphanResult.OrphanRooms;

            // Filtrar por prioridade se configurado
            if (config.RespectPriority)
            {
                orphanRooms = orphanRooms
                    .Where(o => o.PriorityEnum == OrphanPriority.Alta ||
                                o.PriorityEnum == OrphanPriority.Normal)
                    .ToList();

                EmitProgress(
                    $"Filtro de prioridade: {orphanRooms.Count} de " +
                    $"{orphanResult.OrphanCount} (ALTA + NORMAL).");
            }

            if (config.SingleTransaction)
            {
                return CriarComTransacaoUnica(doc, orphanRooms, config);
            }
            else
            {
                return CriarComTransacoesIndividuais(doc, orphanRooms, config);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR COM TRANSACTION ÚNICA
        // ══════════════════════════════════════════════════════════

        private AutoSpaceCreationResult CriarComTransacaoUnica(
            Document doc,
            List<OrphanRoom> orphanRooms,
            AutoSpaceCreationConfig config)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new AutoSpaceCreationResult
            {
                TotalOrphans = orphanRooms.Count
            };

            EmitProgress($"Criando {orphanRooms.Count} Spaces em Transaction única...");

            using var trans = new Transaction(doc, config.TransactionName);

            try
            {
                trans.Start();

                result = ProcessarCriacoes(doc, orphanRooms, config, result);

                if (result.TotalCreated > 0)
                {
                    trans.Commit();
                    EmitProgress($"Transaction committed: {result.TotalCreated} Spaces.");
                }
                else
                {
                    trans.RollBack();
                    EmitProgress("Nenhum Space criado — Transaction cancelada.");
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                EmitProgress($"Transaction falhou: {ex.Message}");
                result.Success = false;
            }

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            GerarResumoLevel(result);
            PersistirResultado(result);
            LogFinal(result);

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR COM TRANSACTIONS INDIVIDUAIS
        // ══════════════════════════════════════════════════════════

        private AutoSpaceCreationResult CriarComTransacoesIndividuais(
            Document doc,
            List<OrphanRoom> orphanRooms,
            AutoSpaceCreationConfig config)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new AutoSpaceCreationResult
            {
                TotalOrphans = orphanRooms.Count
            };

            var levelCache = CacheLevels(doc);
            var limit = config.MaxSpacesPerRun > 0
                ? Math.Min(config.MaxSpacesPerRun, orphanRooms.Count)
                : orphanRooms.Count;

            EmitProgress($"Criando até {limit} Spaces com Transactions individuais...");

            for (int i = 0; i < limit; i++)
            {
                var orphan = orphanRooms[i];

                using var trans = new Transaction(doc,
                    $"{config.TransactionName} - {orphan.Name}");

                try
                {
                    trans.Start();

                    var entry = CriarSpaceIndividual(
                        doc, orphan, levelCache, config);

                    result.Entries.Add(entry);
                    result.TotalProcessed++;

                    if (entry.StatusEnum == SpaceCreationStatus.Criado)
                    {
                        result.TotalCreated++;
                        result.CreatedAreaM2 += entry.AreaM2;
                        trans.Commit();
                    }
                    else
                    {
                        result.TotalFailed++;
                        trans.RollBack();
                    }
                }
                catch (Exception ex)
                {
                    if (trans.HasStarted())
                        trans.RollBack();

                    result.Entries.Add(new SpaceCreationEntry
                    {
                        RoomId = orphan.RoomId,
                        RoomName = orphan.Name,
                        RoomNumber = orphan.Number,
                        LevelName = orphan.LevelName,
                        StatusEnum = SpaceCreationStatus.FalhaExcecao,
                        Status = "FALHA_EXCECAO",
                        ErrorMessage = ex.Message
                    });
                    result.TotalFailed++;
                    result.TotalProcessed++;
                }
            }

            result.Success = result.TotalCreated > 0;

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            GerarResumoLevel(result);
            PersistirResultado(result);
            LogFinal(result);

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR SPACES (SEM GERENCIAMENTO DE TRANSACTION)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Spaces dentro de uma Transaction já ativa.
        /// </summary>
        public AutoSpaceCreationResult CriarSpaces(
            Document doc,
            List<OrphanRoom> orphanRooms,
            AutoSpaceCreationConfig? config = null)
        {
            config ??= new AutoSpaceCreationConfig();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = new AutoSpaceCreationResult
            {
                TotalOrphans = orphanRooms.Count
            };

            result = ProcessarCriacoes(doc, orphanRooms, config, result);

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            GerarResumoLevel(result);
            PersistirResultado(result);
            LogFinal(result);

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  PROCESSAMENTO INTERNO
        // ══════════════════════════════════════════════════════════

        private AutoSpaceCreationResult ProcessarCriacoes(
            Document doc,
            List<OrphanRoom> orphanRooms,
            AutoSpaceCreationConfig config,
            AutoSpaceCreationResult result)
        {
            var levelCache = CacheLevels(doc);
            var limit = config.MaxSpacesPerRun > 0
                ? Math.Min(config.MaxSpacesPerRun, orphanRooms.Count)
                : orphanRooms.Count;

            for (int i = 0; i < limit; i++)
            {
                var orphan = orphanRooms[i];

                try
                {
                    var entry = CriarSpaceIndividual(
                        doc, orphan, levelCache, config);

                    result.Entries.Add(entry);
                    result.TotalProcessed++;

                    if (entry.StatusEnum == SpaceCreationStatus.Criado)
                    {
                        result.TotalCreated++;
                        result.CreatedAreaM2 += entry.AreaM2;
                    }
                    else
                    {
                        result.TotalFailed++;
                    }
                }
                catch (Exception ex)
                {
                    result.Entries.Add(new SpaceCreationEntry
                    {
                        RoomId = orphan.RoomId,
                        RoomName = orphan.Name,
                        RoomNumber = orphan.Number,
                        LevelName = orphan.LevelName,
                        StatusEnum = SpaceCreationStatus.FalhaExcecao,
                        Status = "FALHA_EXCECAO",
                        ErrorMessage = ex.Message
                    });
                    result.TotalFailed++;
                    result.TotalProcessed++;
                }

                // Progresso a cada 10 ou no último
                if ((i + 1) % 10 == 0 || i == limit - 1)
                {
                    EmitProgress(
                        $"Progresso: {i + 1}/{limit} processados, " +
                        $"{result.TotalCreated} criados.");
                }
            }

            if (limit < orphanRooms.Count)
            {
                result.TotalIgnored = orphanRooms.Count - limit;
                EmitProgress(
                    $"{result.TotalIgnored} Rooms ignorados (limite " +
                    $"MaxSpacesPerRun={config.MaxSpacesPerRun}).");
            }

            result.Success = result.TotalCreated > 0;
            result.CreatedAreaM2 = Math.Round(result.CreatedAreaM2, 2);

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL DE SPACE
        // ══════════════════════════════════════════════════════════

        private SpaceCreationEntry CriarSpaceIndividual(
            Document doc,
            OrphanRoom orphan,
            Dictionary<string, Level> levelCache,
            AutoSpaceCreationConfig config)
        {
            var entry = new SpaceCreationEntry
            {
                RoomId = orphan.RoomId,
                RoomName = orphan.Name,
                RoomNumber = orphan.Number,
                LevelName = orphan.LevelName,
                Centroid = orphan.Centroid
            };

            // ── 1. Validar dados mínimos ─────────────────────────
            if (orphan.Centroid.X == 0 &&
                orphan.Centroid.Y == 0 &&
                orphan.Centroid.Z == 0 &&
                orphan.AreaM2 <= 0)
            {
                entry.StatusEnum = SpaceCreationStatus.FalhaDadosInsuficientes;
                entry.Status = "FALHA_DADOS_INSUFICIENTES";
                entry.ErrorMessage = "Room sem centroid válido e área zero";
                return entry;
            }

            // ── 2. Localizar Level ───────────────────────────────
            if (!levelCache.TryGetValue(orphan.LevelName, out var level))
            {
                // Fallback: buscar por LevelId
                level = doc.GetElement(new ElementId(orphan.LevelId)) as Level;

                if (level == null)
                {
                    entry.StatusEnum = SpaceCreationStatus.FalhaLevelNaoEncontrado;
                    entry.Status = "FALHA_LEVEL_NAO_ENCONTRADO";
                    entry.ErrorMessage =
                        $"Level '{orphan.LevelName}' (Id={orphan.LevelId}) " +
                        "não encontrado no modelo";
                    return entry;
                }
            }

            // ── 3. Converter centroid para unidades internas ─────
            var pontoUV = new UV(
                UnitUtils.ConvertToInternalUnits(
                    orphan.Centroid.X, UnitTypeId.Meters),
                UnitUtils.ConvertToInternalUnits(
                    orphan.Centroid.Y, UnitTypeId.Meters)
            );

            // ── 4. Criar Space via API ───────────────────────────
            Space? space;
            try
            {
                space = doc.Create.NewSpace(level, pontoUV);
            }
            catch (Exception ex)
            {
                entry.StatusEnum = SpaceCreationStatus.FalhaExcecao;
                entry.Status = "FALHA_EXCECAO";
                entry.ErrorMessage =
                    $"Revit API Exception: {ex.Message}";
                return entry;
            }

            if (space == null)
            {
                entry.StatusEnum = SpaceCreationStatus.FalhaRevitNull;
                entry.Status = "FALHA_REVIT_NULL";
                entry.ErrorMessage =
                    "doc.Create.NewSpace retornou null";
                return entry;
            }

            // ── 5. Atribuir parâmetros ───────────────────────────
            if (config.CopyRoomParameters)
            {
                AtribuirParametros(space, orphan, config);
            }

            // ── 6. Registrar sucesso ─────────────────────────────
            entry.SpaceId = space.Id.Value;
            entry.SpaceName = space.Name;
            entry.StatusEnum = SpaceCreationStatus.Criado;
            entry.Status = "CRIADO";

            // Ler área real do Space criado
            try
            {
                entry.AreaM2 = Math.Round(
                    UnitUtils.ConvertFromInternalUnits(
                        space.Area, UnitTypeId.SquareMeters), 4);
            }
            catch
            {
                entry.AreaM2 = orphan.AreaM2;
            }

            return entry;
        }

        // ══════════════════════════════════════════════════════════
        //  ATRIBUIÇÃO DE PARÂMETROS
        // ══════════════════════════════════════════════════════════

        private static void AtribuirParametros(
            Space space,
            OrphanRoom orphan,
            AutoSpaceCreationConfig config)
        {
            // Nome
            try
            {
                if (!string.IsNullOrWhiteSpace(orphan.Name))
                    space.Name = orphan.Name;
            }
            catch { /* parâmetro read-only em alguns templates */ }

            // Número
            try
            {
                if (!string.IsNullOrWhiteSpace(orphan.Number))
                    space.Number = orphan.Number;
            }
            catch { /* parâmetro read-only em alguns templates */ }

            // Department
            if (config.CopyDepartment &&
                !string.IsNullOrWhiteSpace(orphan.Department))
            {
                try
                {
                    var deptParam = space.get_Parameter(
                        BuiltInParameter.ROOM_DEPARTMENT);

                    if (deptParam != null && !deptParam.IsReadOnly)
                    {
                        deptParam.Set(orphan.Department);
                    }
                }
                catch { /* department não obrigatório */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CACHE DE LEVELS
        // ══════════════════════════════════════════════════════════

        private static Dictionary<string, Level> CacheLevels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToDictionary(
                    l => l.Name ?? "",
                    l => l,
                    StringComparer.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO POR LEVEL
        // ══════════════════════════════════════════════════════════

        private static void GerarResumoLevel(AutoSpaceCreationResult result)
        {
            var levelGroups = result.Entries
                .GroupBy(e => e.LevelName)
                .ToList();

            foreach (var group in levelGroups)
            {
                var levelName = string.IsNullOrWhiteSpace(group.Key)
                    ? "(sem nível)"
                    : group.Key;

                result.LevelSummary[levelName] = new AutoSpaceLevelSummary
                {
                    Created = group.Count(e =>
                        e.StatusEnum == SpaceCreationStatus.Criado),
                    Failed = group.Count(e =>
                        e.StatusEnum != SpaceCreationStatus.Criado),
                    TotalAreaM2 = Math.Round(
                        group.Where(e =>
                            e.StatusEnum == SpaceCreationStatus.Criado)
                            .Sum(e => e.AreaM2), 2)
                };
            }
        }

        // ══════════════════════════════════════════════════════════
        //  LOG FINAL
        // ══════════════════════════════════════════════════════════

        private void LogFinal(AutoSpaceCreationResult result)
        {
            EmitProgress(
                $"Criação concluída ({result.ExecutionTimeMs}ms): " +
                $"{result.TotalCreated}/{result.TotalOrphans} Spaces criados | " +
                $"{result.TotalFailed} falhas, " +
                $"{result.TotalIgnored} ignorados | " +
                $"Área: {result.CreatedAreaM2:F2}m²");

            // Log de falhas individuais
            foreach (var entry in result.Entries
                         .Where(e => e.StatusEnum != SpaceCreationStatus.Criado))
            {
                EmitProgress(
                    $"  ✗ Room '{entry.RoomName}' ({entry.RoomId}): " +
                    $"{entry.Status} — {entry.ErrorMessage}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PERSISTÊNCIA JSON
        // ══════════════════════════════════════════════════════════

        private void PersistirResultado(AutoSpaceCreationResult result)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "SpaceCreation");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"auto_spaces_{DateTime.Now:yyyyMMdd_HHmmss}.json";
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
