using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginCore.Logging;
using PluginCore.Services;
using Revit2026.Services;

namespace Revit2026.Commands
{
    /// <summary>
    /// Comando principal da Etapa 1 — Detectar e Classificar Ambientes.
    /// 
    /// Fluxo de execução:
    /// 1. Lê todos os Rooms do modelo
    /// 2. Classifica automaticamente cada ambiente
    /// 3. Verifica Spaces MEP existentes
    /// 4. Cria Spaces faltantes (com confirmação do usuário)
    /// 5. Valida resultados e gera logs
    /// 6. Exibe resumo na UI
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DetectarAmbientesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message,
            ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            // Diretório de logs
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HidraulicaRevit", "Logs");

            var logManager = new LogManager(logDir);

            try
            {
                // === PASSO 1: Ler Rooms ===
                var roomReader = new RoomReaderService(doc, logManager);
                var rooms = roomReader.LerTodosOsRooms();

                if (rooms.Count == 0)
                {
                    logManager.Critico("01_Ambientes", "Comando",
                        "Nenhum Room válido encontrado no modelo.");
                    TaskDialog.Show("Hidráulica - Ambientes",
                        "❌ Nenhum Room válido encontrado no modelo.\n\n" +
                        "Verifique se o modelo arquitetônico possui Rooms definidos.");
                    return Result.Failed;
                }

                // === PASSO 2: Classificar Ambientes ===
                var classificador = new ClassificadorAmbientes();
                classificador.ClassificarTodos(rooms);

                var relevantes = rooms.Count(r => r.EhRelevante);
                logManager.Info("01_Ambientes", "Comando",
                    $"Classificação concluída: {relevantes}/{rooms.Count} ambientes relevantes.");

                // === PASSO 3: Validar ===
                var validador = new ValidadorAmbientes(logManager);
                var validacaoOk = validador.ValidarTodos(rooms);

                // === PASSO 4: Verificar Spaces ===
                var spaceManager = new SpaceManagerService(doc, logManager);
                var spacesExistentes = spaceManager.LerTodosOsSpaces();
                var correspondencia = spaceManager.ValidarCorrespondencia(rooms, spacesExistentes);

                // === PASSO 5: Criar Spaces faltantes (se necessário) ===
                if (correspondencia.RoomsSemSpace.Count > 0)
                {
                    var roomsRelevantes = correspondencia.RoomsSemSpace
                        .Where(r => r.EhRelevante)
                        .ToList();

                    if (roomsRelevantes.Count > 0)
                    {
                        var confirmacao = TaskDialog.Show("Hidráulica - Criar Spaces",
                            $"Foram encontrados {roomsRelevantes.Count} ambientes hidráulicos " +
                            $"sem Space MEP correspondente.\n\n" +
                            $"Deseja criar os Spaces automaticamente?",
                            TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                        if (confirmacao == TaskDialogResult.Yes)
                        {
                            using var transaction = new Transaction(doc, "Criar Spaces MEP");
                            transaction.Start();

                            try
                            {
                                var spacesCriados = spaceManager.CriarSpacesParaRooms(roomsRelevantes);
                                transaction.Commit();

                                logManager.Info("01_Ambientes", "Comando",
                                    $"{spacesCriados.Count} Spaces criados com sucesso.");
                            }
                            catch (Exception ex)
                            {
                                transaction.RollBack();
                                logManager.Critico("01_Ambientes", "Comando",
                                    $"Erro ao criar Spaces: {ex.Message}",
                                    detalhes: ex.StackTrace);
                            }
                        }
                    }
                }

                // === PASSO 6: Gerar resultado ===
                var logPath = logManager.ExportarParaJson();
                var resumo = logManager.GerarResumo();

                // Montar relatório dos ambientes
                var relatorio = MontarRelatorio(rooms, logManager);

                TaskDialog.Show("Hidráulica - Resultado", relatorio);

                return validacaoOk ? Result.Succeeded : Result.Succeeded;
            }
            catch (Exception ex)
            {
                logManager.Critico("01_Ambientes", "Comando",
                    $"Erro inesperado: {ex.Message}",
                    detalhes: ex.StackTrace);

                logManager.ExportarParaJson();
                message = ex.Message;
                return Result.Failed;
            }
        }

        /// <summary>
        /// Monta o relatório textual para exibição no TaskDialog.
        /// </summary>
        private string MontarRelatorio(List<PluginCore.Models.AmbienteInfo> rooms,
            LogManager logManager)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("═══ DETECÇÃO DE AMBIENTES ═══");
            sb.AppendLine();

            // Ambientes classificados
            var porTipo = rooms
                .Where(r => r.EhRelevante)
                .GroupBy(r => r.Classificacao.Tipo)
                .OrderBy(g => g.Key);

            foreach (var grupo in porTipo)
            {
                sb.AppendLine($"▸ {grupo.Key} ({grupo.Count()}):");
                foreach (var amb in grupo)
                {
                    var confianca = amb.Classificacao.EhConfiavel ? "✅" :
                                   amb.Classificacao.NecessitaValidacao ? "⚠️" : "❓";
                    sb.AppendLine($"    {confianca} {amb.NomeOriginal} " +
                                 $"({amb.AreaM2:F1}m² | {amb.Nivel})");
                }
                sb.AppendLine();
            }

            // Não classificados
            var naoClassificados = rooms.Where(r => !r.EhRelevante).ToList();
            if (naoClassificados.Count > 0)
            {
                sb.AppendLine($"▸ Não classificados ({naoClassificados.Count}):");
                foreach (var amb in naoClassificados.Take(10))
                {
                    sb.AppendLine($"    ❓ {amb.NomeOriginal}");
                }
                if (naoClassificados.Count > 10)
                    sb.AppendLine($"    ... e mais {naoClassificados.Count - 10}");
            }

            sb.AppendLine();
            sb.AppendLine(logManager.GerarResumo());

            return sb.ToString();
        }
    }
}
