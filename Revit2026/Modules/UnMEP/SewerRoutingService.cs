using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Revit2026.Modules.DynamoIntegration.Logging;

namespace Revit2026.Modules.UnMEP
{
    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO — ESGOTO SANITÁRIO (NBR 8160)
    // ══════════════════════════════════════════════════════════════

    public class SewerRoutingConfig
    {
        [JsonPropertyName("sistema")]
        public string Sistema { get; set; } = "Esgoto Sanitário";

        [JsonPropertyName("diametroPadraoMm")]
        public double DiametroPadraoMm { get; set; } = 50;

        [JsonPropertyName("alturaRamalM")]
        public double AlturaRamalM { get; set; } = 0.10;

        [JsonPropertyName("offsetLajeFundoM")]
        public double OffsetLajeFundoM { get; set; } = 0.15;

        [JsonPropertyName("offsetParedeM")]
        public double OffsetParedeM { get; set; } = 0.05;

        [JsonPropertyName("offsetMinimoEntreEixosM")]
        public double OffsetMinimoEntreEixosM { get; set; } = 0.30;

        [JsonPropertyName("connectToExisting")]
        public bool ConnectToExisting { get; set; } = true;

        [JsonPropertyName("toleranceMm")]
        public double ToleranceMm { get; set; } = 5;

        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 2;

        [JsonPropertyName("prefixoNomenclatura")]
        public string PrefixoNomenclatura { get; set; } = "ES";

        [JsonPropertyName("criarSubcoletores")]
        public bool CriarSubcoletores { get; set; } = true;

        [JsonPropertyName("comprimentoMaximoRamalM")]
        public double ComprimentoMaximoRamalM { get; set; } = 5.0;

        /// NBR 8160: Diâmetros mínimos por aparelho (mm)
        [JsonPropertyName("diametrosPorAparelho")]
        public Dictionary<string, double> DiametrosPorAparelho { get; set; } = new()
        {
            { "Lavatório", 40 },
            { "Vaso Sanitário", 100 },
            { "Chuveiro", 40 },
            { "Pia Cozinha", 50 },
            { "Tanque", 50 },
            { "Máquina de Lavar", 50 },
            { "Bidê", 40 },
            { "Banheira", 50 },
            { "Ralo Sifonado", 50 },
            { "Ralo Seco", 40 },
            { "Mictório", 50 },
            { "Default", 50 }
        };

        /// NBR 8160: UHC por aparelho (Unidades Hunter de Contribuição)
        [JsonPropertyName("uhcPorAparelho")]
        public Dictionary<string, int> UhcPorAparelho { get; set; } = new()
        {
            { "Lavatório", 1 },
            { "Vaso Sanitário", 6 },
            { "Chuveiro", 2 },
            { "Pia Cozinha", 3 },
            { "Tanque", 3 },
            { "Máquina de Lavar", 3 },
            { "Bidê", 1 },
            { "Banheira", 3 },
            { "Ralo Sifonado", 2 },
            { "Ralo Seco", 1 },
            { "Mictório", 2 },
            { "Default", 2 }
        };

        /// Alturas de sub-ramal de esgoto por aparelho (m abaixo piso acabado)
        [JsonPropertyName("alturasSaidaPorAparelho")]
        public Dictionary<string, double> AlturasSaidaPorAparelho { get; set; } = new()
        {
            { "Lavatório", -0.05 },
            { "Vaso Sanitário", -0.10 },
            { "Chuveiro", -0.05 },
            { "Pia Cozinha", -0.10 },
            { "Tanque", -0.10 },
            { "Máquina de Lavar", -0.05 },
            { "Bidê", -0.05 },
            { "Banheira", -0.05 },
            { "Ralo Sifonado", -0.05 },
            { "Ralo Seco", -0.05 },
            { "Mictório", -0.10 },
            { "Default", -0.05 }
        };

