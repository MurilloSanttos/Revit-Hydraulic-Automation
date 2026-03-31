using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit2026.Modules.DynamoIntegration.Logging;
using Revit2026.Modules.DynamoIntegration.Services;
using Revit2026.Modules.Schedules.Common;
using Revit2026.Modules.Schedules.Equipment;
using Revit2026.Modules.Schedules.Export;
using Revit2026.Modules.Schedules.Piping;
using Revit2026.Modules.Sheets;
using Revit2026.Modules.Views;

namespace Revit2026.Modules.Orchestration
{
    public enum EtapaStatus
    {
        Pendente,
        EmExecucao,
        AguardandoAprovacao,
        Aprovada,
        Rejeitada,
        Falha,
        Pulada
    }

    public class EtapaResult
    {
        public string EtapaId { get; set; } = "";
        public string Nome { get; set; } = "";
        public EtapaStatus Status { get; set; } = EtapaStatus.Pendente;
        public long DuracaoMs { get; set; }
        public string? Mensagem { get; set; }
        public object? Dados { get; set; }
        public Exception? Erro { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fim { get; set; }

        public bool Ok => Status == EtapaStatus.Aprovada;

        public override string ToString() =>
            $"[{EtapaId}] {Nome}: {Status} ({DuracaoMs}ms)";
    }

    public class PipelineResult
    {
        public bool Sucesso { get; set; }
        public List<EtapaResult> Etapas { get; set; } = new();
        public long DuracaoTotalMs { get; set; }
        public int EtapasExecutadas { get; set; }
        public int EtapasFalhas { get; set; }
        public int EtapasPuladas { get; set; }
        public string? MensagemFinal { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fim { get; set; }

        public EtapaResult? GetEtapa(string etapaId) =>
            Etapas.FirstOrDefault(e =>
                string.Equals(e.EtapaId, etapaId, StringComparison.OrdinalIgnoreCase));

        public override string ToString() =>
            $"Pipeline: {(Sucesso ? "OK" : "FALHA")} — " +
            $"{EtapasExecutadas} executadas, {EtapasFalhas} falhas " +
            $"({DuracaoTotalMs}ms)";
    }

    public class OrchestratorConfig
    {
        public bool PararNaFalha { get; set; } = true;
        public bool GerarSchedules { get; set; } = true;
        public bool GerarViews { get; set; } = true;
        public bool GerarPranchas { get; set; } = true;
        public bool ExportarCsv { get; set; } = true;
        public bool ExportarExcel { get; set; } = true;
        public string DiretorioExportacao { get; set; } = "";
        public string PrefixoPrancha { get; set; } = "H-";
        public int EscalaView { get; set; } = 50;
        public HashSet<string> EtapasParaPular { get; set; } = new();
    }

    public interface IHydraulicOrchestrator
    {
        PipelineResult ExecutarPipeline(Document doc, OrchestratorConfig? config = null);
        EtapaResult ExecutarEtapa(Document doc, string etapaId);
    }

    public class HydraulicOrchestrator : IHydraulicOrchestrator
    {
        private readonly DynamoExecutionLogger _logger;
        private PipelineResult? _ultimoResultado;

        public PipelineResult? UltimoResultado => _ultimoResultado;

        public event Action<EtapaResult>? EtapaConcluida;
        public event Action<string>? EtapaIniciada;

        public HydraulicOrchestrator()
        {
            _logger = new DynamoExecutionLogger();
        }

        // ══════════════════════════════════════════════════════════
        //  PIPELINE COMPLETO
        // ══════════════════════════════════════════════════════════

