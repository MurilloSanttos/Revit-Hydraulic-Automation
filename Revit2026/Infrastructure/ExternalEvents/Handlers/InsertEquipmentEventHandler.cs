using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginCore.Interfaces;
using PluginCore.Models;
using Revit2026.Services.Insercao;

namespace Revit2026.Infrastructure.ExternalEvents.Handlers
{
    /// <summary>
    /// ExternalEventHandler para inserção automática de equipamentos MEP.
    /// Consome EquipmentInsertionService no thread principal do Revit via
    /// BaseExternalEventHandler (fila thread-safe).
    ///
    /// Modos de uso:
    /// 1. Inserção individual: InserirEquipamento(equip, symbolId, ponto)
    /// 2. Inserção em lote: InserirLote(lista de itens)
    /// 3. Inserção por ambiente: InserirPorAmbiente(doc, roomId, equipamentos)
    ///
    /// Todas as chamadas são enfileiradas e executadas no thread do Revit.
    /// </summary>
    public class InsertEquipmentEventHandler : BaseExternalEventHandler
    {
        private readonly ILogService _log;
        private readonly EquipmentInsertionService _insertionService;

        private const string ETAPA = "04_Insercao";
        private const string COMPONENTE = "InsertEquipHandler";

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTOR
        // ══════════════════════════════════════════════════════════

        public InsertEquipmentEventHandler(
            EquipmentInsertionService insertionService,
            ILogService logService)
            : base(logService)
        {
            _insertionService = insertionService
                ?? throw new ArgumentNullException(nameof(insertionService));
            _log = logService
                ?? throw new ArgumentNullException(nameof(logService));
        }

        // ══════════════════════════════════════════════════════════
        //  NOME DO HANDLER
        // ══════════════════════════════════════════════════════════

        public override string GetName() => "InsertEquipmentEventHandler";

        // ══════════════════════════════════════════════════════════
        //  INSERÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Enfileira inserção de um único equipamento.
        /// </summary>
        /// <param name="equipamento">Dados do equipamento hidráulico.</param>
        /// <param name="symbolId">ElementId do FamilySymbol.</param>
        /// <param name="pontoInsercao">Ponto XYZ em unidades internas (pés).</param>
        /// <param name="callback">Callback opcional com o resultado.</param>
        public void InserirEquipamento(
            EquipamentoHidraulico equipamento,
            ElementId symbolId,
            XYZ pontoInsercao,
            Action<FamilyInstance?>? callback = null)
        {
            EnfileirarAcao(app =>
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        "Nenhum documento ativo no Revit.");
                    callback?.Invoke(null);
                    return;
                }

                var result = ExecutarInsercaoIndividual(
                    doc, equipamento, symbolId, pontoInsercao);

