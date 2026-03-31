using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Revit2026.Modules.DynamoIntegration.Logging;
using Revit2026.Modules.Orchestration;

namespace Revit2026.Modules.UnMEP
{
    // ══════════════════════════════════════════════════════════════
    //  CONTRATOS DE CONFIGURAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class ColdWaterRoutingConfig
    {
        [JsonPropertyName("sistema")]
        public string Sistema { get; set; } = "Água Fria";

        [JsonPropertyName("diametroPadraoMm")]
        public double DiametroPadraoMm { get; set; } = 25;

        [JsonPropertyName("alturaRamalM")]
        public double AlturaRamalM { get; set; } = 0.60;

        [JsonPropertyName("alturaColunaM")]
        public double AlturaColunaM { get; set; } = 2.80;

        [JsonPropertyName("offsetParedeM")]
        public double OffsetParedeM { get; set; } = 0.05;

        [JsonPropertyName("offsetMinimoEntreEixosM")]
        public double OffsetMinimoEntreEixosM { get; set; } = 0.30;

        [JsonPropertyName("slopePercent")]
        public double SlopePercent { get; set; } = 0;

        [JsonPropertyName("connectToExisting")]
        public bool ConnectToExisting { get; set; } = true;

        [JsonPropertyName("toleranceMm")]
        public double ToleranceMm { get; set; } = 5;

        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 2;

        [JsonPropertyName("prefixoNomenclatura")]
        public string PrefixoNomenclatura { get; set; } = "AF";

        /// Diâmetros padrão por família de fixture (mm)
        [JsonPropertyName("diametrosPorAparelho")]
        public Dictionary<string, double> DiametrosPorAparelho { get; set; } = new()
        {
            { "Lavatório", 20 },
            { "Vaso Sanitário", 25 },
            { "Chuveiro", 25 },
            { "Pia Cozinha", 25 },
            { "Tanque", 25 },
            { "Máquina de Lavar", 25 },
            { "Bidê", 20 },
            { "Banheira", 25 },
            { "Torneira Jardim", 20 },
            { "Default", 25 }
        };

        /// Alturas de sub-ramal por tipo de aparelho (m acima piso acabado)
        [JsonPropertyName("alturasPorAparelho")]
        public Dictionary<string, double> AlturasPorAparelho { get; set; } = new()
        {
            { "Lavatório", 0.60 },
            { "Vaso Sanitário", 0.30 },
            { "Chuveiro", 2.20 },
            { "Pia Cozinha", 1.20 },
            { "Tanque", 1.20 },
            { "Máquina de Lavar", 0.80 },
            { "Bidê", 0.30 },
            { "Banheira", 0.50 },
            { "Torneira Jardim", 0.60 },
            { "Default", 0.60 }
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DO ROTEAMENTO
    // ══════════════════════════════════════════════════════════════

    public class RoutingResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "PENDENTE";

        [JsonPropertyName("fixturesProcessed")]
        public int FixturesProcessed { get; set; }

        [JsonPropertyName("fixturesConnected")]
        public int FixturesConnected { get; set; }

        [JsonPropertyName("fixturesFailed")]
        public int FixturesFailed { get; set; }

        [JsonPropertyName("pipesCreated")]
        public List<int> PipesCreated { get; set; } = new();

        [JsonPropertyName("fittingsCreated")]
        public List<int> FittingsCreated { get; set; } = new();

        [JsonPropertyName("trechosGerados")]
        public int TrechosGerados { get; set; }

        [JsonPropertyName("conexoesRealizadas")]
        public int ConexoesRealizadas { get; set; }

        [JsonPropertyName("trechosCriticos")]
        public List<TrechoCritico> TrechosCriticos { get; set; } = new();

        [JsonPropertyName("manualReviewRequired")]
        public List<ManualReviewItem> ManualReview { get; set; } = new();

        [JsonPropertyName("errors")]
        public List<RoutingError> Errors { get; set; } = new();

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class TrechoCritico
    {
        [JsonPropertyName("pipeId")]
        public int PipeId { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "";

        [JsonPropertyName("motivo")]
        public string Motivo { get; set; } = "";

        [JsonPropertyName("gravidade")]
        public string Gravidade { get; set; } = "MEDIA";
    }

    public class ManualReviewItem
    {
        [JsonPropertyName("fixtureId")]
        public int FixtureId { get; set; }

        [JsonPropertyName("fixtureName")]
        public string FixtureName { get; set; } = "";

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";

        [JsonPropertyName("retryCount")]
        public int RetryCount { get; set; }
    }

    public class RoutingError
    {
        [JsonPropertyName("elementId")]
        public int ElementId { get; set; }

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    public class FixtureInfo
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string FamilyName { get; set; } = "";
        public string TypeName { get; set; } = "";
        public XYZ? Location { get; set; }
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;
        public string LevelName { get; set; } = "";
        public bool HasColdWaterConnector { get; set; }
        public Connector? ColdWaterConnector { get; set; }
        public double DiametroMm { get; set; }
        public double AlturaSubRamalM { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DE PRÉ-VALIDAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class PreValidationResult
    {
        public bool IsValid { get; set; }
        public int RoomCount { get; set; }
        public int SpaceCount { get; set; }
        public int FixtureCount { get; set; }
        public int FixturesWithColdWater { get; set; }
        public int LevelCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO PRINCIPAL: ROTEAMENTO ÁGUA FRIA
    // ══════════════════════════════════════════════════════════════

    public interface IColdWaterRoutingService
    {
        PreValidationResult ValidarPreCondicoes(Document doc);
        RoutingResult ExecutarRoteamento(Document doc, ColdWaterRoutingConfig? config = null);
        RoutingResult ValidarPosRoteamento(Document doc, RoutingResult resultadoRoteamento);
    }

    public class ColdWaterRoutingService : IColdWaterRoutingService
    {
        private readonly DynamoExecutionLogger _logger;
        private readonly object _executionLock = new();

        public event Action<string>? OnProgress;
        public event Action<string, int, int>? OnFixtureProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ColdWaterRoutingService()
        {
            _logger = new DynamoExecutionLogger();
        }

        public ColdWaterRoutingService(DynamoExecutionLogger logger)
        {
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════
        //  FASE 1 — PRÉ-VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        public PreValidationResult ValidarPreCondicoes(Document doc)
        {
            var result = new PreValidationResult { IsValid = true };

            EmitProgress("Validando pré-condições do modelo...");

            // 1.1 — Rooms
            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .ToElements();

            result.RoomCount = rooms.Count;

            if (rooms.Count == 0)
            {
                result.Errors.Add("Nenhum Room encontrado no modelo.");
                result.IsValid = false;
            }

            var roomsSemNome = rooms.Count(r =>
                string.IsNullOrWhiteSpace(r.Name) || r.Name == "Room");
            if (roomsSemNome > 0)
                result.Warnings.Add(
                    $"{roomsSemNome} Room(s) sem nome definido.");

            // 1.2 — Spaces MEP
            var spaces = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .ToElements();

            result.SpaceCount = spaces.Count;

            if (spaces.Count == 0)
                result.Warnings.Add(
                    "Nenhum Space MEP encontrado. Recomenda-se executar " +
                    "02_CriarSpacesMassivo.dyn antes do roteamento.");

            // 1.3 — Fixtures com conector de água fria
            var fixtures = CollectColdWaterFixtures(doc);
            result.FixtureCount = fixtures.Count;
            result.FixturesWithColdWater = fixtures.Count(f => f.HasColdWaterConnector);

            if (fixtures.Count == 0)
            {
                result.Errors.Add("Nenhum fixture MEP encontrado no modelo.");
                result.IsValid = false;
            }

            if (result.FixturesWithColdWater == 0 && fixtures.Count > 0)
            {
                result.Errors.Add(
                    "Fixtures encontrados, porém nenhum possui conector " +
                    "de água fria (DomainType.PipingDomesticColdWater).");
                result.IsValid = false;
            }

            var fixturesSemLocacao = fixtures.Count(f => f.Location == null);
            if (fixturesSemLocacao > 0)
                result.Warnings.Add(
                    $"{fixturesSemLocacao} fixture(s) sem LocationPoint definido.");

            // 1.4 — Levels
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .ToElements();

            result.LevelCount = levels.Count;

            if (levels.Count == 0)
            {
                result.Errors.Add("Nenhum Level encontrado no modelo.");
                result.IsValid = false;
            }

            // 1.5 — PipeTypes disponíveis
            var pipeTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipeType))
                .ToElements();

            if (pipeTypes.Count == 0)
            {
                result.Errors.Add("Nenhum PipeType carregado no modelo.");
                result.IsValid = false;
            }

            // 1.6 — PipingSystemTypes
            var systemTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .ToElements();

            var hasColdWaterSystem = systemTypes.Any(st =>
            {
                var pst = st as PipingSystemType;
                return pst?.SystemClassification ==
                       MEPSystemClassification.DomesticColdWater;
            });

            if (!hasColdWaterSystem)
            {
                result.Errors.Add(
                    "Nenhum PipingSystemType com classificação " +
                    "DomesticColdWater encontrado.");
                result.IsValid = false;
            }

            EmitProgress($"Pré-validação concluída: " +
                         $"{result.RoomCount} rooms, " +
                         $"{result.FixturesWithColdWater} fixtures AF, " +
                         $"{result.LevelCount} levels. " +
                         $"Válido: {result.IsValid}");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  FASE 2 — EXECUÇÃO DO ROTEAMENTO
        // ══════════════════════════════════════════════════════════

        public RoutingResult ExecutarRoteamento(
            Document doc,
            ColdWaterRoutingConfig? config = null)
        {
            lock (_executionLock)
            {
                config ??= new ColdWaterRoutingConfig();

                var result = new RoutingResult();
                var sw = Stopwatch.StartNew();

                var log = DynamoExecutionLogger.Start(
                    "ColdWaterRoutingService",
                    JsonSerializer.Serialize(config, JsonOpts));

                try
                {
                    // ── Pré-validação ──
                    EmitProgress("Iniciando pré-validação...");
                    var preVal = ValidarPreCondicoes(doc);

                    if (!preVal.IsValid)
                    {
                        result.Status = "FALHA_PRE_VALIDACAO";
                        result.Errors.AddRange(
                            preVal.Errors.Select(e => new RoutingError
                            {
                                Stage = "PreValidation",
                                Message = e
                            }));
                        DynamoExecutionLogger.MarkFailed(log,
                            "Pré-validação falhou: " +
                            string.Join("; ", preVal.Errors));
                        _logger.WriteExecutionLog(log);
                        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                        return result;
                    }

                    // ── Coletar fixtures ──
                    EmitProgress("Coletando fixtures de água fria...");
                    var fixtures = CollectColdWaterFixtures(doc);
                    var fixturesValidos = fixtures
                        .Where(f => f.HasColdWaterConnector && f.Location != null)
                        .ToList();

                    result.FixturesProcessed = fixturesValidos.Count;

                    // ── Resolver PipeType e SystemType ──
                    var pipeType = ResolvePipeType(doc);
                    var systemType = ResolveColdWaterSystemType(doc);

                    if (pipeType == null || systemType == null)
                    {
                        result.Status = "FALHA_TIPO_NAO_ENCONTRADO";
                        result.Errors.Add(new RoutingError
                        {
                            Stage = "TypeResolution",
                            Message = "PipeType ou PipingSystemType " +
                                      "DomesticColdWater não encontrado."
                        });
                        DynamoExecutionLogger.MarkFailed(log, result.Status);
                        _logger.WriteExecutionLog(log);
                        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                        return result;
                    }

                    // ── Agrupar por Level ──
                    var porLevel = fixturesValidos
                        .GroupBy(f => f.LevelId)
                        .OrderBy(g =>
                        {
                            var lvl = doc.GetElement(g.Key) as Level;
                            return lvl?.Elevation ?? 0;
                        })
                        .ToList();

                    EmitProgress($"Roteando {fixturesValidos.Count} " +
                                 $"fixtures em {porLevel.Count} níveis...");

                    // ── Roteamento por level ──
                    using var trans = new Transaction(doc,
                        $"{config.PrefixoNomenclatura} - Roteamento Água Fria");
                    trans.Start();

                    int fixIdx = 0;

                    foreach (var levelGroup in porLevel)
                    {
                        var level = doc.GetElement(levelGroup.Key) as Level;
                        if (level == null) continue;

                        EmitProgress($"Processando {level.Name} " +
                                     $"({levelGroup.Count()} fixtures)...");

                        foreach (var fixture in levelGroup)
                        {
                            fixIdx++;
                            OnFixtureProgress?.Invoke(
                                fixture.FamilyName,
                                fixIdx,
                                fixturesValidos.Count);

                            var ok = RotearFixture(
                                doc, fixture, config, pipeType, systemType,
                                level, result);

                            if (ok)
                            {
                                result.FixturesConnected++;
                            }
                            else
                            {
                                // Retry
                                var retried = false;
                                for (int r = 0; r < config.MaxRetries; r++)
                                {
                                    EmitProgress(
                                        $"  Retry {r + 1}/{config.MaxRetries} " +
                                        $"para {fixture.FamilyName}...");

                                    if (RotearFixture(doc, fixture, config,
                                            pipeType, systemType, level, result))
                                    {
                                        result.FixturesConnected++;
                                        retried = true;
                                        break;
                                    }
                                }

                                if (!retried)
                                {
                                    result.FixturesFailed++;
                                    result.ManualReview.Add(new ManualReviewItem
                                    {
                                        FixtureId = fixture.Id.IntegerValue,
                                        FixtureName =
                                            $"{fixture.FamilyName}: {fixture.TypeName}",
                                        Reason =
                                            "Falha após tentativas de roteamento",
                                        RetryCount = config.MaxRetries
                                    });
                                }
                            }
                        }
                    }

                    // ── Conectar rede ──
                    if (config.ConnectToExisting)
                    {
                        EmitProgress("Conectando trechos adjacentes...");
                        var conCount = ConectarTrechosAdjacentes(
                            doc, result.PipesCreated, config.ToleranceMm);
                        result.ConexoesRealizadas += conCount;
                    }

                    trans.Commit();

                    // ── Determinar status ──
                    result.TrechosGerados = result.PipesCreated.Count;

                    if (result.FixturesFailed == 0 && result.PipesCreated.Count > 0)
                        result.Status = "OK";
                    else if (result.FixturesConnected > 0 && result.FixturesFailed > 0)
                        result.Status = "PARCIAL";
                    else if (result.PipesCreated.Count == 0)
                        result.Status = "FALHA_TOTAL";
                    else
                        result.Status = "OK";

                    result.Success = result.Status == "OK";
                    result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                    result.Timestamp = DateTime.UtcNow;

                    DynamoExecutionLogger.MarkSuccess(log,
                        JsonSerializer.Serialize(result, JsonOpts));
                }
                catch (Exception ex)
                {
                    result.Status = "ERRO_FATAL";
                    result.Success = false;
                    result.Errors.Add(new RoutingError
                    {
                        Stage = "Global",
                        Message = ex.Message
                    });

                    DynamoExecutionLogger.MarkException(log, ex);
                }

                _logger.WriteExecutionLog(log);
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;

                EmitProgress($"Roteamento concluído: {result.Status} " +
                             $"({result.ExecutionTimeMs}ms)");

                return result;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  FASE 3 — VALIDAÇÃO PÓS-ROTEAMENTO
        // ══════════════════════════════════════════════════════════

        public RoutingResult ValidarPosRoteamento(
            Document doc,
            RoutingResult resultadoRoteamento)
        {
            EmitProgress("Validando pós-roteamento...");

            // 3.1 — Verificar conectividade
            foreach (var pipeId in resultadoRoteamento.PipesCreated)
            {
                var pipe = doc.GetElement(new ElementId(pipeId));
                if (pipe == null)
                {
                    resultadoRoteamento.TrechosCriticos.Add(new TrechoCritico
                    {
                        PipeId = pipeId,
                        Tipo = "ORPHAN",
                        Motivo = "Pipe não encontrado no modelo após criação",
                        Gravidade = "ALTA"
                    });
                    continue;
                }

                var cm = (pipe as Pipe)?.ConnectorManager;
                if (cm == null) continue;

                var hasUnconnected = false;
                foreach (Connector c in cm.Connectors)
                {
                    if (!c.IsConnected)
                        hasUnconnected = true;
                }

                if (hasUnconnected)
                {
                    resultadoRoteamento.TrechosCriticos.Add(new TrechoCritico
                    {
                        PipeId = pipeId,
                        Tipo = "DESCONECTADO",
                        Motivo = "Pipe possui conector(es) não conectados",
                        Gravidade = "MEDIA"
                    });
                }
            }

            // 3.2 — Verificar diâmetros
            foreach (var pipeId in resultadoRoteamento.PipesCreated)
            {
                var pipe = doc.GetElement(new ElementId(pipeId)) as Pipe;
                if (pipe == null) continue;

                var diamParam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (diamParam != null)
                {
                    var diamFt = diamParam.AsDouble();
                    var diamMm = diamFt * 304.8;

                    if (diamMm < 15 || diamMm > 200)
                    {
                        resultadoRoteamento.TrechosCriticos.Add(new TrechoCritico
                        {
                            PipeId = pipeId,
                            Tipo = "DIAMETRO_INVALIDO",
                            Motivo = $"Diâmetro fora do range: {diamMm:F0}mm",
                            Gravidade = "ALTA"
                        });
                    }
                }
            }

            // 3.3 — Verificar comprimento mínimo
            foreach (var pipeId in resultadoRoteamento.PipesCreated)
            {
                var pipe = doc.GetElement(new ElementId(pipeId)) as Pipe;
                if (pipe == null) continue;

                var lenParam = pipe.get_Parameter(
                    BuiltInParameter.CURVE_ELEM_LENGTH);
                if (lenParam != null)
                {
                    var lenFt = lenParam.AsDouble();
                    if (lenFt < 0.01) // < 3mm
                    {
                        resultadoRoteamento.TrechosCriticos.Add(new TrechoCritico
                        {
                            PipeId = pipeId,
                            Tipo = "TRECHO_MINIMO",
                            Motivo = "Trecho menor que 3mm",
                            Gravidade = "MEDIA"
                        });
                    }
                }
            }

            // 3.4 — Atualizar status
            if (resultadoRoteamento.TrechosCriticos.Any(
                    t => t.Gravidade == "ALTA"))
            {
                resultadoRoteamento.Status = "VALIDACAO_COM_CRITICOS";
            }

            EmitProgress($"Validação pós-roteamento: " +
                         $"{resultadoRoteamento.TrechosCriticos.Count} " +
                         $"trecho(s) crítico(s)");

            return resultadoRoteamento;
        }

        // ══════════════════════════════════════════════════════════
        //  PIPELINE COMPLETO (conveniência)
        // ══════════════════════════════════════════════════════════

        public RoutingResult ExecutarPipelineCompleto(
            Document doc,
            ColdWaterRoutingConfig? config = null)
        {
            config ??= new ColdWaterRoutingConfig();

            EmitProgress("═══ PIPELINE ÁGUA FRIA — INÍCIO ═══");

            // Execute
            var resultado = ExecutarRoteamento(doc, config);

            // Validate
            if (resultado.Status != "FALHA_PRE_VALIDACAO" &&
                resultado.Status != "FALHA_TIPO_NAO_ENCONTRADO" &&
                resultado.Status != "ERRO_FATAL")
            {
                resultado = ValidarPosRoteamento(doc, resultado);
            }

            // Persist
            SalvarResultadoJson(resultado, config.PrefixoNomenclatura);

            EmitProgress($"═══ PIPELINE CONCLUÍDO: {resultado.Status} ═══");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  ROTEAMENTO INDIVIDUAL POR FIXTURE
        // ══════════════════════════════════════════════════════════

        private bool RotearFixture(
            Document doc,
            FixtureInfo fixture,
            ColdWaterRoutingConfig config,
            PipeType pipeType,
            PipingSystemType systemType,
            Level level,
            RoutingResult result)
        {
            try
            {
                if (fixture.Location == null || fixture.ColdWaterConnector == null)
                    return false;

                var connOrigin = fixture.ColdWaterConnector.Origin;

                // Determinar diâmetro e altura para este aparelho
                var diamMm = ResolverDiametroPorAparelho(
                    fixture.FamilyName, config);
                var alturaM = ResolverAlturaPorAparelho(
                    fixture.FamilyName, config);
                var diamFt = diamMm / 304.8;

                // Ponto de conexão no fixture
                var fixPt = connOrigin;

                // Ponto do sub-ramal (horizontal, na parede)
                var ramalZ = level.Elevation + (alturaM / 0.3048);
                var ramalPt = new XYZ(
                    fixPt.X,
                    fixPt.Y,
                    ramalZ);

                // Criar pipe: fixture → sub-ramal
                var dist = fixPt.DistanceTo(ramalPt);
                if (dist < 0.01) // mínimo 3mm
                    ramalPt = new XYZ(fixPt.X, fixPt.Y, fixPt.Z + 0.1);

                var pipe = Pipe.Create(
                    doc,
                    systemType.Id,
                    pipeType.Id,
                    level.Id,
                    fixPt,
                    ramalPt);

                // Aplicar diâmetro
                var paramDiam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (paramDiam != null && !paramDiam.IsReadOnly)
                    paramDiam.Set(diamFt);

                result.PipesCreated.Add(pipe.Id.IntegerValue);

                // Tentar conectar ao fixture
                TryConnectPipeToFixture(
                    pipe, fixture.ColdWaterConnector, config.ToleranceMm);

                result.ConexoesRealizadas++;

                return true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new RoutingError
                {
                    ElementId = fixture.Id.IntegerValue,
                    Stage = "RotearFixture",
                    Message = ex.Message
                });
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  COLETA DE FIXTURES
        // ══════════════════════════════════════════════════════════

        private List<FixtureInfo> CollectColdWaterFixtures(Document doc)
        {
            var fixtures = new List<FixtureInfo>();

            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var elem in collector)
            {
                var fi = elem as FamilyInstance;
                if (fi == null) continue;

                var info = new FixtureInfo
                {
                    Id = fi.Id,
                    FamilyName = fi.Symbol?.FamilyName ?? "",
                    TypeName = fi.Name ?? ""
                };

                // Location
                if (fi.Location is LocationPoint lp)
                    info.Location = lp.Point;

                // Level
                var levelParam = fi.get_Parameter(
                    BuiltInParameter.FAMILY_LEVEL_PARAM);
                if (levelParam != null)
                {
                    info.LevelId = levelParam.AsElementId();
                    var lvl = doc.GetElement(info.LevelId) as Level;
                    info.LevelName = lvl?.Name ?? "";
                }

                // Connectors
                var mepModel = fi.MEPModel;
                if (mepModel?.ConnectorManager != null)
                {
                    foreach (Connector c in mepModel.ConnectorManager.Connectors)
                    {
                        if (c.Domain == Domain.DomainPiping)
                        {
                            try
                            {
                                var pipeConn = c.PipeSystemType;
                                if (pipeConn == PipeSystemType.DomesticColdWater ||
                                    pipeConn == PipeSystemType.DomesticHotWater ||
                                    pipeConn == PipeSystemType.Undefined)
                                {
                                    info.HasColdWaterConnector = true;
                                    info.ColdWaterConnector = c;
                                    break;
                                }
                            }
                            catch
                            {
                                // Conector sem PipeSystemType definido
                                info.HasColdWaterConnector = true;
                                info.ColdWaterConnector = c;
                                break;
                            }
                        }
                    }
                }

                fixtures.Add(info);
            }

            return fixtures;
        }

        // ══════════════════════════════════════════════════════════
        //  RESOLUÇÃO DE TIPOS
        // ══════════════════════════════════════════════════════════

        private static PipeType? ResolvePipeType(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(PipeType))
                .Cast<PipeType>()
                .FirstOrDefault();
        }

        private static PipingSystemType? ResolveColdWaterSystemType(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .FirstOrDefault(st =>
                    st.SystemClassification ==
                    MEPSystemClassification.DomesticColdWater);
        }

        // ══════════════════════════════════════════════════════════
        //  CONEXÃO DE PIPES
        // ══════════════════════════════════════════════════════════

        private static void TryConnectPipeToFixture(
            Pipe pipe,
            Connector fixtureConnector,
            double toleranceMm)
        {
            var toleranceFt = toleranceMm / 304.8;

            var pipeConnectors = pipe.ConnectorManager?.Connectors;
            if (pipeConnectors == null) return;

            Connector? bestMatch = null;
            double bestDist = double.MaxValue;

            foreach (Connector pc in pipeConnectors)
            {
                var d = pc.Origin.DistanceTo(fixtureConnector.Origin);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestMatch = pc;
                }
            }

            if (bestMatch != null && bestDist <= toleranceFt)
            {
                if (!bestMatch.IsConnected && !fixtureConnector.IsConnected)
                {
                    try
                    {
                        bestMatch.ConnectTo(fixtureConnector);
                    }
                    catch
                    {
                        // conexão incompatível
                    }
                }
            }
        }

        private static int ConectarTrechosAdjacentes(
            Document doc,
            List<int> pipeIds,
            double toleranceMm)
        {
            var toleranceFt = toleranceMm / 304.8;
            int connections = 0;

            var pipes = pipeIds
                .Select(id => doc.GetElement(new ElementId(id)) as Pipe)
                .Where(p => p != null)
                .ToList();

            for (int i = 0; i < pipes.Count; i++)
            {
                var cm1 = pipes[i]!.ConnectorManager;
                if (cm1 == null) continue;

                for (int j = i + 1; j < pipes.Count; j++)
                {
                    var cm2 = pipes[j]!.ConnectorManager;
                    if (cm2 == null) continue;

                    Connector? bestC1 = null, bestC2 = null;
                    double bestDist = double.MaxValue;

                    foreach (Connector c1 in cm1.Connectors)
                    {
                        if (c1.IsConnected) continue;
                        foreach (Connector c2 in cm2.Connectors)
                        {
                            if (c2.IsConnected) continue;
                            var d = c1.Origin.DistanceTo(c2.Origin);
                            if (d < bestDist)
                            {
                                bestDist = d;
                                bestC1 = c1;
                                bestC2 = c2;
                            }
                        }
                    }

                    if (bestC1 != null && bestC2 != null &&
                        bestDist <= toleranceFt)
                    {
                        try
                        {
                            bestC1.ConnectTo(bestC2);
                            connections++;
                        }
                        catch
                        {
                            // incompatível
                        }
                    }
                }
            }

            return connections;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private static double ResolverDiametroPorAparelho(
            string familyName,
            ColdWaterRoutingConfig config)
        {
            foreach (var kvp in config.DiametrosPorAparelho)
            {
                if (familyName.IndexOf(kvp.Key,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }

            return config.DiametrosPorAparelho
                .GetValueOrDefault("Default", config.DiametroPadraoMm);
        }

        private static double ResolverAlturaPorAparelho(
            string familyName,
            ColdWaterRoutingConfig config)
        {
            foreach (var kvp in config.AlturasPorAparelho)
            {
                if (familyName.IndexOf(kvp.Key,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }

            return config.AlturasPorAparelho
                .GetValueOrDefault("Default", config.AlturaRamalM);
        }

        private void SalvarResultadoJson(
            RoutingResult resultado,
            string prefixo)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Routing");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"{prefixo}_routing_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(resultado, JsonOpts);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                EmitProgress($"Resultado salvo em: {filePath}");
            }
            catch
            {
                // falha no salvamento não deve quebrar o fluxo
            }
        }

        private void EmitProgress(string message)
        {
            OnProgress?.Invoke(message);
        }
    }
}
