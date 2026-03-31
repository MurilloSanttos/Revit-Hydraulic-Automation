using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Revit2026.Modules.DynamoIntegration.Logging;

namespace Revit2026.Modules.UnMEP
{
    // ══════════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════════

    public enum AdjustmentType
    {
        MoveEndpoint,
        ChangeHeight,
        ChangeSlope,
        ChangeOffset,
        ChangeDiameter,
        Reconnect,
        Reroute,
        Delete
    }

    // ══════════════════════════════════════════════════════════════
    //  SNAPSHOT — ESTADO ANTERIOR (UNDO)
    // ══════════════════════════════════════════════════════════════

    public class PipeSnapshot
    {
        [JsonPropertyName("elementId")]
        public long ElementId { get; set; }

        [JsonPropertyName("startPoint")]
        public double[] StartPoint { get; set; } = Array.Empty<double>();

        [JsonPropertyName("endPoint")]
        public double[] EndPoint { get; set; } = Array.Empty<double>();

        [JsonPropertyName("diameterMm")]
        public double DiameterMm { get; set; }

        [JsonPropertyName("slopePercent")]
        public double SlopePercent { get; set; }

        [JsonPropertyName("levelId")]
        public long LevelId { get; set; }

        [JsonPropertyName("systemName")]
        public string SystemName { get; set; } = "";

        [JsonPropertyName("connectedIds")]
        public List<long> ConnectedIds { get; set; } = new();

        [JsonPropertyName("capturedAt")]
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    }

    // ══════════════════════════════════════════════════════════════
    //  AJUSTE INDIVIDUAL
    // ══════════════════════════════════════════════════════════════

    public class RouteAdjustment
    {
        [JsonPropertyName("adjustmentId")]
        public string AdjustmentId { get; set; } = Guid.NewGuid().ToString("N")[..8];

        [JsonPropertyName("elementId")]
        public long ElementId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("typeEnum")]
        [JsonIgnore]
        public AdjustmentType TypeEnum { get; set; }

        [JsonPropertyName("before")]
        public PipeSnapshot? Before { get; set; }

        [JsonPropertyName("after")]
        public PipeSnapshot? After { get; set; }

        [JsonPropertyName("applied")]
        public bool Applied { get; set; }

        [JsonPropertyName("reverted")]
        public bool Reverted { get; set; }

        [JsonPropertyName("validationStatus")]
        public string ValidationStatus { get; set; } = "PENDENTE";

        [JsonPropertyName("issues")]
        public List<ValidationIssue> Issues { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = Environment.UserName;
    }

    // ══════════════════════════════════════════════════════════════
    //  PARÂMETROS DE AJUSTE
    // ══════════════════════════════════════════════════════════════

    public class MoveEndpointParams
    {
        public long ElementId { get; set; }
        public int EndpointIndex { get; set; } // 0 = start, 1 = end
        public XYZ NewPosition { get; set; } = XYZ.Zero;
    }

    public class ChangeHeightParams
    {
        public long ElementId { get; set; }
        public double NewHeightM { get; set; }
        public bool MaintainSlope { get; set; } = true;
    }

    public class ChangeSlopeParams
    {
        public long ElementId { get; set; }
        public double NewSlopePercent { get; set; }
        public int AnchorEnd { get; set; } // 0 = fixar start, 1 = fixar end
    }

    public class ChangeOffsetParams
    {
        public long ElementId { get; set; }
        public double OffsetXM { get; set; }
        public double OffsetYM { get; set; }
    }

    public class ChangeDiameterParams
    {
        public long ElementId { get; set; }
        public double NewDiameterMm { get; set; }
    }

    public class ReconnectParams
    {
        public long ElementId { get; set; }
        public long TargetElementId { get; set; }
        public int ConnectorIndex { get; set; } // qual conector do pipe
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DO AJUSTE
    // ══════════════════════════════════════════════════════════════

    public class AdjustmentSessionResult
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N")[..12];

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "PENDENTE";

        [JsonPropertyName("adjustmentsApplied")]
        public int AdjustmentsApplied { get; set; }

        [JsonPropertyName("adjustmentsReverted")]
        public int AdjustmentsReverted { get; set; }

        [JsonPropertyName("adjustmentsFailed")]
        public int AdjustmentsFailed { get; set; }