                callback?.Invoke(result);
            });
        }

        /// <summary>
        /// Executa inserção individual no thread do Revit.
        /// </summary>
        private FamilyInstance? ExecutarInsercaoIndividual(
            Document doc,
            EquipamentoHidraulico equipamento,
            ElementId symbolId,
            XYZ pontoInsercao)
        {
            _log.Info(ETAPA, COMPONENTE,
                $"Inserindo equipamento: {equipamento.Tipo} " +
                $"(SymbolId={symbolId.Value})...");

            // Obter FamilySymbol
            var symbol = doc.GetElement(symbolId) as FamilySymbol;
            if (symbol == null)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao inserir equipamento {equipamento.Id}: " +
                    $"FamilySymbol não encontrado (Id={symbolId.Value}).",
                    symbolId.Value);
                return null;
            }

            // Executar dentro de Transaction
            using var trans = new Transaction(doc,
                $"Inserir {equipamento.Tipo}");

            try
            {
                trans.Start();

                var instancia = _insertionService.InserirEquipamento(
                    doc, equipamento, symbol, pontoInsercao, _log);

                if (instancia != null)
                {
                    trans.Commit();
                    _log.Info(ETAPA, COMPONENTE,
                        $"Equipamento inserido com sucesso: " +
                        $"{equipamento.Tipo} → Id={instancia.Id.Value}",
                        instancia.Id.Value);
                }
                else
                {
                    trans.RollBack();
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao inserir equipamento {equipamento.Tipo}.");
                }

                return instancia;
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                _log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao inserir equipamento {equipamento.Tipo}: {ex.Message}",
                    detalhes: ex.StackTrace);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  INSERÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Enfileira inserção de múltiplos equipamentos.
        /// Todos são inseridos em uma única Transaction.
        /// </summary>
        /// <param name="itens">Lista de (Equipamento, SymbolId, Ponto).</param>
        /// <param name="callback">Callback opcional com o resultado.</param>
        public void InserirLote(
            List<(EquipamentoHidraulico Equip, ElementId SymbolId, XYZ Ponto)> itens,
            Action<EquipmentInsertionService.ResultadoInsercaoLote?>? callback = null)
        {
            EnfileirarAcao(app =>
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        "Nenhum documento ativo no Revit.");
                    callback?.Invoke(null);
                    return;
                }

                var result = ExecutarInsercaoLote(doc, itens);
                callback?.Invoke(result);
            });
        }

        /// <summary>
        /// Executa inserção em lote no thread do Revit.
        /// </summary>
        private EquipmentInsertionService.ResultadoInsercaoLote ExecutarInsercaoLote(
            Document doc,
            List<(EquipamentoHidraulico Equip, ElementId SymbolId, XYZ Ponto)> itens)
        {
            _log.Info(ETAPA, COMPONENTE,
                $"Inserção em lote: {itens.Count} equipamentos...");

            // Resolver FamilySymbols
            var itensResolvidos = new List<(EquipamentoHidraulico, FamilySymbol, XYZ)>();
            int naoResolvidos = 0;

            foreach (var (equip, symbolId, ponto) in itens)
            {
                var symbol = doc.GetElement(symbolId) as FamilySymbol;
                if (symbol != null)
                {
                    itensResolvidos.Add((equip, symbol, ponto));
                }
                else
                {
                    naoResolvidos++;
                    _log.Medio(ETAPA, COMPONENTE,
                        $"FamilySymbol não encontrado para {equip.Tipo} " +
                        $"(Id={symbolId.Value}), ignorando.",
                        symbolId.Value);
                }
            }

            if (naoResolvidos > 0)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"{naoResolvidos} equipamentos ignorados por FamilySymbol inválido.");
            }

            if (itensResolvidos.Count == 0)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    "Nenhum equipamento válido para inserção em lote.");
                return new EquipmentInsertionService.ResultadoInsercaoLote
                {
                    Total = itens.Count,
                    Falhas = itens.Count
                };
            }

            // Delegar ao serviço de inserção (ele gerencia a Transaction)
            var resultado = _insertionService.InserirLote(doc, itensResolvidos, _log);

            // Log de resumo
            _log.Info(ETAPA, COMPONENTE,
                $"═══ Resumo Inserção em Lote ═══\n" +
                $"  Total solicitados:  {itens.Count}\n" +
                $"  Symbols resolvidos: {itensResolvidos.Count}\n" +
                $"  Inseridos:          {resultado.Inseridos}\n" +
                $"  Falhas:             {resultado.Falhas + naoResolvidos}\n" +
                $"  Instâncias criadas: {resultado.Instancias.Count}");

            if (resultado.Erros.Count > 0)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Erros na inserção: {string.Join("; ", resultado.Erros)}");
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  INSERÇÃO POR AMBIENTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Enfileira inserção de todos os equipamentos de um ambiente.
        /// Resolve symbols automaticamente por nome de família.
        /// </summary>
        /// <param name="equipamentos">Equipamentos com FamilySymbol já definido.</param>
        /// <param name="symbolMap">Mapa de Tipo → ElementId do FamilySymbol.</param>
        /// <param name="pontoMap">Mapa de EquipamentoId → Ponto de inserção.</param>
        /// <param name="callback">Callback com resultado.</param>
        public void InserirPorAmbiente(
            List<EquipamentoHidraulico> equipamentos,
            Dictionary<string, ElementId> symbolMap,
            Dictionary<string, XYZ> pontoMap,
            Action<EquipmentInsertionService.ResultadoInsercaoLote?>? callback = null)
        {
            // Montar lista de itens
            var itens = new List<(EquipamentoHidraulico, ElementId, XYZ)>();
            int semSymbol = 0;
            int semPonto = 0;

            foreach (var equip in equipamentos)
            {
                var tipoStr = equip.Tipo.ToString();

                if (!symbolMap.TryGetValue(tipoStr, out var symbolId))
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Equipamento {equip.Id} ({equip.Tipo}): " +
                        $"Symbol não mapeado, ignorando.");
                    semSymbol++;
                    continue;
                }

                if (!pontoMap.TryGetValue(equip.Id, out var ponto))
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Equipamento {equip.Id} ({equip.Tipo}): " +
                        $"Ponto de inserção não definido, ignorando.");
                    semPonto++;
                    continue;
                }

                itens.Add((equip, symbolId, ponto));
            }

            if (semSymbol > 0 || semPonto > 0)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Inserção por ambiente: {semSymbol} sem symbol, " +
                    $"{semPonto} sem ponto ({itens.Count} válidos).");
            }

            if (itens.Count > 0)
            {
                InserirLote(itens, callback);
            }
            else
            {
                _log.Critico(ETAPA, COMPONENTE,
                    "Nenhum equipamento válido para inserção por ambiente.");
                callback?.Invoke(null);
            }
        }
    }
}
