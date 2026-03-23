using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Revit2026
{
    /// <summary>
    /// Ponto de entrada do plugin — implementa IExternalApplication.
    /// Registra aba, painel e botões na ribbon do Revit.
    /// </summary>
    public class App : IExternalApplication
    {
        // ══════════════════════════════════════════════════════════
        //  CONSTANTES
        // ══════════════════════════════════════════════════════════

        private const string TAB_NAME = "Hidráulica";
        private const string PANEL_AMBIENTES = "Ambientes";
        private const string PANEL_PIPELINE = "Pipeline";

        // ══════════════════════════════════════════════════════════
        //  STARTUP
        // ══════════════════════════════════════════════════════════

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Inicializar PluginCore
                PluginCore.CoreBootstrap.Initialize();

                // 2. Criar aba personalizada na ribbon
                application.CreateRibbonTab(TAB_NAME);

                // 3. Registrar painéis e botões
                RegistrarPainelAmbientes(application);
                RegistrarPainelPipeline(application);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erro - Hidráulica",
                    $"Falha ao inicializar plugin:\n{ex.Message}");
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
                // Cleanup: logs, recursos, event handlers
                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRO DE PAINÉIS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Painel "Ambientes" — botão de detecção e classificação.
        /// </summary>
        private void RegistrarPainelAmbientes(UIControlledApplication application)
        {
            var panel = application.CreateRibbonPanel(TAB_NAME, PANEL_AMBIENTES);
            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Botão: Detectar Ambientes
            var btnDetectar = new PushButtonData(
                "DetectarAmbientes",
                "Detectar\nAmbientes",
                assemblyPath,
                "Revit2026.Commands.DetectarAmbientesCommand")
            {
                ToolTip = "Detecta e classifica automaticamente os ambientes " +
                          "do modelo (Rooms → Spaces MEP).",
                LongDescription = "Etapa 1 do fluxo hidráulico:\n" +
                                  "• Lê todos os Rooms do modelo arquitetônico\n" +
                                  "• Classifica cada ambiente (Banheiro, Cozinha, etc.)\n" +
                                  "• Cria Spaces MEP correspondentes\n" +
                                  "• Gera relatório com diagnóstico"
            };

            panel.AddItem(btnDetectar);
        }

        /// <summary>
        /// Painel "Pipeline" — botão de execução do pipeline completo.
        /// </summary>
        private void RegistrarPainelPipeline(UIControlledApplication application)
        {
            var panel = application.CreateRibbonPanel(TAB_NAME, PANEL_PIPELINE);
            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            // Botão: Iniciar Pipeline
            var btnPipeline = new PushButtonData(
                "StartPipeline",
                "Iniciar\nPipeline",
                assemblyPath,
                "Revit2026.Commands.StartPipelineCommand")
            {
                ToolTip = "Abre a interface de automação e inicia o pipeline " +
                          "completo do projeto hidráulico.",
                LongDescription = "Pipeline completo:\n" +
                                  "• Detectar e classificar ambientes\n" +
                                  "• Inserir equipamentos\n" +
                                  "• Gerar pontos, prumadas e redes\n" +
                                  "• Dimensionar e exportar"
            };

            panel.AddItem(btnPipeline);
        }
    }
}
