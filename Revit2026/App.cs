using System.Diagnostics;
using System.Reflection;
using Autodesk.Revit.UI;
using PluginCore.Logging;

namespace Revit2026
{
    /// <summary>
    /// Ponto de entrada do plugin — implementa IExternalApplication.
    /// Registra aba "Hidráulica" com 3 painéis e 12 botões na ribbon.
    /// Inclui validação de carregamento e diagnóstico.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string TAB_NAME = "Hidráulica";
        private const string PLUGIN_NAME = "Hydraulic Automation";
        private const string PLUGIN_VERSION = "0.1.0";

        private LogManager? _logManager;

        // ══════════════════════════════════════════════════════════
        //  STARTUP
        // ══════════════════════════════════════════════════════════

        public Result OnStartup(UIControlledApplication application)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // ── 1. Inicializar logging ─────────────────────
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HidraulicaRevit", "Logs");

                _logManager = new LogManager(logDir);
                _logManager.Info("Startup", "App",
                    $"Iniciando {PLUGIN_NAME} v{PLUGIN_VERSION}...");

                // ── 2. Inicializar PluginCore ──────────────────
                PluginCore.CoreBootstrap.Initialize();
                _logManager.Info("Startup", "App",
                    "PluginCore.CoreBootstrap inicializado com sucesso.");

                // ── 3. Validar ambiente ────────────────────────
                var diagnostico = ValidarAmbiente(application);
                _logManager.Info("Startup", "App", diagnostico);

                // ── 4. Criar aba (ignora se já existir) ────────
                try { application.CreateRibbonTab(TAB_NAME); }
                catch { _logManager.Info("Startup", "App", "Aba já existente — reutilizando."); }

                // ── 5. Registrar painéis ───────────────────────
                var asm = Assembly.GetExecutingAssembly().Location;

                RegistrarPainelDeteccao(application, asm);
                _logManager.Info("Startup", "App", "Painel 'Detecção' registrado (4 botões).");

                RegistrarPainelRedes(application, asm);
                _logManager.Info("Startup", "App", "Painel 'Redes' registrado (5 botões).");

                RegistrarPainelExportacao(application, asm);
                _logManager.Info("Startup", "App", "Painel 'Exportação' registrado (3 botões).");

                // ── 6. Finalizar ───────────────────────────────
                sw.Stop();

                var resumo = $"{PLUGIN_NAME} v{PLUGIN_VERSION}\n\n" +
                             $"✅ Plugin carregado com sucesso.\n\n" +
                             $"• PluginCore: Inicializado\n" +
                             $"• Aba: \"{TAB_NAME}\" registrada\n" +
                             $"• Painéis: 3 (Detecção, Redes, Exportação)\n" +
                             $"• Botões: 12 comandos disponíveis\n" +
                             $"• Tempo de inicialização: {sw.ElapsedMilliseconds}ms\n\n" +
                             $"Log: {logDir}";

                _logManager.Info("Startup", "App",
                    $"Inicialização concluída em {sw.ElapsedMilliseconds}ms.");

                TaskDialog.Show(PLUGIN_NAME, resumo);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                sw.Stop();

                // Log do erro
                _logManager?.Critico("Startup", "App",
                    $"FALHA na inicialização: {ex.Message}",
                    detalhes: ex.StackTrace);

                try { _logManager?.ExportarParaJson(); } catch { /* ignore */ }

                // Feedback visual detalhado
                var erro = $"{PLUGIN_NAME} — FALHA\n\n" +
                           $"❌ Erro ao inicializar o plugin.\n\n" +
                           $"Exceção: {ex.GetType().Name}\n" +
                           $"Mensagem: {ex.Message}\n\n" +
                           $"Tempo até falha: {sw.ElapsedMilliseconds}ms\n\n" +
                           $"Verifique o log para detalhes completos.";

                TaskDialog.Show("Erro - " + PLUGIN_NAME, erro);

                return Result.Failed;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SHUTDOWN
        // ══════════════════════════════════════════════════════════

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                _logManager?.Info("Shutdown", "App", "Plugin finalizado.");
                _logManager?.ExportarParaJson();

                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  DIAGNÓSTICO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida o ambiente de execução e gera resumo de diagnóstico.
        /// </summary>
        private string ValidarAmbiente(UIControlledApplication application)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var revitVersion = application.ControlledApplication.VersionNumber;
            var revitBuild = application.ControlledApplication.VersionBuild;
            var assemblyPath = assembly.Location;
            var coreInitialized = PluginCore.CoreBootstrap.IsInitialized;

