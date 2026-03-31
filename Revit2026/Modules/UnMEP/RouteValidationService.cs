using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Revit2026.Modules.DynamoIntegration.Logging;

namespace Revit2026.Modules.UnMEP
{
    // ══════════════════════════════════════════════════════════════
    //  ENUMS E STATUS
    // ══════════════════════════════════════════════════════════════

    public enum RouteItemStatus
    {
        Ok,
        AjusteNecessario,
        FalhaCritica
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIGURAÇÃO DE VALIDAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class RouteValidationConfig
    {
        [JsonPropertyName("validarConectividade")]
        public bool ValidarConectividade { get; set; } = true;

        [JsonPropertyName("validarDiametros")]
        public bool ValidarDiametros { get; set; } = true;

        [JsonPropertyName("validarSlope")]
        public bool ValidarSlope { get; set; } = true;

        [JsonPropertyName("validarAlturas")]
        public bool ValidarAlturas { get; set; } = true;

        [JsonPropertyName("validarConflitos")]
        public bool ValidarConflitos { get; set; } = true;

        [JsonPropertyName("validarComprimento")]
        public bool ValidarComprimento { get; set; } = true;

        [JsonPropertyName("toleranciaConexaoMm")]
        public double ToleranciaConexaoMm { get; set; } = 5;

        [JsonPropertyName("toleranciaSlopePercent")]
        public double ToleranciaSlopePercent { get; set; } = 20;

        [JsonPropertyName("distanciaMinConflitMm")]
        public double DistanciaMinConflitMm { get; set; } = 25;

        [JsonPropertyName("comprimentoMinMm")]
        public double ComprimentoMinMm { get; set; } = 3;

        [JsonPropertyName("comprimentoMaxM")]
        public double ComprimentoMaxM { get; set; } = 15;

        // NBR 5626 — Água fria
        [JsonPropertyName("afDiametroMinMm")]
        public double AfDiametroMinMm { get; set; } = 15;

        [JsonPropertyName("afDiametroMaxMm")]
        public double AfDiametroMaxMm { get; set; } = 200;

        // NBR 8160 — Esgoto
        [JsonPropertyName("esDiametroMinMm")]
        public double EsDiametroMinMm { get; set; } = 40;

        [JsonPropertyName("esDiametroMaxMm")]
        public double EsDiametroMaxMm { get; set; } = 200;

        [JsonPropertyName("esSlopeMin75Percent")]
        public double EsSlopeMin75Percent { get; set; } = 2.0;

        [JsonPropertyName("esSlopeMin100Percent")]
        public double EsSlopeMin100Percent { get; set; } = 1.0;

        // Alturas
        [JsonPropertyName("alturaMinRamalM")]
        public double AlturaMinRamalM { get; set; } = -0.50;

        [JsonPropertyName("alturaMaxRamalM")]
        public double AlturaMaxRamalM { get; set; } = 3.50;
    }

    // ══════════════════════════════════════════════════════════════
    //  ITEM DE VALIDAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class RouteValidationItem
    {
        [JsonPropertyName("elementId")]
        public int ElementId { get; set; }

        [JsonPropertyName("elementType")]
        public string ElementType { get; set; } = "";

        [JsonPropertyName("systemName")]
        public string SystemName { get; set; } = "";

        [JsonPropertyName("systemClassification")]
        public string SystemClassification { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "OK";

        [JsonPropertyName("statusEnum")]
        [JsonIgnore]
        public RouteItemStatus StatusEnum { get; set; } = RouteItemStatus.Ok;

        [JsonPropertyName("diameterMm")]
        public double DiameterMm { get; set; }

        [JsonPropertyName("lengthM")]
        public double LengthM { get; set; }

        [JsonPropertyName("slopePercent")]
        public double SlopePercent { get; set; }

        [JsonPropertyName("heightM")]
        public double HeightM { get; set; }

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; } = "";

        [JsonPropertyName("connectorsTotal")]
        public int ConnectorsTotal { get; set; }

        [JsonPropertyName("connectorsConnected")]
        public int ConnectorsConnected { get; set; }

        [JsonPropertyName("isFullyConnected")]
        public bool IsFullyConnected { get; set; }

        [JsonPropertyName("issues")]
        public List<ValidationIssue> Issues { get; set; } = new();
    }