        public PipelineResult ExecutarPipeline(
            Document doc,
            OrchestratorConfig? config = null)
        {
            config ??= new OrchestratorConfig();

            var resultado = new PipelineResult
            {
                Inicio = DateTime.UtcNow
            };

            var sw = Stopwatch.StartNew();

            try
            {
                // E09 — Schedules (Quantitativos)
                if (config.GerarSchedules)
                {
                    var etapaSchedules = ExecutarEtapaInternal(
                        doc, "E09-Schedules", "Gerar Tabelas de Quantitativos",
                        () => GerarSchedules(doc, config));

                    resultado.Etapas.Add(etapaSchedules);
                    EtapaConcluida?.Invoke(etapaSchedules);

                    if (!etapaSchedules.Ok && config.PararNaFalha)
                    {
                        resultado.MensagemFinal = "Falha na geração de schedules.";
                        return FinalizarPipeline(resultado, sw);
                    }
                }

                // E10 — Views Hidráulicas
                if (config.GerarViews)
                {
                    var etapaViews = ExecutarEtapaInternal(
                        doc, "E10-Views", "Criar Views Hidráulicas",
                        () => GerarViews(doc));

                    resultado.Etapas.Add(etapaViews);
                    EtapaConcluida?.Invoke(etapaViews);

                    if (!etapaViews.Ok && config.PararNaFalha)
                    {
                        resultado.MensagemFinal = "Falha na criação de views.";
                        return FinalizarPipeline(resultado, sw);
                    }
                }

                // E11 — Pranchas
                if (config.GerarPranchas)
                {
                    var etapaPranchas = ExecutarEtapaInternal(
                        doc, "E11-Pranchas", "Montar Pranchas",
                        () => GerarPranchas(doc, config));

                    resultado.Etapas.Add(etapaPranchas);
                    EtapaConcluida?.Invoke(etapaPranchas);
                }

                // E12 — Exportação
                if (config.ExportarCsv || config.ExportarExcel)
                {
                    var etapaExport = ExecutarEtapaInternal(
                        doc, "E12-Export", "Exportar Quantitativos",
                        () => ExportarDados(doc, config));

                    resultado.Etapas.Add(etapaExport);
                    EtapaConcluida?.Invoke(etapaExport);
                }

                resultado.Sucesso = resultado.Etapas.All(e =>
                    e.Status is EtapaStatus.Aprovada or EtapaStatus.Pulada);

                resultado.MensagemFinal = resultado.Sucesso
                    ? "Pipeline concluído com sucesso."
                    : "Pipeline concluído com falhas.";
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.MensagemFinal = $"Erro fatal: {ex.Message}";
            }

            return FinalizarPipeline(resultado, sw);
        }

        // ══════════════════════════════════════════════════════════
        //  ETAPA INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        public EtapaResult ExecutarEtapa(Document doc, string etapaId)
        {
            return etapaId switch
            {
                "E09-Schedules" => ExecutarEtapaInternal(doc, etapaId,
                    "Gerar Schedules", () => GerarSchedules(doc, new OrchestratorConfig())),
                "E10-Views" => ExecutarEtapaInternal(doc, etapaId,
                    "Criar Views", () => GerarViews(doc)),
                "E11-Pranchas" => ExecutarEtapaInternal(doc, etapaId,
                    "Montar Pranchas", () => GerarPranchas(doc, new OrchestratorConfig())),
                "E12-Export" => ExecutarEtapaInternal(doc, etapaId,
                    "Exportar", () => ExportarDados(doc, new OrchestratorConfig())),
                _ => new EtapaResult
                {
                    EtapaId = etapaId,
                    Status = EtapaStatus.Falha,
                    Mensagem = $"Etapa '{etapaId}' não reconhecida."
                }
            };
        }

        // ══════════════════════════════════════════════════════════
        //  E09 — SCHEDULES
        // ══════════════════════════════════════════════════════════

        private object GerarSchedules(Document doc, OrchestratorConfig config)
        {
            var schedules = new List<ViewSchedule>();

            using var trans = new Transaction(doc, "HydraulicOrchestrator - Schedules");
            trans.Start();

            try
            {
                var configurator = new ScheduleConfiguratorService();

                // Quantitativo de Tubulação
                var pipeCreator = new PipeQuantityScheduleCreator();
                var pipeSchedule = pipeCreator.Criar(doc);
                if (pipeSchedule != null) schedules.Add(pipeSchedule);

                // Quantitativo de Conexões
                var fittingCreator = new PipeFittingQuantityScheduleCreator();
                var fittingSchedule = fittingCreator.Criar(doc);
                if (fittingSchedule != null) schedules.Add(fittingSchedule);

                // Equipamentos por Ambiente
                var equipCreator = new EquipmentByRoomScheduleCreator();
                var equipSchedule = equipCreator.Criar(doc);
                if (equipSchedule != null) schedules.Add(equipSchedule);

                trans.Commit();
            }
            catch
            {
                if (trans.HasStarted()) trans.RollBack();
                throw;
            }

            return schedules;
        }

        // ══════════════════════════════════════════════════════════
        //  E10 — VIEWS
        // ══════════════════════════════════════════════════════════

        private object GerarViews(Document doc)
        {
            var viewCreator = new FloorPlanHydraulicViewCreator();
            return viewCreator.CriarParaTodosLevels(doc);
        }

        // ══════════════════════════════════════════════════════════
        //  E11 — PRANCHAS
        // ══════════════════════════════════════════════════════════

        private object GerarPranchas(Document doc, OrchestratorConfig config)
        {
            var sheetCreator = new ViewSheetCreator();
            var numbering = new SheetNumberingService();
            var viewPlacement = new ViewPlacementService();
            var schedulePlacement = new SchedulePlacementService();
            var legendsNotes = new LegendsAndNotesService();

            var resultados = new List<string>();

            using var trans = new Transaction(doc, "HydraulicOrchestrator - Pranchas");
            trans.Start();