        [JsonPropertyName("revalidationPassed")]
        public bool RevalidationPassed { get; set; }

        [JsonPropertyName("adjustments")]
        public List<RouteAdjustment> Adjustments { get; set; } = new();

        [JsonPropertyName("disconnectedElements")]
        public List<long> DisconnectedElements { get; set; } = new();

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO PRINCIPAL: AJUSTE MANUAL DE ROTAS
    // ══════════════════════════════════════════════════════════════

    public interface IRouteAdjustmentService
    {
        List<RouteValidationItem> ObterTrechosProblematicos(
            RouteValidationResult validacao);

        RouteAdjustment MoverEndpoint(
            Document doc, MoveEndpointParams param);

        RouteAdjustment AlterarAltura(
            Document doc, ChangeHeightParams param);

        RouteAdjustment AlterarSlope(
            Document doc, ChangeSlopeParams param);

        RouteAdjustment AlterarOffset(
            Document doc, ChangeOffsetParams param);

        RouteAdjustment AlterarDiametro(
            Document doc, ChangeDiameterParams param);

        RouteAdjustment Reconectar(
            Document doc, ReconnectParams param);

        bool ReverterAjuste(Document doc, string adjustmentId);

        AdjustmentSessionResult FinalizarSessao(Document doc);
    }

    public class RouteAdjustmentService : IRouteAdjustmentService
    {
        private readonly DynamoExecutionLogger _logger;
        private readonly RouteValidationService _validator;
        private readonly List<RouteAdjustment> _sessionAdjustments = new();
        private readonly Dictionary<string, PipeSnapshot> _undoStack = new();
        private readonly string _sessionId;

        public event Action<string>? OnProgress;
        public event Action<RouteAdjustment>? OnAdjustmentApplied;
        public event Action<string>? OnAdjustmentReverted;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public RouteAdjustmentService()
        {
            _logger = new DynamoExecutionLogger();
            _validator = new RouteValidationService();
            _sessionId = Guid.NewGuid().ToString("N")[..12];
        }

        public RouteAdjustmentService(
            DynamoExecutionLogger logger,
            RouteValidationService validator)
        {
            _logger = logger;
            _validator = validator;
            _sessionId = Guid.NewGuid().ToString("N")[..12];
        }

        public int AjustesPendentes => _sessionAdjustments
            .Count(a => a.Applied && !a.Reverted);

        // ══════════════════════════════════════════════════════════
        //  1. DETECÇÃO DE TRECHOS PROBLEMÁTICOS
        // ══════════════════════════════════════════════════════════

        public List<RouteValidationItem> ObterTrechosProblematicos(
            RouteValidationResult validacao)
        {
            return validacao.Items
                .Where(i =>
                    i.StatusEnum == RouteItemStatus.AjusteNecessario ||
                    i.StatusEnum == RouteItemStatus.FalhaCritica)
                .OrderByDescending(i =>
                    i.StatusEnum == RouteItemStatus.FalhaCritica ? 1 : 0)
                .ThenBy(i => i.ElementId)
                .ToList();
        }

        // ══════════════════════════════════════════════════════════
        //  2. MOVER ENDPOINT
        // ══════════════════════════════════════════════════════════

        public RouteAdjustment MoverEndpoint(
            Document doc,
            MoveEndpointParams param)
        {
            var adj = CriarAjuste(param.ElementId, AdjustmentType.MoveEndpoint);

            try
            {
                var pipe = doc.GetElement(new ElementId(param.ElementId)) as Pipe;
                if (pipe == null)
                    return MarcarFalha(adj, "Pipe não encontrado");

                // Snapshot antes
                adj.Before = CapturarSnapshot(doc, pipe);

                var loc = pipe.Location as LocationCurve;
                if (loc?.Curve is not Line line)
                    return MarcarFalha(adj, "Pipe sem LocationCurve/Line");

                // Validar que não desconecta
                var outroEnd = param.EndpointIndex == 0
                    ? line.GetEndPoint(1)
                    : line.GetEndPoint(0);

                var newLine = param.EndpointIndex == 0
                    ? Line.CreateBound(param.NewPosition, outroEnd)
                    : Line.CreateBound(line.GetEndPoint(0), param.NewPosition);

                // Verificar comprimento mínimo
                if (newLine.Length < 0.01)
                    return MarcarFalha(adj,
                        "Novo trecho seria < 3mm — operação cancelada");

                using var trans = new Transaction(doc,
                    $"Ajuste Manual - MoveEndpoint [{adj.AdjustmentId}]");
                trans.Start();

                loc.Curve = newLine;

                trans.Commit();

                // Snapshot depois
                adj.After = CapturarSnapshot(doc, pipe);
                adj.Applied = true;

                // Revalidar
                RevalidarTrecho(doc, adj);

                EmitProgress($"Endpoint movido: Pipe {param.ElementId}");
                RegistrarAjuste(adj);
            }
            catch (Exception ex)
            {
                MarcarFalha(adj, ex.Message);
            }

            return adj;
        }