        /// NBR 8160 — Declividade mínima por faixa de diâmetro
        public double GetSlopePercent(double diameterMm)
        {
            // NBR 8160:1999 / ABNT: Ø ≤ 75mm → 2%, Ø ≥ 100mm → 1%
            if (diameterMm <= 75) return 2.0;
            return 1.0;
        }

        /// NBR 8160 — Diâmetro do ramal por somatório UHC
        public double GetDiametroPorUhcAcumulado(int uhcTotal)
        {
            if (uhcTotal <= 3) return 50;
            if (uhcTotal <= 6) return 75;
            if (uhcTotal <= 20) return 100;
            if (uhcTotal <= 160) return 150;
            return 200;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DO ROTEAMENTO DE ESGOTO
    // ══════════════════════════════════════════════════════════════

    public class SewerRoutingResult
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

        [JsonPropertyName("uhcTotal")]
        public int UhcTotal { get; set; }

        [JsonPropertyName("pipesCreated")]
        public List<long> PipesCreated { get; set; } = new();

        [JsonPropertyName("fittingsCreated")]
        public List<long> FittingsCreated { get; set; } = new();

        [JsonPropertyName("trechosGerados")]
        public int TrechosGerados { get; set; }

        [JsonPropertyName("conexoesRealizadas")]
        public int ConexoesRealizadas { get; set; }

        [JsonPropertyName("subcoletoresCriados")]
        public int SubcoletoresCriados { get; set; }

        [JsonPropertyName("slopeApplied")]
        public List<SlopeDetail> SlopeDetails { get; set; } = new();

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

    public class SlopeDetail
    {
        [JsonPropertyName("pipeId")]
        public long PipeId { get; set; }

        [JsonPropertyName("diameterMm")]
        public double DiameterMm { get; set; }

        [JsonPropertyName("slopePercent")]
        public double SlopePercent { get; set; }

        [JsonPropertyName("lengthM")]
        public double LengthM { get; set; }

        [JsonPropertyName("dropM")]
        public double DropM { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  FIXTURE INFO — ESGOTO
    // ══════════════════════════════════════════════════════════════

    public class SewerFixtureInfo
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string FamilyName { get; set; } = "";
        public string TypeName { get; set; } = "";
        public XYZ? Location { get; set; }
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;
        public string LevelName { get; set; } = "";
        public bool HasDrainConnector { get; set; }
        public Connector? DrainConnector { get; set; }
        public double DiametroMm { get; set; }
        public int Uhc { get; set; }
        public double AlturaSubRamalM { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  PRÉ-VALIDAÇÃO — ESGOTO
    // ══════════════════════════════════════════════════════════════

    public class SewerPreValidationResult
    {
        public bool IsValid { get; set; }
        public int RoomCount { get; set; }
        public int SpaceCount { get; set; }
        public int FixtureCount { get; set; }
        public int FixturesWithDrain { get; set; }
        public int UhcTotal { get; set; }
        public int LevelCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO PRINCIPAL: ROTEAMENTO ESGOTO SANITÁRIO
    // ══════════════════════════════════════════════════════════════

    public interface ISewerRoutingService
    {
        SewerPreValidationResult ValidarPreCondicoes(Document doc);

        SewerRoutingResult ExecutarRoteamento(
            Document doc, SewerRoutingConfig? config = null);

        SewerRoutingResult ValidarPosRoteamento(
            Document doc, SewerRoutingResult resultado);

        SewerRoutingResult ExecutarPipelineCompleto(
            Document doc, SewerRoutingConfig? config = null);
    }

    public class SewerRoutingService : ISewerRoutingService
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

        public SewerRoutingService()
        {
            _logger = new DynamoExecutionLogger();
        }

        public SewerRoutingService(DynamoExecutionLogger logger)
        {
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════
        //  FASE 1 — PRÉ-VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        public SewerPreValidationResult ValidarPreCondicoes(Document doc)
        {
            var result = new SewerPreValidationResult { IsValid = true };

            EmitProgress("Validando pré-condições para esgoto sanitário...");

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

            // 1.3 — Fixtures com conector de esgoto
            var config = new SewerRoutingConfig();
            var fixtures = CollectSewerFixtures(doc, config);
            result.FixtureCount = fixtures.Count;
            result.FixturesWithDrain = fixtures.Count(f => f.HasDrainConnector);
            result.UhcTotal = fixtures.Where(f => f.HasDrainConnector).Sum(f => f.Uhc);

            if (fixtures.Count == 0)
            {
                result.Errors.Add(
                    "Nenhum fixture MEP de esgoto encontrado no modelo.");
                result.IsValid = false;
            }

            if (result.FixturesWithDrain == 0 && fixtures.Count > 0)
            {
                result.Errors.Add(
                    "Fixtures encontrados, porém nenhum possui conector " +
                    "de drenagem (DomainPiping + Sanitary/Other).");
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

            // 1.5 — PipeTypes
            var pipeTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipeType))
                .ToElements();

            if (pipeTypes.Count == 0)
            {
                result.Errors.Add("Nenhum PipeType carregado no modelo.");
                result.IsValid = false;
            }

            // 1.6 — PipingSystemType Sanitário
            var hasSanitarySystem = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .Any(st =>
                    st.SystemClassification == MEPSystemClassification.Sanitary ||
                    st.SystemClassification == MEPSystemClassification.OtherPipe);

            if (!hasSanitarySystem)
            {
                result.Errors.Add(
                    "Nenhum PipingSystemType com classificação " +
                    "Sanitary ou OtherPipe encontrado.");
                result.IsValid = false;
            }

            EmitProgress($"Pré-validação esgoto concluída: " +
                         $"{result.RoomCount} rooms, " +
                         $"{result.FixturesWithDrain} fixtures ES, " +
                         $"UHC total: {result.UhcTotal}, " +
                         $"{result.LevelCount} levels. " +
                         $"Válido: {result.IsValid}");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  FASE 2 — EXECUÇÃO DO ROTEAMENTO
        // ══════════════════════════════════════════════════════════

        public SewerRoutingResult ExecutarRoteamento(
            Document doc,
            SewerRoutingConfig? config = null)
        {
            lock (_executionLock)
            {
                config ??= new SewerRoutingConfig();

                var result = new SewerRoutingResult();
                var sw = Stopwatch.StartNew();

                var log = DynamoExecutionLogger.Start(
                    "SewerRoutingService",
                    JsonSerializer.Serialize(config, JsonOpts));

                try
                {
                    // ── Pré-validação ──
                    EmitProgress("Iniciando pré-validação esgoto...");
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
                            "Pré-validação esgoto falhou: " +
                            string.Join("; ", preVal.Errors));
                        _logger.WriteExecutionLog(log);
                        result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                        return result;
                    }

                    // ── Coletar fixtures ──
                    EmitProgress("Coletando fixtures de esgoto...");
                    var fixtures = CollectSewerFixtures(doc, config);
                    var fixturesValidos = fixtures
                        .Where(f => f.HasDrainConnector && f.Location != null)
                        .ToList();

                    result.FixturesProcessed = fixturesValidos.Count;
                    result.UhcTotal = fixturesValidos.Sum(f => f.Uhc);

                    // ── Resolver PipeType e SystemType ──
                    var pipeType = ResolvePipeType(doc);
                    var systemType = ResolveSanitarySystemType(doc);

                    if (pipeType == null || systemType == null)
                    {
                        result.Status = "FALHA_TIPO_NAO_ENCONTRADO";
                        result.Errors.Add(new RoutingError
                        {
                            Stage = "TypeResolution",
                            Message = "PipeType ou PipingSystemType " +
                                      "Sanitary não encontrado."
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
                                 $"fixtures esgoto em {porLevel.Count} " +
                                 $"níveis (UHC total: {result.UhcTotal})...");

                    // ── Roteamento por level ──
                    using var trans = new Transaction(doc,
                        $"{config.PrefixoNomenclatura} - Roteamento Esgoto");
                    trans.Start();

                    int fixIdx = 0;

                    foreach (var levelGroup in porLevel)
                    {
                        var level = doc.GetElement(levelGroup.Key) as Level;
                        if (level == null) continue;

                        var levelFixtures = levelGroup.ToList();
                        int levelUhc = levelFixtures.Sum(f => f.Uhc);

                        EmitProgress($"Processando {level.Name}: " +
                                     $"{levelFixtures.Count} fixtures, " +
                                     $"UHC={levelUhc}");

                        // ── Ramais individuais por fixture ──
                        foreach (var fixture in levelFixtures)
                        {
                            fixIdx++;
                            OnFixtureProgress?.Invoke(
                                fixture.FamilyName,
                                fixIdx,
                                fixturesValidos.Count);

                            var ok = RotearFixtureEsgoto(
                                doc, fixture, config, pipeType,
                                systemType, level, result);

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

                                    if (RotearFixtureEsgoto(doc, fixture, config,
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
                                        FixtureId = fixture.Id.Value,
                                        FixtureName =
                                            $"{fixture.FamilyName}: {fixture.TypeName}",
                                        Reason =
                                            "Falha após tentativas de " +
                                            "roteamento esgoto",
                                        RetryCount = config.MaxRetries
                                    });
                                }
                            }
                        }

                        // ── Subcoletor do nível ──
                        if (config.CriarSubcoletores && levelFixtures.Count > 1)
                        {
                            var subResult = CriarSubcoletor(
                                doc, levelFixtures, config, pipeType,
                                systemType, level, result);

                            if (subResult)
                            {
                                result.SubcoletoresCriados++;
                                EmitProgress($"  Subcoletor criado em " +
                                             $"{level.Name}");
                            }
                        }
                    }

                    // ── Conectar trechos adjacentes ──
                    if (config.ConnectToExisting)
                    {
                        EmitProgress("Conectando trechos adjacentes esgoto...");
                        var conCount = ConectarTrechosAdjacentes(
                            doc, result.PipesCreated, config.ToleranceMm);
                        result.ConexoesRealizadas += conCount;
                    }

                    trans.Commit();

                    // ── Determinar status ──
                    result.TrechosGerados = result.PipesCreated.Count;

                    if (result.FixturesFailed == 0 &&
                        result.PipesCreated.Count > 0)
                        result.Status = "OK";
                    else if (result.FixturesConnected > 0 &&
                             result.FixturesFailed > 0)
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

                EmitProgress($"Roteamento esgoto concluído: {result.Status} " +
                             $"({result.ExecutionTimeMs}ms)");

                return result;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  FASE 3 — VALIDAÇÃO PÓS-ROTEAMENTO
        // ══════════════════════════════════════════════════════════

        public SewerRoutingResult ValidarPosRoteamento(
            Document doc,
            SewerRoutingResult resultado)
        {
            EmitProgress("Validando pós-roteamento esgoto...");

            foreach (var pipeId in resultado.PipesCreated)
            {
                var pipe = doc.GetElement(new ElementId(pipeId)) as Pipe;
                if (pipe == null)
                {
                    resultado.TrechosCriticos.Add(new TrechoCritico
                    {
                        PipeId = pipeId,
                        Tipo = "ORPHAN",
                        Motivo = "Pipe não encontrado após criação",
                        Gravidade = "ALTA"
                    });
                    continue;
                }

                // 3.1 — Conectividade
                var cm = pipe.ConnectorManager;
                if (cm != null)
                {
                    foreach (Connector c in cm.Connectors)
                    {
                        if (!c.IsConnected)
                        {
                            resultado.TrechosCriticos.Add(new TrechoCritico
                            {
                                PipeId = pipeId,
                                Tipo = "DESCONECTADO",
                                Motivo = "Conector não conectado",
                                Gravidade = "MEDIA"
                            });
                            break;
                        }
                    }
                }

                // 3.2 — Diâmetro válido (40–200mm para esgoto)
                var diamParam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (diamParam != null)
                {
                    var diamMm = diamParam.AsDouble() * 304.8;
                    if (diamMm < 40 || diamMm > 200)
                    {
                        resultado.TrechosCriticos.Add(new TrechoCritico
                        {
                            PipeId = pipeId,
                            Tipo = "DIAMETRO_INVALIDO",
                            Motivo = $"Diâmetro fora do range esgoto: " +
                                     $"{diamMm:F0}mm (NBR 8160: 40–200mm)",
                            Gravidade = "ALTA"
                        });
                    }
                }

                // 3.3 — Comprimento mínimo
                var lenParam = pipe.get_Parameter(
                    BuiltInParameter.CURVE_ELEM_LENGTH);
                if (lenParam != null)
                {
                    var lenFt = lenParam.AsDouble();
                    if (lenFt < 0.01)
                    {
                        resultado.TrechosCriticos.Add(new TrechoCritico
                        {
                            PipeId = pipeId,
                            Tipo = "TRECHO_MINIMO",
                            Motivo = "Trecho menor que 3mm",
                            Gravidade = "MEDIA"
                        });
                    }
                }

                // 3.4 — Validar slope em trechos horizontais
                var loc = pipe.Location as LocationCurve;
                if (loc?.Curve is Line line)
                {
                    var p0 = line.GetEndPoint(0);
                    var p1 = line.GetEndPoint(1);
                    var dz = Math.Abs(p1.Z - p0.Z);
                    var horizontalLen = Math.Sqrt(
                        Math.Pow(p1.X - p0.X, 2) +
                        Math.Pow(p1.Y - p0.Y, 2));

                    if (horizontalLen > 0.1) // trecho > ~30mm
                    {
                        var slopePercent = (dz / horizontalLen) * 100.0;
                        var diamMm2 = (diamParam?.AsDouble() ?? 0) * 304.8;
                        var expectedSlope = new SewerRoutingConfig()
                            .GetSlopePercent(diamMm2);

                        if (slopePercent < expectedSlope * 0.8) // 20% tolerância
                        {
                            resultado.TrechosCriticos.Add(new TrechoCritico
                            {
                                PipeId = pipeId,
                                Tipo = "SLOPE_INSUFICIENTE",
                                Motivo = $"Slope {slopePercent:F2}% < " +
                                         $"mínimo {expectedSlope}% " +
                                         $"(Ø{diamMm2:F0}mm, NBR 8160)",
                                Gravidade = "ALTA"
                            });
                        }
                    }
                }
            }

            // 3.5 — Atualizar status
            var altaCount = resultado.TrechosCriticos
                .Count(t => t.Gravidade == "ALTA");
            if (altaCount > 0)
                resultado.Status = "VALIDACAO_COM_CRITICOS";

            EmitProgress($"Pós-validação esgoto: " +
                         $"{resultado.TrechosCriticos.Count} crítico(s), " +
                         $"{altaCount} de gravidade ALTA");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  PIPELINE COMPLETO
        // ══════════════════════════════════════════════════════════

        public SewerRoutingResult ExecutarPipelineCompleto(
            Document doc,
            SewerRoutingConfig? config = null)
        {
            config ??= new SewerRoutingConfig();

            EmitProgress("═══ PIPELINE ESGOTO SANITÁRIO — INÍCIO ═══");

            var resultado = ExecutarRoteamento(doc, config);

            if (resultado.Status != "FALHA_PRE_VALIDACAO" &&
                resultado.Status != "FALHA_TIPO_NAO_ENCONTRADO" &&
                resultado.Status != "ERRO_FATAL")
            {
                resultado = ValidarPosRoteamento(doc, resultado);
            }

            SalvarResultadoJson(resultado, config.PrefixoNomenclatura);

            EmitProgress($"═══ PIPELINE ESGOTO CONCLUÍDO: " +
                         $"{resultado.Status} ═══");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  ROTEAMENTO INDIVIDUAL POR FIXTURE
        // ══════════════════════════════════════════════════════════

        private bool RotearFixtureEsgoto(
            Document doc,
            SewerFixtureInfo fixture,
            SewerRoutingConfig config,
            PipeType pipeType,
            PipingSystemType systemType,
            Level level,
            SewerRoutingResult result)
        {
            try
            {
                if (fixture.Location == null || fixture.DrainConnector == null)
                    return false;

                var connOrigin = fixture.DrainConnector.Origin;

                // Resolver diâmetro por aparelho
                var diamMm = ResolverDiametroPorAparelho(
                    fixture.FamilyName, config);
                var diamFt = diamMm / 304.8;

                // Resolver slope conforme NBR 8160
                var slopePercent = config.GetSlopePercent(diamMm);
                var slopeFraction = slopePercent / 100.0;

                // Altura de saída
                var alturaM = ResolverAlturaSaida(
                    fixture.FamilyName, config);

                // Ponto do fixture (conector drain)
                var fixPt = connOrigin;

                // Ponto do ramal horizontal (sob laje)
                var ramalZ = level.Elevation +
                             (alturaM / 0.3048);

                // Comprimento horizontal estimado (fixture → parede)
                var horizontalLen = 1.0 / 0.3048; // ~1m padrão

                // Drop calculado pelo slope
                var drop = horizontalLen * slopeFraction;

                // Ponto de destino com inclinação aplicada
                var ramalPt = new XYZ(
                    fixPt.X,
                    fixPt.Y - horizontalLen,
                    ramalZ - drop);

                // Minimo distância
                var dist = fixPt.DistanceTo(ramalPt);
                if (dist < 0.01)
                    ramalPt = new XYZ(fixPt.X, fixPt.Y - 0.1, fixPt.Z - 0.05);

                // Criar pipe com slope
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

                // Aplicar slope no parâmetro do Pipe
                var paramSlope = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_SLOPE);
                if (paramSlope != null && !paramSlope.IsReadOnly)
                    paramSlope.Set(slopeFraction);

                result.PipesCreated.Add(pipe.Id.Value);

                // Registrar detalhes do slope
                var lenM = dist * 0.3048;
                result.SlopeDetails.Add(new SlopeDetail
                {
                    PipeId = pipe.Id.Value,
                    DiameterMm = diamMm,
                    SlopePercent = slopePercent,
                    LengthM = Math.Round(lenM, 4),
                    DropM = Math.Round(drop * 0.3048, 4)
                });

                // Conectar ao fixture
                TryConnectPipeToFixture(
                    pipe, fixture.DrainConnector, config.ToleranceMm);

                result.ConexoesRealizadas++;
                return true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new RoutingError
                {
                    ElementId = fixture.Id.Value,
                    Stage = "RotearFixtureEsgoto",
                    Message = ex.Message
                });
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SUBCOLETOR POR NÍVEL
        // ══════════════════════════════════════════════════════════

        private bool CriarSubcoletor(
            Document doc,
            List<SewerFixtureInfo> fixtures,
            SewerRoutingConfig config,
            PipeType pipeType,
            PipingSystemType systemType,
            Level level,
            SewerRoutingResult result)
        {
            try
            {
                if (fixtures.Count < 2) return false;

                // UHC acumulado do nível → diâmetro do subcoletor
                var uhcLevel = fixtures.Sum(f => f.Uhc);
                var subDiamMm = config.GetDiametroPorUhcAcumulado(uhcLevel);
                var subDiamFt = subDiamMm / 304.8;
                var slopePercent = config.GetSlopePercent(subDiamMm);
                var slopeFraction = slopePercent / 100.0;

                // Encontrar bounding box dos fixtures
                var xs = fixtures
                    .Where(f => f.Location != null)
                    .Select(f => f.Location!.X)
                    .ToList();
                var ys = fixtures
                    .Where(f => f.Location != null)
                    .Select(f => f.Location!.Y)
                    .ToList();

                if (xs.Count < 2) return false;

                var minX = xs.Min();
                var maxX = xs.Max();
                var avgY = ys.Average();

                // Z do subcoletor (abaixo da laje)
                var subZ = level.Elevation -
                           (config.OffsetLajeFundoM / 0.3048);

                var totalLen = maxX - minX;
                if (totalLen < 0.05) totalLen = 1.0 / 0.3048;

                var drop = totalLen * slopeFraction;

                var startPt = new XYZ(minX, avgY, subZ);
                var endPt = new XYZ(maxX, avgY, subZ - drop);

                var subPipe = Pipe.Create(
                    doc,
                    systemType.Id,
                    pipeType.Id,
                    level.Id,
                    startPt,
                    endPt);

                var paramDiam = subPipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (paramDiam != null && !paramDiam.IsReadOnly)
                    paramDiam.Set(subDiamFt);

                var paramSlope = subPipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_SLOPE);
                if (paramSlope != null && !paramSlope.IsReadOnly)
                    paramSlope.Set(slopeFraction);

                result.PipesCreated.Add(subPipe.Id.Value);

                result.SlopeDetails.Add(new SlopeDetail
                {
                    PipeId = subPipe.Id.Value,
                    DiameterMm = subDiamMm,
                    SlopePercent = slopePercent,
                    LengthM = Math.Round(totalLen * 0.3048, 4),
                    DropM = Math.Round(drop * 0.3048, 4)
                });

                return true;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new RoutingError
                {
                    Stage = "CriarSubcoletor",
                    Message = $"{level.Name}: {ex.Message}"
                });
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  COLETA DE FIXTURES DE ESGOTO
        // ══════════════════════════════════════════════════════════

        private List<SewerFixtureInfo> CollectSewerFixtures(
            Document doc,
            SewerRoutingConfig config)
        {
            var fixtures = new List<SewerFixtureInfo>();

            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var elem in collector)
            {
                var fi = elem as FamilyInstance;
                if (fi == null) continue;

                var info = new SewerFixtureInfo
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

                // UHC
                info.Uhc = ResolverUhc(info.FamilyName, config);

                // Diâmetro mínimo
                info.DiametroMm = ResolverDiametroPorAparelho(
                    info.FamilyName, config);

                // Connectors — buscar drain/sanitary
                var mepModel = fi.MEPModel;
                if (mepModel?.ConnectorManager != null)
                {
                    foreach (Connector c in mepModel.ConnectorManager.Connectors)
                    {
                        if (c.Domain != Domain.DomainPiping)
                            continue;

                        try
                        {
                            var pst = c.PipeSystemType;
                            if (pst == PipeSystemType.Sanitary ||
                                pst == PipeSystemType.OtherPipe)
                            {
                                info.HasDrainConnector = true;
                                info.DrainConnector = c;
                                break;
                            }
                        }
                        catch
                        {
                            // Conector sem PipeSystemType → assume drain
                            info.HasDrainConnector = true;
                            info.DrainConnector = c;
                            break;
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

        private static PipingSystemType? ResolveSanitarySystemType(
            Document doc)
        {
            // Prefere Sanitary, fallback para OtherPipe
            var sanitary = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .FirstOrDefault(st =>
                    st.SystemClassification ==
                    MEPSystemClassification.Sanitary);

            if (sanitary != null) return sanitary;

            return new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .FirstOrDefault(st =>
                    st.SystemClassification ==
                    MEPSystemClassification.OtherPipe);
        }

        // ══════════════════════════════════════════════════════════
        //  CONEXÃO
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
            List<long> pipeIds,
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
            SewerRoutingConfig config)
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

        private static int ResolverUhc(
            string familyName,
            SewerRoutingConfig config)
        {
            foreach (var kvp in config.UhcPorAparelho)
            {
                if (familyName.IndexOf(kvp.Key,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }

            return config.UhcPorAparelho
                .GetValueOrDefault("Default", 2);
        }

        private static double ResolverAlturaSaida(
            string familyName,
            SewerRoutingConfig config)
        {
            foreach (var kvp in config.AlturasSaidaPorAparelho)
            {
                if (familyName.IndexOf(kvp.Key,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }

            return config.AlturasSaidaPorAparelho
                .GetValueOrDefault("Default", -0.05);
        }

        private void SalvarResultadoJson(
            SewerRoutingResult resultado,
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