            var diag = $"Diagnóstico do ambiente:\n" +
                       $"  Revit: {revitVersion} (Build {revitBuild})\n" +
                       $"  Plugin: {PLUGIN_NAME} v{PLUGIN_VERSION}\n" +
                       $"  Assembly: {assemblyPath}\n" +
                       $"  .NET: {Environment.Version}\n" +
                       $"  OS: {Environment.OSVersion}\n" +
                       $"  CoreInitialized: {coreInitialized}\n" +
                       $"  Machine: {Environment.MachineName}\n" +
                       $"  User: {Environment.UserName}";

            return diag;
        }

        // ══════════════════════════════════════════════════════════
        //  PAINEL 1: Detecção e Preparação (E01-E04)
        // ══════════════════════════════════════════════════════════

        private void RegistrarPainelDeteccao(UIControlledApplication app, string asm)
        {
            var panel = app.CreateRibbonPanel(TAB_NAME, "Detecção");

            panel.AddItem(new PushButtonData(
                "DetectarAmbientes", "Detectar\nAmbientes", asm,
                "Revit2026.Commands.DetectarAmbientesCommand")
            { ToolTip = "E01 — Detecta Rooms e cria Spaces MEP." });

            panel.AddItem(new PushButtonData(
                "ClassificarAmbientes", "Classificar\nAmbientes", asm,
                "Revit2026.Commands.ClassificarAmbientesCommand")
            { ToolTip = "E02 — Classifica ambientes por tipo funcional." });

            panel.AddSeparator();

            panel.AddItem(new PushButtonData(
                "InserirEquipamentos", "Inserir\nEquipamentos", asm,
                "Revit2026.Commands.InserirEquipamentosCommand")
            { ToolTip = "E03 — Insere equipamentos hidráulicos nos ambientes." });

            panel.AddItem(new PushButtonData(
                "GerarPontos", "Gerar\nPontos", asm,
                "Revit2026.Commands.GerarPontosCommand")
            { ToolTip = "E04 — Gera pontos hidráulicos (AF/ES/VE)." });
        }

        // ══════════════════════════════════════════════════════════
        //  PAINEL 2: Redes e Dimensionamento (E05-E09)
        // ══════════════════════════════════════════════════════════

        private void RegistrarPainelRedes(UIControlledApplication app, string asm)
        {
            var panel = app.CreateRibbonPanel(TAB_NAME, "Redes");

            panel.AddItem(new PushButtonData(
                "CriarPrumadas", "Criar\nPrumadas", asm,
                "Revit2026.Commands.CriarPrumadasCommand")
            { ToolTip = "E05 — Cria prumadas verticais AF/ES/VE." });

            panel.AddItem(new PushButtonData(
                "RedeAguaFria", "Rede\nÁgua Fria", asm,
                "Revit2026.Commands.RedeAguaFriaCommand")
            { ToolTip = "E06 — Gera rede de água fria." });

            panel.AddItem(new PushButtonData(
                "RedeEsgoto", "Rede\nEsgoto", asm,
                "Revit2026.Commands.RedeEsgotoCommand")
            { ToolTip = "E07 — Gera rede de esgoto sanitário." });

            panel.AddSeparator();

            panel.AddItem(new PushButtonData(
                "Ventilacao", "Rede\nVentilação", asm,
                "Revit2026.Commands.VentilacaoCommand")
            { ToolTip = "E08 — Gera rede de ventilação." });

            panel.AddItem(new PushButtonData(
                "Dimensionamento", "Dimensionar\nRedes", asm,
                "Revit2026.Commands.DimensionamentoCommand")
            { ToolTip = "E09 — Calcula vazão, diâmetro e pressão (NBR 5626/8160)." });
        }

        // ══════════════════════════════════════════════════════════
        //  PAINEL 3: Exportação (E10-E11 + Pipeline)
        // ══════════════════════════════════════════════════════════

        private void RegistrarPainelExportacao(UIControlledApplication app, string asm)
        {
            var panel = app.CreateRibbonPanel(TAB_NAME, "Exportação");

            panel.AddItem(new PushButtonData(
                "GerarTabelas", "Gerar\nTabelas", asm,
                "Revit2026.Commands.GerarTabelasCommand")
            { ToolTip = "E10 — Gera tabelas de dimensionamento e quantitativos." });

            panel.AddItem(new PushButtonData(
                "GerarPranchas", "Gerar\nPranchas", asm,
                "Revit2026.Commands.GerarPranchasCommand")
            { ToolTip = "E11 — Gera pranchas finais do projeto." });

            panel.AddSeparator();

            panel.AddItem(new PushButtonData(
                "StartPipeline", "▶ Pipeline\nCompleto", asm,
                "Revit2026.Commands.StartPipelineCommand")
            { ToolTip = "Executa todas as etapas em sequência com validação humana." });
        }
    }
}