        // ══════════════════════════════════════════════════════════
        //  3. ALTERAR ALTURA (Z)
        // ══════════════════════════════════════════════════════════

        public RouteAdjustment AlterarAltura(
            Document doc,
            ChangeHeightParams param)
        {
            var adj = CriarAjuste(param.ElementId, AdjustmentType.ChangeHeight);

            try
            {
                var pipe = doc.GetElement(new ElementId(param.ElementId)) as Pipe;
                if (pipe == null)
                    return MarcarFalha(adj, "Pipe não encontrado");

                adj.Before = CapturarSnapshot(doc, pipe);

                var loc = pipe.Location as LocationCurve;
                if (loc?.Curve is not Line line)
                    return MarcarFalha(adj, "Pipe sem LocationCurve/Line");

                var p0 = line.GetEndPoint(0);
                var p1 = line.GetEndPoint(1);
                var newZFt = param.NewHeightM / 0.3048;

                Line newLine;
                if (param.MaintainSlope)
                {
                    // Manter o ΔZ igual, só mover verticalmente
                    var dz = p1.Z - p0.Z;
                    newLine = Line.CreateBound(
                        new XYZ(p0.X, p0.Y, newZFt),
                        new XYZ(p1.X, p1.Y, newZFt + dz));
                }
                else
                {
                    // Ambos endpoints na mesma Z
                    newLine = Line.CreateBound(
                        new XYZ(p0.X, p0.Y, newZFt),
                        new XYZ(p1.X, p1.Y, newZFt));
                }

                using var trans = new Transaction(doc,
                    $"Ajuste Manual - AlterarAltura [{adj.AdjustmentId}]");
                trans.Start();

                loc.Curve = newLine;

                trans.Commit();

                adj.After = CapturarSnapshot(doc, pipe);
                adj.Applied = true;

                RevalidarTrecho(doc, adj);
                EmitProgress($"Altura alterada: Pipe {param.ElementId} → " +
                             $"{param.NewHeightM:F2}m");
                RegistrarAjuste(adj);
            }
            catch (Exception ex)
            {
                MarcarFalha(adj, ex.Message);
            }

            return adj;
        }

        // ══════════════════════════════════════════════════════════
        //  4. ALTERAR SLOPE
        // ══════════════════════════════════════════════════════════

        public RouteAdjustment AlterarSlope(
            Document doc,
            ChangeSlopeParams param)
        {
            var adj = CriarAjuste(param.ElementId, AdjustmentType.ChangeSlope);

            try
            {
                var pipe = doc.GetElement(new ElementId(param.ElementId)) as Pipe;
                if (pipe == null)
                    return MarcarFalha(adj, "Pipe não encontrado");

                adj.Before = CapturarSnapshot(doc, pipe);

                var loc = pipe.Location as LocationCurve;
                if (loc?.Curve is not Line line)
                    return MarcarFalha(adj, "Pipe sem LocationCurve/Line");

                var p0 = line.GetEndPoint(0);
                var p1 = line.GetEndPoint(1);

                var horizLen = Math.Sqrt(
                    Math.Pow(p1.X - p0.X, 2) +
                    Math.Pow(p1.Y - p0.Y, 2));

                if (horizLen < 0.01)
                    return MarcarFalha(adj,
                        "Trecho vertical — slope não aplicável");

                var slopeFraction = param.NewSlopePercent / 100.0;
                var newDrop = horizLen * slopeFraction;

                XYZ newP0, newP1;
                if (param.AnchorEnd == 0)
                {
                    // Fixar start, ajustar end
                    newP0 = p0;
                    newP1 = new XYZ(p1.X, p1.Y, p0.Z - newDrop);
                }
                else
                {
                    // Fixar end, ajustar start
                    newP0 = new XYZ(p0.X, p0.Y, p1.Z + newDrop);
                    newP1 = p1;
                }

                var newLine = Line.CreateBound(newP0, newP1);

                using var trans = new Transaction(doc,
                    $"Ajuste Manual - AlterarSlope [{adj.AdjustmentId}]");
                trans.Start();

                loc.Curve = newLine;

                // Atualizar parâmetro slope
                var slopeParam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_SLOPE);
                if (slopeParam != null && !slopeParam.IsReadOnly)
                    slopeParam.Set(slopeFraction);

                trans.Commit();

                adj.After = CapturarSnapshot(doc, pipe);
                adj.Applied = true;

                RevalidarTrecho(doc, adj);
                EmitProgress($"Slope alterado: Pipe {param.ElementId} → " +
                             $"{param.NewSlopePercent}%");
                RegistrarAjuste(adj);
            }
            catch (Exception ex)
            {
                MarcarFalha(adj, ex.Message);
            }