            try
            {
                // Coletar views hidráulicas
                var viewsHid = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .Where(v => !v.IsTemplate &&
                                v.Name.StartsWith("HID -"))
                    .Cast<View>()
                    .ToList();

                // Coletar schedules gerados
                var schedules = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSchedule))
                    .Cast<ViewSchedule>()
                    .Where(s => s.Name.StartsWith("Quantitativo") ||
                                s.Name.StartsWith("Equipamentos"))
                    .ToList();

                // Prancha de Views
                if (viewsHid.Count > 0)
                {
                    var numViews = numbering.GerarNumeroPrancha(
                        doc, config.PrefixoPrancha, "");

                    var sheetViews = sheetCreator.Criar(
                        doc, "Plantas Hidráulicas", numViews);

                    viewPlacement.PosicionarViews(doc, sheetViews, viewsHid);

                    // Notas padrão
                    var notas = LegendsAndNotesService.NotasPadraoHidraulica();
                    legendsNotes.AdicionarNotas(doc, sheetViews, notas);

                    resultados.Add($"Prancha {numViews} - Views criada");
                }

                // Prancha de Quantitativos
                if (schedules.Count > 0)
                {
                    var numQuant = numbering.GerarNumeroPrancha(
                        doc, config.PrefixoPrancha, "");

                    var sheetQuant = sheetCreator.Criar(
                        doc, "Quantitativos Hidráulicos", numQuant);

                    schedulePlacement.AdicionarTabelas(
                        doc, sheetQuant, schedules);

                    resultados.Add($"Prancha {numQuant} - Quantitativos criada");
                }

                trans.Commit();
            }
            catch
            {
                if (trans.HasStarted()) trans.RollBack();
                throw;
            }

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  E12 — EXPORTAÇÃO
        // ══════════════════════════════════════════════════════════

        private object ExportarDados(Document doc, OrchestratorConfig config)
        {
            var exportService = new ScheduleExportService();
            var exportados = new List<string>();

            var dirExport = config.DiretorioExportacao;
            if (string.IsNullOrEmpty(dirExport))
            {
                dirExport = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "HidraulicaExport");
            }

            var schedules = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .Where(s => s.Name.StartsWith("Quantitativo") ||
                            s.Name.StartsWith("Equipamentos"))
                .ToList();

            if (schedules.Count == 0)
                return exportados;

            foreach (var schedule in schedules)
            {
                var nomeArquivo = schedule.Name
                    .Replace(" ", "_")
                    .Replace("-", "_");

                if (config.ExportarCsv)
                {
                    var csvPath = Path.Combine(dirExport, $"{nomeArquivo}.csv");
                    exportService.ExportarCsv(schedule, csvPath);
                    exportados.Add(csvPath);
                }
            }

            if (config.ExportarExcel && schedules.Count > 0)
            {
                var xlsxPath = Path.Combine(dirExport, "Quantitativos_Hidraulicos.xlsx");
                exportService.ExportarMultiplosExcel(schedules, xlsxPath);
                exportados.Add(xlsxPath);
            }

            return exportados;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private EtapaResult ExecutarEtapaInternal(
            Document doc,
            string etapaId,
            string nome,
            Func<object> acao)
        {
            var result = new EtapaResult
            {
                EtapaId = etapaId,
                Nome = nome,
                Status = EtapaStatus.EmExecucao,
                Inicio = DateTime.UtcNow
            };

            EtapaIniciada?.Invoke(etapaId);
            var sw = Stopwatch.StartNew();

            try
            {
                result.Dados = acao();
                result.Status = EtapaStatus.Aprovada;
                result.Mensagem = "Concluído com sucesso.";
            }
            catch (Exception ex)
            {
                result.Status = EtapaStatus.Falha;
                result.Mensagem = ex.Message;
                result.Erro = ex;
            }

            result.DuracaoMs = sw.ElapsedMilliseconds;
            result.Fim = DateTime.UtcNow;

            return result;
        }

        private PipelineResult FinalizarPipeline(
            PipelineResult resultado,
            Stopwatch sw)
        {
            resultado.DuracaoTotalMs = sw.ElapsedMilliseconds;
            resultado.Fim = DateTime.UtcNow;
            resultado.EtapasExecutadas = resultado.Etapas
                .Count(e => e.Status != EtapaStatus.Pendente &&
                            e.Status != EtapaStatus.Pulada);
            resultado.EtapasFalhas = resultado.Etapas
                .Count(e => e.Status == EtapaStatus.Falha);
            resultado.EtapasPuladas = resultado.Etapas
                .Count(e => e.Status == EtapaStatus.Pulada);

            _ultimoResultado = resultado;
            return resultado;
        }
    }
}
