using System.Reflection;
using Autodesk.Revit.UI;

namespace Revit2026
{
    /// <summary>
    /// Ponto de entrada do plugin — implementa IExternalApplication.
    /// Registra aba "Hidráulica" com 3 painéis e 12 botões na ribbon.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string TAB_NAME = "Hidráulica";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Inicializar PluginCore
                PluginCore.CoreBootstrap.Initialize();

                // 2. Criar aba (ignora se já existir)
                try { application.CreateRibbonTab(TAB_NAME); }
                catch { /* Aba já existe */ }

                // 3. Caminho do assembly
                var asm = Assembly.GetExecutingAssembly().Location;

                // 4. Registrar painéis
                RegistrarPainelDeteccao(application, asm);
                RegistrarPainelRedes(application, asm);
                RegistrarPainelExportacao(application, asm);

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
            try { return Result.Succeeded; }
            catch { return Result.Failed; }
        }

        // ══════════════════════════════════════════════════════════
        //  PAINEL 1: Detecção e Preparação (E01-E04)
        // ══════════════════════════════════════════════════════════

        private void RegistrarPainelDeteccao(UIControlledApplication app, string asm)
        {
            var panel = app.CreateRibbonPanel(TAB_NAME, "Detecção");

            // E01 — Detectar Ambientes
            panel.AddItem(new PushButtonData(
                "DetectarAmbientes", "Detectar\nAmbientes", asm,
                "Revit2026.Commands.DetectarAmbientesCommand")
            {
                ToolTip = "E01 — Detecta Rooms e cria Spaces MEP."
            });

            // E02 — Classificar Ambientes
            panel.AddItem(new PushButtonData(
                "ClassificarAmbientes", "Classificar\nAmbientes", asm,
                "Revit2026.Commands.ClassificarAmbientesCommand")
            {
                ToolTip = "E02 — Classifica ambientes por tipo funcional."
            });

            panel.AddSeparator();

            // E03 — Inserir Equipamentos
            panel.AddItem(new PushButtonData(
                "InserirEquipamentos", "Inserir\nEquipamentos", asm,
                "Revit2026.Commands.InserirEquipamentosCommand")
            {
                ToolTip = "E03 — Insere equipamentos hidráulicos nos ambientes."
            });

            // E04 — Gerar Pontos
            panel.AddItem(new PushButtonData(
                "GerarPontos", "Gerar\nPontos", asm,
                "Revit2026.Commands.GerarPontosCommand")
            {
                ToolTip = "E04 — Gera pontos hidráulicos (AF/ES/VE)."
            });
        }

        // ══════════════════════════════════════════════════════════
        //  PAINEL 2: Redes e Dimensionamento (E05-E09)
        // ══════════════════════════════════════════════════════════

        private void RegistrarPainelRedes(UIControlledApplication app, string asm)
        {
            var panel = app.CreateRibbonPanel(TAB_NAME, "Redes");

            // E05 — Criar Prumadas
            panel.AddItem(new PushButtonData(
                "CriarPrumadas", "Criar\nPrumadas", asm,
                "Revit2026.Commands.CriarPrumadasCommand")
            {
                ToolTip = "E05 — Cria prumadas verticais AF/ES/VE."
            });

            // E06 — Rede Água Fria
            panel.AddItem(new PushButtonData(
                "RedeAguaFria", "Rede\nÁgua Fria", asm,
                "Revit2026.Commands.RedeAguaFriaCommand")
            {
                ToolTip = "E06 — Gera rede de água fria."
            });

            // E07 — Rede Esgoto
            panel.AddItem(new PushButtonData(
                "RedeEsgoto", "Rede\nEsgoto", asm,
                "Revit2026.Commands.RedeEsgotoCommand")
            {
                ToolTip = "E07 — Gera rede de esgoto sanitário."
            });

            panel.AddSeparator();

            // E08 — Ventilação
            panel.AddItem(new PushButtonData(
                "Ventilacao", "Rede\nVentilação", asm,
                "Revit2026.Commands.VentilacaoCommand")
            {
                ToolTip = "E08 — Gera rede de ventilação."
            });

            // E09 — Dimensionamento
            panel.AddItem(new PushButtonData(
                "Dimensionamento", "Dimensionar\nRedes", asm,
                "Revit2026.Commands.DimensionamentoCommand")
            {
                ToolTip = "E09 — Calcula vazão, diâmetro e pressão (NBR 5626/8160)."
            });
        }

        // ══════════════════════════════════════════════════════════
        //  PAINEL 3: Exportação (E10-E11 + Pipeline)
        // ══════════════════════════════════════════════════════════

        private void RegistrarPainelExportacao(UIControlledApplication app, string asm)
        {
            var panel = app.CreateRibbonPanel(TAB_NAME, "Exportação");

            // E10 — Gerar Tabelas
            panel.AddItem(new PushButtonData(
                "GerarTabelas", "Gerar\nTabelas", asm,
                "Revit2026.Commands.GerarTabelasCommand")
            {
                ToolTip = "E10 — Gera tabelas de dimensionamento e quantitativos."
            });

            // E11 — Gerar Pranchas
            panel.AddItem(new PushButtonData(
                "GerarPranchas", "Gerar\nPranchas", asm,
                "Revit2026.Commands.GerarPranchasCommand")
            {
                ToolTip = "E11 — Gera pranchas finais do projeto."
            });

            panel.AddSeparator();

            // Pipeline Completo
            panel.AddItem(new PushButtonData(
                "StartPipeline", "▶ Pipeline\nCompleto", asm,
                "Revit2026.Commands.StartPipelineCommand")
            {
                ToolTip = "Executa todas as etapas em sequência com validação humana."
            });
        }
    }
}
