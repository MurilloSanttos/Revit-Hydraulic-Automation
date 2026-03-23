using System.Reflection;
using Autodesk.Revit.UI;

namespace Revit2026
{
    /// <summary>
    /// Ponto de entrada do plugin — implementa IExternalApplication.
    /// Registra aba "Hidráulica", painel "Automação" e botões na ribbon.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string TAB_NAME = "Hidráulica";
        private const string PANEL_NAME = "Automação";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Inicializar PluginCore
                PluginCore.CoreBootstrap.Initialize();

                // 2. Criar aba (ignora se já existir)
                try
                {
                    application.CreateRibbonTab(TAB_NAME);
                }
                catch
                {
                    // Aba já existe — ok
                }

                // 3. Criar painel
                var panel = application.CreateRibbonPanel(TAB_NAME, PANEL_NAME);

                // 4. Registrar botões
                RegistrarBotoes(panel);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erro - Hidráulica",
                    $"Falha ao inicializar plugin:\n{ex.Message}");
                return Result.Failed;
            }
        }

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

        /// <summary>
        /// Registra todos os botões no painel "Automação".
        /// </summary>
        private void RegistrarBotoes(RibbonPanel panel)
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            // ── Botão: Detectar Ambientes ──────────────────────
            var btnDetectar = new PushButtonData(
                "DetectarAmbientes",
                "Detectar\nAmbientes",
                assemblyPath,
                "Revit2026.Commands.DetectarAmbientesCommand")
            {
                ToolTip = "Detecta e classifica automaticamente os ambientes " +
                          "do modelo (Rooms → Spaces MEP).",
                LongDescription = "Etapa 1 do fluxo hidráulico:\n" +
                                  "• Lê todos os Rooms do modelo\n" +
                                  "• Classifica cada ambiente\n" +
                                  "• Cria Spaces MEP correspondentes\n" +
                                  "• Gera relatório com diagnóstico"
            };

            panel.AddItem(btnDetectar);

            // ── Separador ──────────────────────────────────────
            panel.AddSeparator();

            // ── Botão: Iniciar Pipeline ────────────────────────
            var btnPipeline = new PushButtonData(
                "StartPipeline",
                "Iniciar\nPipeline",
                assemblyPath,
                "Revit2026.Commands.StartPipelineCommand")
            {
                ToolTip = "Abre a interface de automação e inicia o " +
                          "pipeline completo do projeto hidráulico.",
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