    public class ValidationIssue
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "INFO";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("suggestedAction")]
        public string SuggestedAction { get; set; } = "";

        [JsonPropertyName("currentValue")]
        public string CurrentValue { get; set; } = "";

        [JsonPropertyName("expectedValue")]
        public string ExpectedValue { get; set; } = "";
    }

    public class ConflictItem
    {
        [JsonPropertyName("elementA")]
        public int ElementA { get; set; }

        [JsonPropertyName("elementB")]
        public int ElementB { get; set; }

        [JsonPropertyName("distanceMm")]
        public double DistanceMm { get; set; }

        [JsonPropertyName("conflictType")]
        public string ConflictType { get; set; } = "";

        [JsonPropertyName("location")]
        public string Location { get; set; } = "";
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DA VALIDAÇÃO
    // ══════════════════════════════════════════════════════════════

    public class RouteValidationResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "PENDENTE";

        // ── Contadores gerais ──

        [JsonPropertyName("totalPipes")]
        public int TotalPipes { get; set; }

        [JsonPropertyName("totalFittings")]
        public int TotalFittings { get; set; }

        [JsonPropertyName("totalElements")]
        public int TotalElements { get; set; }

        // ── Contadores por status ──

        [JsonPropertyName("okCount")]
        public int OkCount { get; set; }

        [JsonPropertyName("warningCount")]
        public int WarningCount { get; set; }

        [JsonPropertyName("criticalCount")]
        public int CriticalCount { get; set; }

        // ── Conformidade ──

        [JsonPropertyName("connectivityRate")]
        public double ConnectivityRate { get; set; }

        [JsonPropertyName("diameterConformityRate")]
        public double DiameterConformityRate { get; set; }

        [JsonPropertyName("slopeConformityRate")]
        public double SlopeConformityRate { get; set; }

        [JsonPropertyName("overallConformityRate")]
        public double OverallConformityRate { get; set; }

        // ── Detalhes ──

        [JsonPropertyName("items")]
        public List<RouteValidationItem> Items { get; set; } = new();

        [JsonPropertyName("conflicts")]
        public List<ConflictItem> Conflicts { get; set; } = new();

        // ── Resumo por sistema ──

        [JsonPropertyName("summaryBySystem")]
        public Dictionary<string, SystemSummary> SummaryBySystem { get; set; } = new();

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SystemSummary
    {
        [JsonPropertyName("systemName")]
        public string SystemName { get; set; } = "";

        [JsonPropertyName("pipeCount")]
        public int PipeCount { get; set; }

        [JsonPropertyName("fittingCount")]
        public int FittingCount { get; set; }

        [JsonPropertyName("totalLengthM")]
        public double TotalLengthM { get; set; }

        [JsonPropertyName("okCount")]
        public int OkCount { get; set; }

        [JsonPropertyName("warningCount")]
        public int WarningCount { get; set; }

        [JsonPropertyName("criticalCount")]
        public int CriticalCount { get; set; }

        [JsonPropertyName("conformityRate")]
        public double ConformityRate { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO PRINCIPAL: VALIDAÇÃO DE ROTAS
    // ══════════════════════════════════════════════════════════════

    public interface IRouteValidationService
    {
        RouteValidationResult ValidarRotas(
            Document doc, RouteValidationConfig? config = null);

        RouteValidationResult ValidarRotasPorIds(
            Document doc, List<int> pipeIds,
            RouteValidationConfig? config = null);
    }

    public class RouteValidationService : IRouteValidationService
    {
        private readonly DynamoExecutionLogger _logger;

        public event Action<string>? OnProgress;
        public event Action<int, int>? OnItemProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public RouteValidationService()
        {
            _logger = new DynamoExecutionLogger();
        }

        public RouteValidationService(DynamoExecutionLogger logger)
        {
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO COMPLETA DO MODELO
        // ══════════════════════════════════════════════════════════

        public RouteValidationResult ValidarRotas(
            Document doc,
            RouteValidationConfig? config = null)
        {
            config ??= new RouteValidationConfig();

            var sw = Stopwatch.StartNew();
            var result = new RouteValidationResult();

            var log = DynamoExecutionLogger.Start(
                "RouteValidationService",
                JsonSerializer.Serialize(config, JsonOpts));

            try
            {
                EmitProgress("═══ VALIDAÇÃO DE ROTAS — INÍCIO ═══");

                // ── 1. Coletar pipes ──
                EmitProgress("Coletando pipes do modelo...");
                var pipes = new FilteredElementCollector(doc)
                    .OfClass(typeof(Pipe))
                    .Cast<Pipe>()
                    .ToList();

                result.TotalPipes = pipes.Count;

                // ── 2. Coletar fittings ──
                EmitProgress("Coletando fittings do modelo...");
                var fittings = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_PipeFitting)
                    .WhereElementIsNotElementType()
                    .ToElements();

                result.TotalFittings = fittings.Count;
                result.TotalElements = pipes.Count + fittings.Count;

                EmitProgress($"Encontrados: {pipes.Count} pipes, " +
                             $"{fittings.Count} fittings");

                if (result.TotalElements == 0)
                {
                    result.Status = "VAZIO";
                    result.Success = false;
                    DynamoExecutionLogger.MarkFailed(log,
                        "Nenhum pipe ou fitting encontrado.");
                    _logger.WriteExecutionLog(log);
                    result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                    return result;
                }

                // ── 3. Validar cada pipe ──
                EmitProgress("Validando pipes...");
                int idx = 0;

                foreach (var pipe in pipes)
                {
                    idx++;
                    OnItemProgress?.Invoke(idx, pipes.Count);

                    var item = ValidarPipe(doc, pipe, config);
                    result.Items.Add(item);
                }

                // ── 4. Validar fittings ──
                EmitProgress("Validando fittings...");

                foreach (var fitting in fittings)
                {
                    var item = ValidarFitting(doc, fitting, config);
                    result.Items.Add(item);
                }

                // ── 5. Detectar conflitos ──
                if (config.ValidarConflitos)
                {
                    EmitProgress("Detectando conflitos entre trechos...");
                    var conflicts = DetectarConflitos(doc, pipes, config);
                    result.Conflicts = conflicts;
                }

                // ── 6. Calcular estatísticas ──
                EmitProgress("Calculando estatísticas...");
                CalcularEstatisticas(result);

                // ── 7. Resumo por sistema ──
                GerarResumoPorSistema(result);

                // ── 8. Status final ──
                if (result.CriticalCount > 0)
                    result.Status = "CRITICO";
                else if (result.WarningCount > 0)
                    result.Status = "AJUSTES_NECESSARIOS";
                else
                    result.Status = "OK";

                result.Success = result.Status == "OK";
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                result.Timestamp = DateTime.UtcNow;

                DynamoExecutionLogger.MarkSuccess(log,
                    JsonSerializer.Serialize(BuildResumo(result), JsonOpts));
            }
            catch (Exception ex)
            {
                result.Status = "ERRO_FATAL";
                result.Success = false;

                DynamoExecutionLogger.MarkException(log, ex);
            }

            _logger.WriteExecutionLog(log);
            SalvarResultadoJson(result);

            EmitProgress($"═══ VALIDAÇÃO CONCLUÍDA: {result.Status} " +
                         $"({result.ExecutionTimeMs}ms) ═══");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO POR IDs ESPECÍFICOS
        // ══════════════════════════════════════════════════════════

        public RouteValidationResult ValidarRotasPorIds(
            Document doc,
            List<int> pipeIds,
            RouteValidationConfig? config = null)
        {
            config ??= new RouteValidationConfig();

            var sw = Stopwatch.StartNew();
            var result = new RouteValidationResult();

            EmitProgress($"Validando {pipeIds.Count} elementos por ID...");

            foreach (var id in pipeIds)
            {
                var elem = doc.GetElement(new ElementId(id));
                if (elem == null)
                {
                    result.Items.Add(new RouteValidationItem
                    {
                        ElementId = id,
                        ElementType = "DESCONHECIDO",
                        Status = "FALHA_CRITICA",
                        StatusEnum = RouteItemStatus.FalhaCritica,
                        Issues = new List<ValidationIssue>
                        {
                            new()
                            {
                                Code = "ELEM_NOT_FOUND",
                                Severity = "CRITICO",
                                Message = $"Elemento {id} não encontrado",
                                SuggestedAction = "Verificar se o elemento foi deletado"
                            }
                        }
                    });
                    continue;
                }

                if (elem is Pipe pipe)
                {
                    result.Items.Add(ValidarPipe(doc, pipe, config));
                    result.TotalPipes++;
                }
                else if (elem.Category?.Id.IntegerValue ==
                         (int)BuiltInCategory.OST_PipeFitting)
                {
                    result.Items.Add(ValidarFitting(doc, elem, config));
                    result.TotalFittings++;
                }
            }

            result.TotalElements = result.TotalPipes + result.TotalFittings;

            CalcularEstatisticas(result);
            GerarResumoPorSistema(result);

            if (result.CriticalCount > 0)
                result.Status = "CRITICO";
            else if (result.WarningCount > 0)
                result.Status = "AJUSTES_NECESSARIOS";
            else
                result.Status = "OK";

            result.Success = result.Status == "OK";
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE PIPE INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        private RouteValidationItem ValidarPipe(
            Document doc,
            Pipe pipe,
            RouteValidationConfig config)
        {
            var item = new RouteValidationItem
            {
                ElementId = pipe.Id.IntegerValue,
                ElementType = "Pipe"
            };

            // ── Extrair parâmetros ──
            var systemClassification = GetSystemClassification(pipe);
            item.SystemClassification = systemClassification;
            item.SystemName = GetSystemName(pipe);

            // Diâmetro
            var diamParam = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            item.DiameterMm = (diamParam?.AsDouble() ?? 0) * 304.8;

            // Comprimento
            var lenParam = pipe.get_Parameter(
                BuiltInParameter.CURVE_ELEM_LENGTH);
            item.LengthM = (lenParam?.AsDouble() ?? 0) * 0.3048;

            // Slope
            var slopeParam = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_SLOPE);
            var slopeFraction = slopeParam?.AsDouble() ?? 0;
            item.SlopePercent = slopeFraction * 100.0;

            // Altura (Z médio)
            var loc = pipe.Location as LocationCurve;
            if (loc?.Curve is Line line)
            {
                var p0 = line.GetEndPoint(0);
                var p1 = line.GetEndPoint(1);
                item.HeightM = ((p0.Z + p1.Z) / 2.0) * 0.3048;
            }

            // Level
            var levelParam = pipe.get_Parameter(
                BuiltInParameter.RBS_START_LEVEL_PARAM);
            if (levelParam != null)
            {
                var lvl = doc.GetElement(levelParam.AsElementId()) as Level;
                item.LevelName = lvl?.Name ?? "";
            }

            // Conectores
            if (pipe.ConnectorManager != null)
            {
                foreach (Connector c in pipe.ConnectorManager.Connectors)
                {
                    item.ConnectorsTotal++;
                    if (c.IsConnected)
                        item.ConnectorsConnected++;
                }
            }

            item.IsFullyConnected =
                item.ConnectorsTotal > 0 &&
                item.ConnectorsConnected == item.ConnectorsTotal;

            // ── Validações ──
            bool isSanitary = systemClassification.Contains("Sanitary") ||
                              systemClassification.Contains("Other");
            bool isColdWater = systemClassification.Contains("ColdWater") ||
                               systemClassification.Contains("Domestic");

            // V1 — Conectividade
            if (config.ValidarConectividade && !item.IsFullyConnected)
            {
                var disconnected = item.ConnectorsTotal - item.ConnectorsConnected;
                item.Issues.Add(new ValidationIssue
                {
                    Code = "CONN_INCOMPLETE",
                    Severity = disconnected == item.ConnectorsTotal
                        ? "CRITICO" : "AVISO",
                    Message = $"{disconnected} de {item.ConnectorsTotal} " +
                              "conector(es) não conectados",
                    SuggestedAction = "Executar 09_ConectarRede.dyn ou " +
                                      "conectar manualmente",
                    CurrentValue = $"{item.ConnectorsConnected}/{item.ConnectorsTotal}",
                    ExpectedValue = $"{item.ConnectorsTotal}/{item.ConnectorsTotal}"
                });
            }

            // V2 — Diâmetro
            if (config.ValidarDiametros)
            {
                double minDiam = isSanitary
                    ? config.EsDiametroMinMm
                    : config.AfDiametroMinMm;
                double maxDiam = isSanitary
                    ? config.EsDiametroMaxMm
                    : config.AfDiametroMaxMm;

                if (item.DiameterMm < minDiam)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "DIAM_BELOW_MIN",
                        Severity = "CRITICO",
                        Message = $"Diâmetro {item.DiameterMm:F0}mm < " +
                                  $"mínimo {minDiam:F0}mm",
                        SuggestedAction = $"Ajustar para ≥ {minDiam:F0}mm",
                        CurrentValue = $"{item.DiameterMm:F0}mm",
                        ExpectedValue = $"≥ {minDiam:F0}mm"
                    });
                }
                else if (item.DiameterMm > maxDiam)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "DIAM_ABOVE_MAX",
                        Severity = "AVISO",
                        Message = $"Diâmetro {item.DiameterMm:F0}mm > " +
                                  $"máximo {maxDiam:F0}mm",
                        SuggestedAction = "Verificar necessidade de ramal maior",
                        CurrentValue = $"{item.DiameterMm:F0}mm",
                        ExpectedValue = $"≤ {maxDiam:F0}mm"
                    });
                }

                if (item.DiameterMm <= 0)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "DIAM_ZERO",
                        Severity = "CRITICO",
                        Message = "Diâmetro não definido (0mm)",
                        SuggestedAction = "Atribuir diâmetro via unMEP ou " +
                                          "manualmente"
                    });
                }
            }

            // V3 — Slope (somente esgoto)
            if (config.ValidarSlope && isSanitary)
            {
                var expectedSlope = item.DiameterMm <= 75
                    ? config.EsSlopeMin75Percent
                    : config.EsSlopeMin100Percent;

                var tolerance = 1.0 - (config.ToleranciaSlopePercent / 100.0);
                var minAcceptable = expectedSlope * tolerance;

                // Verificar se é trecho horizontal (não prumada)
                bool isHorizontal = true;
                if (loc?.Curve is Line ln)
                {
                    var ep0 = ln.GetEndPoint(0);
                    var ep1 = ln.GetEndPoint(1);
                    var horizLen = Math.Sqrt(
                        Math.Pow(ep1.X - ep0.X, 2) +
                        Math.Pow(ep1.Y - ep0.Y, 2));
                    var vertLen = Math.Abs(ep1.Z - ep0.Z);

                    // Se vert > horiz → prumada, não valida slope
                    if (horizLen < 0.01 || vertLen / horizLen > 2.0)
                        isHorizontal = false;
                }

                if (isHorizontal && item.SlopePercent < minAcceptable)
                {
                    var severity = item.SlopePercent < expectedSlope * 0.5
                        ? "CRITICO" : "AVISO";

                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "SLOPE_BELOW_MIN",
                        Severity = severity,
                        Message = $"Slope {item.SlopePercent:F2}% < " +
                                  $"mínimo NBR 8160: {expectedSlope}% " +
                                  $"(Ø{item.DiameterMm:F0}mm)",
                        SuggestedAction = "Executar 07_AplicarInclinacao.dyn " +
                                          "ou ajustar manualmente",
                        CurrentValue = $"{item.SlopePercent:F2}%",
                        ExpectedValue = $"≥ {expectedSlope}%"
                    });
                }

                if (isHorizontal && item.SlopePercent > 5.0)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "SLOPE_ABOVE_MAX",
                        Severity = "AVISO",
                        Message = $"Slope {item.SlopePercent:F2}% > 5% " +
                                  "(pode causar separação do fluxo)",
                        SuggestedAction = "Reduzir slope para ≤ 5%",
                        CurrentValue = $"{item.SlopePercent:F2}%",
                        ExpectedValue = "≤ 5%"
                    });
                }
            }

            // V4 — Comprimento
            if (config.ValidarComprimento)
            {
                var minLenMm = config.ComprimentoMinMm;
                if (item.LengthM * 1000 < minLenMm)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "LEN_BELOW_MIN",
                        Severity = "AVISO",
                        Message = $"Comprimento {item.LengthM * 1000:F1}mm < " +
                                  $"mínimo {minLenMm}mm",
                        SuggestedAction = "Remover micro-trecho ou mesclar " +
                                          "com adjacente"
                    });
                }

                if (item.LengthM > config.ComprimentoMaxM)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "LEN_ABOVE_MAX",
                        Severity = "AVISO",
                        Message = $"Comprimento {item.LengthM:F2}m > " +
                                  $"máximo {config.ComprimentoMaxM}m",
                        SuggestedAction = "Verificar necessidade de caixa " +
                                          "de inspeção intermediária"
                    });
                }
            }

            // V5 — Altura
            if (config.ValidarAlturas)
            {
                if (item.HeightM < config.AlturaMinRamalM)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "HEIGHT_BELOW_MIN",
                        Severity = "AVISO",
                        Message = $"Altura {item.HeightM:F2}m < " +
                                  $"mínimo {config.AlturaMinRamalM}m",
                        SuggestedAction = "Verificar cota da laje e ajustar Z"
                    });
                }

                if (item.HeightM > config.AlturaMaxRamalM)
                {
                    item.Issues.Add(new ValidationIssue
                    {
                        Code = "HEIGHT_ABOVE_MAX",
                        Severity = "AVISO",
                        Message = $"Altura {item.HeightM:F2}m > " +
                                  $"máximo {config.AlturaMaxRamalM}m",
                        SuggestedAction = "Verificar se é prumada ou ramal"
                    });
                }
            }

            // ── Determinar status final do item ──
            DeterminarStatusItem(item);

            return item;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE FITTING INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        private RouteValidationItem ValidarFitting(
            Document doc,
            Element fitting,
            RouteValidationConfig config)
        {
            var item = new RouteValidationItem
            {
                ElementId = fitting.Id.IntegerValue,
                ElementType = "PipeFitting"
            };

            // Família / Tipo
            if (fitting is FamilyInstance fi)
            {
                item.SystemName = fi.Symbol?.FamilyName ?? "";

                // Level
                var lvlParam = fi.get_Parameter(
                    BuiltInParameter.FAMILY_LEVEL_PARAM);
                if (lvlParam != null)
                {
                    var lvl = doc.GetElement(lvlParam.AsElementId()) as Level;
                    item.LevelName = lvl?.Name ?? "";
                }

                // Location
                if (fi.Location is LocationPoint lp)
                    item.HeightM = lp.Point.Z * 0.3048;

                // Conectores
                var mep = fi.MEPModel;
                if (mep?.ConnectorManager != null)
                {
                    foreach (Connector c in mep.ConnectorManager.Connectors)
                    {
                        item.ConnectorsTotal++;
                        if (c.IsConnected)
                            item.ConnectorsConnected++;

                        // Extrair diâmetro do conector
                        if (c.Domain == Domain.DomainPiping && item.DiameterMm == 0)
                        {
                            try
                            {
                                item.DiameterMm = c.Radius * 2 * 304.8;
                            }
                            catch { }
                        }
                    }
                }

                item.IsFullyConnected =
                    item.ConnectorsTotal > 0 &&
                    item.ConnectorsConnected == item.ConnectorsTotal;
            }

            // V1 — Conectividade de fitting
            if (config.ValidarConectividade && !item.IsFullyConnected)
            {
                var disc = item.ConnectorsTotal - item.ConnectorsConnected;
                item.Issues.Add(new ValidationIssue
                {
                    Code = "FIT_CONN_INCOMPLETE",
                    Severity = disc >= 2 ? "CRITICO" : "AVISO",
                    Message = $"Fitting com {disc} de " +
                              $"{item.ConnectorsTotal} conector(es) " +
                              "não conectados",
                    SuggestedAction = "Conectar manualmente ou via " +
                                      "09_ConectarRede.dyn"
                });
            }

            // V2 — Fitting sem conectores (família incorreta)
            if (item.ConnectorsTotal == 0)
            {
                item.Issues.Add(new ValidationIssue
                {
                    Code = "FIT_NO_CONNECTORS",
                    Severity = "CRITICO",
                    Message = "Fitting sem conectores MEP",
                    SuggestedAction = "Substituir por família MEP válida"
                });
            }

            DeterminarStatusItem(item);
            return item;
        }

        // ══════════════════════════════════════════════════════════
        //  DETECÇÃO DE CONFLITOS
        // ══════════════════════════════════════════════════════════

        private List<ConflictItem> DetectarConflitos(
            Document doc,
            List<Pipe> pipes,
            RouteValidationConfig config)
        {
            var conflicts = new List<ConflictItem>();
            var minDistFt = config.DistanciaMinConflitMm / 304.8;

            // Otimização: só compara pipes do mesmo nível
            var porLevel = pipes
                .GroupBy(p =>
                {
                    var lp = p.get_Parameter(
                        BuiltInParameter.RBS_START_LEVEL_PARAM);
                    return lp?.AsElementId() ?? ElementId.InvalidElementId;
                });

            foreach (var levelGroup in porLevel)
            {
                var levelPipes = levelGroup.ToList();

                for (int i = 0; i < levelPipes.Count; i++)
                {
                    var loc1 = levelPipes[i].Location as LocationCurve;
                    if (loc1?.Curve == null) continue;

                    for (int j = i + 1; j < levelPipes.Count; j++)
                    {
                        var loc2 = levelPipes[j].Location as LocationCurve;
                        if (loc2?.Curve == null) continue;

                        // Verificar distância mínima entre centros
                        var mid1 = loc1.Curve.Evaluate(0.5, true);
                        var mid2 = loc2.Curve.Evaluate(0.5, true);
                        var dist = mid1.DistanceTo(mid2);

                        if (dist < minDistFt && dist > 0.001)
                        {
                            // Verificar se são do mesmo sistema (não é conflito)
                            var sys1 = GetSystemName(levelPipes[i]);
                            var sys2 = GetSystemName(levelPipes[j]);

                            string conflictType;
                            if (dist < 0.001)
                                conflictType = "SOBREPOSICAO";
                            else if (sys1 == sys2)
                                conflictType = "PROXIMIDADE_MESMO_SISTEMA";
                            else
                                conflictType = "INTERFERENCIA_CRUZADA";

                            conflicts.Add(new ConflictItem
                            {
                                ElementA = levelPipes[i].Id.IntegerValue,
                                ElementB = levelPipes[j].Id.IntegerValue,
                                DistanceMm = Math.Round(dist * 304.8, 1),
                                ConflictType = conflictType,
                                Location = $"({mid1.X * 0.3048:F2}, " +
                                           $"{mid1.Y * 0.3048:F2}, " +
                                           $"{mid1.Z * 0.3048:F2})m"
                            });
                        }
                    }
                }
            }

            EmitProgress($"Conflitos detectados: {conflicts.Count}");
            return conflicts;
        }

        // ══════════════════════════════════════════════════════════
        //  ESTATÍSTICAS E RESUMO
        // ══════════════════════════════════════════════════════════

        private static void CalcularEstatisticas(RouteValidationResult result)
        {
            result.OkCount = result.Items
                .Count(i => i.StatusEnum == RouteItemStatus.Ok);
            result.WarningCount = result.Items
                .Count(i => i.StatusEnum == RouteItemStatus.AjusteNecessario);
            result.CriticalCount = result.Items
                .Count(i => i.StatusEnum == RouteItemStatus.FalhaCritica);

            var total = result.Items.Count;
            if (total == 0) return;

            // Conectividade
            var withConnectors = result.Items
                .Where(i => i.ConnectorsTotal > 0).ToList();
            result.ConnectivityRate = withConnectors.Count > 0
                ? Math.Round(
                    withConnectors.Count(i => i.IsFullyConnected) * 100.0 /
                    withConnectors.Count, 1)
                : 100;

            // Diâmetro
            var pipesWithDiam = result.Items
                .Where(i => i.ElementType == "Pipe" && i.DiameterMm > 0)
                .ToList();
            var diamOk = pipesWithDiam.Count(i =>
                !i.Issues.Any(iss =>
                    iss.Code.StartsWith("DIAM_")));
            result.DiameterConformityRate = pipesWithDiam.Count > 0
                ? Math.Round(diamOk * 100.0 / pipesWithDiam.Count, 1)
                : 100;

            // Slope
            var pipesWithSlope = result.Items
                .Where(i => i.ElementType == "Pipe" &&
                            i.SystemClassification.Contains("Sanitary"))
                .ToList();
            var slopeOk = pipesWithSlope.Count(i =>
                !i.Issues.Any(iss =>
                    iss.Code.StartsWith("SLOPE_")));
            result.SlopeConformityRate = pipesWithSlope.Count > 0
                ? Math.Round(slopeOk * 100.0 / pipesWithSlope.Count, 1)
                : 100;

            // Overall
            result.OverallConformityRate = Math.Round(
                result.OkCount * 100.0 / total, 1);
        }

        private static void GerarResumoPorSistema(RouteValidationResult result)
        {
            var groups = result.Items
                .GroupBy(i => string.IsNullOrEmpty(i.SystemName)
                    ? "Sem Sistema"
                    : i.SystemName);

            foreach (var group in groups)
            {
                var items = group.ToList();
                var summary = new SystemSummary
                {
                    SystemName = group.Key,
                    PipeCount = items.Count(i => i.ElementType == "Pipe"),
                    FittingCount = items.Count(i => i.ElementType == "PipeFitting"),
                    TotalLengthM = Math.Round(
                        items.Where(i => i.ElementType == "Pipe")
                            .Sum(i => i.LengthM), 2),
                    OkCount = items.Count(i =>
                        i.StatusEnum == RouteItemStatus.Ok),
                    WarningCount = items.Count(i =>
                        i.StatusEnum == RouteItemStatus.AjusteNecessario),
                    CriticalCount = items.Count(i =>
                        i.StatusEnum == RouteItemStatus.FalhaCritica)
                };

                summary.ConformityRate = items.Count > 0
                    ? Math.Round(summary.OkCount * 100.0 / items.Count, 1)
                    : 100;

                result.SummaryBySystem[group.Key] = summary;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private static void DeterminarStatusItem(RouteValidationItem item)
        {
            if (item.Issues.Any(i => i.Severity == "CRITICO"))
            {
                item.StatusEnum = RouteItemStatus.FalhaCritica;
                item.Status = "FALHA_CRITICA";
            }
            else if (item.Issues.Any(i => i.Severity == "AVISO"))
            {
                item.StatusEnum = RouteItemStatus.AjusteNecessario;
                item.Status = "AJUSTE_NECESSARIO";
            }
            else
            {
                item.StatusEnum = RouteItemStatus.Ok;
                item.Status = "OK";
            }
        }

        private static string GetSystemClassification(Pipe pipe)
        {
            try
            {
                var sysParam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                if (sysParam != null)
                {
                    var sysType = pipe.Document.GetElement(
                        sysParam.AsElementId()) as PipingSystemType;
                    return sysType?.SystemClassification.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        private static string GetSystemName(Pipe pipe)
        {
            try
            {
                var sysParam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                if (sysParam != null)
                {
                    var sysType = pipe.Document.GetElement(
                        sysParam.AsElementId());
                    return sysType?.Name ?? "";
                }
            }
            catch { }
            return "";
        }

        private static object BuildResumo(RouteValidationResult result)
        {
            return new
            {
                result.Status,
                result.TotalPipes,
                result.TotalFittings,
                result.OkCount,
                result.WarningCount,
                result.CriticalCount,
                result.ConnectivityRate,
                result.DiameterConformityRate,
                result.SlopeConformityRate,
                result.OverallConformityRate,
                Conflicts = result.Conflicts.Count,
                result.ExecutionTimeMs
            };
        }

        private void SalvarResultadoJson(RouteValidationResult resultado)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Validation");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"route_validation_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(resultado, JsonOpts);
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