            return adj;
        }

        // ══════════════════════════════════════════════════════════
        //  5. ALTERAR OFFSET (X, Y)
        // ══════════════════════════════════════════════════════════

        public RouteAdjustment AlterarOffset(
            Document doc,
            ChangeOffsetParams param)
        {
            var adj = CriarAjuste(param.ElementId, AdjustmentType.ChangeOffset);

            try
            {
                var pipe = doc.GetElement(new ElementId(param.ElementId)) as Pipe;
                if (pipe == null)
                    return MarcarFalha(adj, "Pipe não encontrado");

                adj.Before = CapturarSnapshot(doc, pipe);

                var loc = pipe.Location as LocationCurve;
                if (loc?.Curve is not Line line)
                    return MarcarFalha(adj, "Pipe sem LocationCurve/Line");

                var dxFt = param.OffsetXM / 0.3048;
                var dyFt = param.OffsetYM / 0.3048;
                var translation = new XYZ(dxFt, dyFt, 0);

                using var trans = new Transaction(doc,
                    $"Ajuste Manual - AlterarOffset [{adj.AdjustmentId}]");
                trans.Start();

                ElementTransformUtils.MoveElement(doc, pipe.Id, translation);

                trans.Commit();

                adj.After = CapturarSnapshot(doc, pipe);
                adj.Applied = true;

                RevalidarTrecho(doc, adj);
                EmitProgress($"Offset aplicado: Pipe {param.ElementId} → " +
                             $"ΔX={param.OffsetXM}m, ΔY={param.OffsetYM}m");
                RegistrarAjuste(adj);
            }
            catch (Exception ex)
            {
                MarcarFalha(adj, ex.Message);
            }

            return adj;
        }

        // ══════════════════════════════════════════════════════════
        //  6. ALTERAR DIÂMETRO
        // ══════════════════════════════════════════════════════════

        public RouteAdjustment AlterarDiametro(
            Document doc,
            ChangeDiameterParams param)
        {
            var adj = CriarAjuste(param.ElementId, AdjustmentType.ChangeDiameter);

            try
            {
                var pipe = doc.GetElement(new ElementId(param.ElementId)) as Pipe;
                if (pipe == null)
                    return MarcarFalha(adj, "Pipe não encontrado");

                adj.Before = CapturarSnapshot(doc, pipe);

                var diamFt = param.NewDiameterMm / 304.8;

                using var trans = new Transaction(doc,
                    $"Ajuste Manual - AlterarDiametro [{adj.AdjustmentId}]");
                trans.Start();

                var diamParam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (diamParam == null || diamParam.IsReadOnly)
                {
                    trans.RollBack();
                    return MarcarFalha(adj,
                        "Parâmetro de diâmetro é read-only");
                }

                diamParam.Set(diamFt);

                trans.Commit();

                adj.After = CapturarSnapshot(doc, pipe);
                adj.Applied = true;

                RevalidarTrecho(doc, adj);
                EmitProgress($"Diâmetro alterado: Pipe {param.ElementId} → " +
                             $"{param.NewDiameterMm}mm");
                RegistrarAjuste(adj);
            }
            catch (Exception ex)
            {
                MarcarFalha(adj, ex.Message);
            }

            return adj;
        }

        // ══════════════════════════════════════════════════════════
        //  7. RECONECTAR
        // ══════════════════════════════════════════════════════════

