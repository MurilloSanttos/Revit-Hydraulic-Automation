using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Revit2026
{
    /// <summary>
    /// Ponto de entrada do plugin — registra a aba, painel e botões na ribbon do Revit.
    /// Implementa IExternalApplication para ser carregado automaticamente pelo Revit.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string TAB_NAME = "Hidráulica";
        private const string PANEL_NAME = "Automação";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Inicializar PluginCore
                PluginCore.CoreBootstrap.Initialize();

                // Criar aba personalizada
                application.CreateRibbonTab(TAB_NAME);

                // Criar painel
                var panel = application.CreateRibbonPanel(TAB_NAME, PANEL_NAME);

                // Caminho do assembly
                var assemblyPath = Assembly.GetExecutingAssembly().Location;

                // === Botão: Detectar Ambientes ===
                var btnDetectarData = new PushButtonData(
                    "DetectarAmbientes",
                    "Detectar\nAmbientes",
                    assemblyPath,
                    "Revit2026.Commands.DetectarAmbientesCommand")
                {
                    ToolTip = "Detecta e classifica automaticamente os ambientes do modelo " +
                              "(Rooms → Spaces MEP) para o projeto hidráulico.",
                    LongDescription = "Etapa 1 do fluxo hidráulico:\n" +
                                     "• Lê todos os Rooms do modelo arquitetônico\n" +
                                     "• Classifica cada ambiente (Banheiro, Cozinha, etc.)\n" +
                                     "• Cria Spaces MEP correspondentes\n" +
                                     "• Gera relatório com diagnóstico"
                };

                panel.AddItem(btnDetectarData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erro - Hidráulica",
                    $"Falha ao inicializar plugin Hidráulica:\n{ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