        public RouteAdjustment Reconectar(
            Document doc,
            ReconnectParams param)
        {
            var adj = CriarAjuste(param.ElementId, AdjustmentType.Reconnect);

            try
            {
                var pipe = doc.GetElement(new ElementId(param.ElementId)) as Pipe;
                if (pipe == null)
                    return MarcarFalha(adj, "Pipe não encontrado");

                var target = doc.GetElement(new ElementId(param.TargetElementId));
                if (target == null)
                    return MarcarFalha(adj, "Target não encontrado");

                adj.Before = CapturarSnapshot(doc, pipe);

                // Encontrar conectores
                var pipeConn = GetConnectorByIndex(pipe, param.ConnectorIndex);
                if (pipeConn == null)
                    return MarcarFalha(adj,
                        $"Conector {param.ConnectorIndex} não encontrado no pipe");

                var targetConn = FindNearestConnector(target, pipeConn.Origin);
                if (targetConn == null)
                    return MarcarFalha(adj,
                        "Nenhum conector disponível no target");

                using var trans = new Transaction(doc,
                    $"Ajuste Manual - Reconectar [{adj.AdjustmentId}]");
                trans.Start();

                // Desconectar existente
                if (pipeConn.IsConnected)
                {
                    foreach (Connector other in pipeConn.AllRefs)
                    {
                        try { pipeConn.DisconnectFrom(other); }
                        catch { }
                    }
                }

                // Conectar ao target
                if (!targetConn.IsConnected)
                {
                    pipeConn.ConnectTo(targetConn);
                }
                else
                {
                    trans.RollBack();
                    return MarcarFalha(adj,
                        "Conector do target já está conectado");
                }

                trans.Commit();

                adj.After = CapturarSnapshot(doc, pipe);
                adj.Applied = true;

                RevalidarTrecho(doc, adj);
                EmitProgress($"Reconectado: Pipe {param.ElementId} → " +
                             $"Target {param.TargetElementId}");
                RegistrarAjuste(adj);
            }
            catch (Exception ex)
            {
                MarcarFalha(adj, ex.Message);
            }

            return adj;
        }

        // ══════════════════════════════════════════════════════════
        //  8. REVERTER AJUSTE (UNDO)
        // ══════════════════════════════════════════════════════════

        public bool ReverterAjuste(Document doc, string adjustmentId)
        {
            var adj = _sessionAdjustments
                .FirstOrDefault(a => a.AdjustmentId == adjustmentId);

            if (adj == null || !adj.Applied || adj.Reverted)
            {
                EmitProgress($"Ajuste {adjustmentId} não encontrado ou " +
                             "já revertido");
                return false;
            }

            if (adj.Before == null)
            {
                EmitProgress($"Sem snapshot para reverter {adjustmentId}");
                return false;
            }

            try
            {
                var pipe = doc.GetElement(
                    new ElementId(adj.ElementId)) as Pipe;
                if (pipe == null) return false;

                using var trans = new Transaction(doc,
                    $"Reverter Ajuste [{adjustmentId}]");
                trans.Start();

                // Restaurar geometria
                var loc = pipe.Location as LocationCurve;
                if (loc != null && adj.Before.StartPoint.Length == 3 &&
                    adj.Before.EndPoint.Length == 3)
                {
                    var restoreP0 = new XYZ(
                        adj.Before.StartPoint[0],
                        adj.Before.StartPoint[1],
                        adj.Before.StartPoint[2]);
                    var restoreP1 = new XYZ(
                        adj.Before.EndPoint[0],
                        adj.Before.EndPoint[1],
                        adj.Before.EndPoint[2]);

                    if (restoreP0.DistanceTo(restoreP1) > 0.001)
                        loc.Curve = Line.CreateBound(restoreP0, restoreP1);
                }

                // Restaurar diâmetro
                if (adj.Before.DiameterMm > 0)
                {
                    var diamParam = pipe.get_Parameter(
                        BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                    if (diamParam != null && !diamParam.IsReadOnly)
                        diamParam.Set(adj.Before.DiameterMm / 304.8);
                }

                // Restaurar slope
                var slopeParam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_SLOPE);
                if (slopeParam != null && !slopeParam.IsReadOnly)
                    slopeParam.Set(adj.Before.SlopePercent / 100.0);

                trans.Commit();

                adj.Reverted = true;
                adj.ValidationStatus = "REVERTIDO";

                EmitProgress($"Ajuste {adjustmentId} revertido com sucesso");
                OnAdjustmentReverted?.Invoke(adjustmentId);

                return true;
            }
            catch (Exception ex)
            {
                EmitProgress($"Falha ao reverter {adjustmentId}: {ex.Message}");
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  9. REVERTER TODOS
        // ══════════════════════════════════════════════════════════

        public int ReverterTodos(Document doc)
        {
            int reverted = 0;

            // Reverter na ordem inversa
            var toRevert = _sessionAdjustments
                .Where(a => a.Applied && !a.Reverted)
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            foreach (var adj in toRevert)
            {
                if (ReverterAjuste(doc, adj.AdjustmentId))
                    reverted++;
            }

            EmitProgress($"Revertidos {reverted} de {toRevert.Count} ajustes");
            return reverted;
        }

        // ══════════════════════════════════════════════════════════
        //  10. FINALIZAR SESSÃO
        // ══════════════════════════════════════════════════════════

        public AdjustmentSessionResult FinalizarSessao(Document doc)
        {
            var sw = Stopwatch.StartNew();

            EmitProgress("═══ FINALIZANDO SESSÃO DE AJUSTES ═══");

            var result = new AdjustmentSessionResult
            {
                SessionId = _sessionId,
                Adjustments = _sessionAdjustments.ToList(),
                AdjustmentsApplied = _sessionAdjustments
                    .Count(a => a.Applied && !a.Reverted),
                AdjustmentsReverted = _sessionAdjustments
                    .Count(a => a.Reverted),
                AdjustmentsFailed = _sessionAdjustments
                    .Count(a => !a.Applied)
            };

            // Revalidação completa dos trechos ajustados
            var adjustedIds = _sessionAdjustments
                .Where(a => a.Applied && !a.Reverted)
                .Select(a => a.ElementId)
                .Distinct()
                .ToList();

            if (adjustedIds.Count > 0)
            {
                EmitProgress($"Revalidando {adjustedIds.Count} " +
                             "trechos ajustados...");

                var validacao = _validator.ValidarRotasPorIds(
                    doc, adjustedIds);

                result.RevalidationPassed =
                    validacao.CriticalCount == 0;

                // Verificar desconexões causadas
                var disconnected = validacao.Items
                    .Where(i => !i.IsFullyConnected)
                    .Select(i => i.ElementId)
                    .ToList();
                result.DisconnectedElements = disconnected;

                if (disconnected.Count > 0)
                {
                    EmitProgress($"⚠️ {disconnected.Count} elemento(s) " +
                                 "desconectados após ajustes");
                }
            }
            else
            {
                result.RevalidationPassed = true;
            }

            // Status final
            if (result.AdjustmentsFailed > 0)
                result.Status = "COM_FALHAS";
            else if (!result.RevalidationPassed)
                result.Status = "VALIDACAO_FALHOU";
            else if (result.DisconnectedElements.Count > 0)
                result.Status = "DESCONEXOES_DETECTADAS";
            else
                result.Status = "OK";

            result.Success = result.Status == "OK";
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            result.Timestamp = DateTime.UtcNow;

            // Persistir
            SalvarSessaoJson(result);

            // Log
            var log = DynamoExecutionLogger.Start(
                "RouteAdjustmentService",
                JsonSerializer.Serialize(new
                {
                    result.SessionId,
                    result.AdjustmentsApplied,
                    result.AdjustmentsReverted,
                    result.AdjustmentsFailed
                }, JsonOpts));

            if (result.Success)
                DynamoExecutionLogger.MarkSuccess(log,
                    JsonSerializer.Serialize(result, JsonOpts));
            else
                DynamoExecutionLogger.MarkFailed(log, result.Status);

            _logger.WriteExecutionLog(log);

            EmitProgress($"═══ SESSÃO FINALIZADA: {result.Status} " +
                         $"({result.ExecutionTimeMs}ms) ═══");

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS — SNAPSHOT
        // ══════════════════════════════════════════════════════════

        private static PipeSnapshot CapturarSnapshot(Document doc, Pipe pipe)
        {
            var snapshot = new PipeSnapshot
            {
                ElementId = pipe.Id.Value,
                CapturedAt = DateTime.UtcNow
            };

            // Geometria
            var loc = pipe.Location as LocationCurve;
            if (loc?.Curve is Line line)
            {
                var p0 = line.GetEndPoint(0);
                var p1 = line.GetEndPoint(1);
                snapshot.StartPoint = new[] { p0.X, p0.Y, p0.Z };
                snapshot.EndPoint = new[] { p1.X, p1.Y, p1.Z };
            }

            // Diâmetro
            var diamParam = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            snapshot.DiameterMm = (diamParam?.AsDouble() ?? 0) * 304.8;

            // Slope
            var slopeParam = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_SLOPE);
            snapshot.SlopePercent = (slopeParam?.AsDouble() ?? 0) * 100;

            // Level
            var levelParam = pipe.get_Parameter(
                BuiltInParameter.RBS_START_LEVEL_PARAM);
            snapshot.LevelId = levelParam?.AsElementId().Value ?? -1;

            // Sistema
            var sysParam = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            if (sysParam != null)
            {
                var sysType = doc.GetElement(sysParam.AsElementId());
                snapshot.SystemName = sysType?.Name ?? "";
            }

            // Conectados
            if (pipe.ConnectorManager != null)
            {
                foreach (Connector c in pipe.ConnectorManager.Connectors)
                {
                    if (!c.IsConnected) continue;
                    foreach (Connector other in c.AllRefs)
                    {
                        if (other.Owner.Id != pipe.Id)
                            snapshot.ConnectedIds.Add(
                                other.Owner.Id.Value);
                    }
                }
            }

            return snapshot;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS — CONECTORES
        // ══════════════════════════════════════════════════════════

        private static Connector? GetConnectorByIndex(
            Pipe pipe, int index)
        {
            if (pipe.ConnectorManager == null) return null;

            int i = 0;
            foreach (Connector c in pipe.ConnectorManager.Connectors)
            {
                if (i == index) return c;
                i++;
            }
            return null;
        }

        private static Connector? FindNearestConnector(
            Element target, XYZ origin)
        {
            ConnectorSet? connectors = null;

            if (target is Pipe p)
                connectors = p.ConnectorManager?.Connectors;
            else if (target is FamilyInstance fi)
                connectors = fi.MEPModel?.ConnectorManager?.Connectors;

            if (connectors == null) return null;

            Connector? best = null;
            double bestDist = double.MaxValue;

            foreach (Connector c in connectors)
            {
                if (c.IsConnected) continue;
                var d = c.Origin.DistanceTo(origin);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }

            return best;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS — VALIDAÇÃO E REGISTRO
        // ══════════════════════════════════════════════════════════

        private void RevalidarTrecho(Document doc, RouteAdjustment adj)
        {
            try
            {
                var valResult = _validator.ValidarRotasPorIds(
                    doc, new List<long> { adj.ElementId });

                var item = valResult.Items.FirstOrDefault();
                if (item != null)
                {
                    adj.ValidationStatus = item.Status;
                    adj.Issues = item.Issues;
                }
                else
                {
                    adj.ValidationStatus = "NAO_ENCONTRADO";
                }
            }
            catch
            {
                adj.ValidationStatus = "ERRO_VALIDACAO";
            }
        }

        private RouteAdjustment CriarAjuste(
            long elementId, AdjustmentType type)
        {
            return new RouteAdjustment
            {
                ElementId = elementId,
                TypeEnum = type,
                Type = type.ToString(),
                Timestamp = DateTime.UtcNow
            };
        }

        private static RouteAdjustment MarcarFalha(
            RouteAdjustment adj, string message)
        {
            adj.Applied = false;
            adj.ValidationStatus = "FALHA";
            adj.Issues.Add(new ValidationIssue
            {
                Code = "ADJ_FAILED",
                Severity = "CRITICO",
                Message = message
            });
            return adj;
        }

        private void RegistrarAjuste(RouteAdjustment adj)
        {
            _sessionAdjustments.Add(adj);

            if (adj.Before != null)
                _undoStack[adj.AdjustmentId] = adj.Before;

            OnAdjustmentApplied?.Invoke(adj);
        }

        private void SalvarSessaoJson(AdjustmentSessionResult resultado)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Adjustments");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"adj_session_{_sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(resultado, JsonOpts);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                EmitProgress($"Sessão salva: {filePath}");
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
