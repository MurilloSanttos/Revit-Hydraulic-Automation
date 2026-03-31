using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services.Insercao
{
    /// <summary>
    /// Serviço responsável pela inserção de equipamentos hidráulicos
    /// como FamilyInstance no modelo Revit.
    ///
    /// Fluxo:
    /// 1. Valida pré-condições (symbol, ponto, ativação)
    /// 2. Cria FamilyInstance via NewFamilyInstance
    /// 3. Associa Level (explícito ou inferido)
    /// 4. Preenche parâmetros do equipamento
    /// 5. Retorna instância criada ou null em caso de falha
    ///
    /// IMPORTANTE: Deve ser chamado dentro de uma Transaction ativa.
    /// </summary>
    public class EquipmentInsertionService
    {
        private const string ETAPA = "04_Insercao";
        private const string COMPONENTE = "EquipInsertion";

        // ══════════════════════════════════════════════════════════
        //  INSERÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Insere um equipamento hidráulico no modelo Revit.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit ativo.</param>
        /// <param name="equipamento">Dados do equipamento a inserir.</param>
        /// <param name="symbol">FamilySymbol (tipo) a ser instanciado.</param>
        /// <param name="pontoInsercao">Ponto XYZ em unidades internas do Revit (pés).</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>FamilyInstance criada ou null em caso de falha.</returns>
        public FamilyInstance? InserirEquipamento(
            Document doc,
            EquipamentoHidraulico equipamento,
            FamilySymbol symbol,
            XYZ pontoInsercao,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (equipamento == null) throw new ArgumentNullException(nameof(equipamento));

            // ── 1. Validar pré-condições ──────────────────────
            if (symbol == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao inserir equipamento {equipamento.Id}: " +
                    $"FamilySymbol é nulo (Tipo={equipamento.Tipo}).",
                    equipamento.RevitElementId);
                return null;
            }

            if (pontoInsercao == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao inserir equipamento {equipamento.Id}: " +
                    $"ponto de inserção nulo.",
                    equipamento.RevitElementId);
                return null;
            }

            // ── 2. Ativar símbolo ─────────────────────────────
            try
            {
                if (!symbol.IsActive)
                {
                    symbol.Activate();
                    doc.Regenerate();
                }
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao ativar FamilySymbol '{symbol.Name}': {ex.Message}",
                    equipamento.RevitElementId,
                    detalhes: ex.StackTrace);
                return null;
            }

            // ── 3. Criar FamilyInstance ────────────────────────
            FamilyInstance? instancia = null;

            try
            {
                instancia = doc.Create.NewFamilyInstance(
                    pontoInsercao,
                    symbol,
                    StructuralType.NonStructural);

                if (instancia == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao inserir equipamento {equipamento.Id}: " +
                        $"Revit retornou null para NewFamilyInstance " +
                        $"(Symbol='{symbol.Name}').",
                        equipamento.RevitElementId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao inserir equipamento {equipamento.Id}: {ex.Message}",
                    equipamento.RevitElementId,
                    detalhes: ex.StackTrace);
                return null;
            }

            // ── 4. Associar Level ─────────────────────────────
            AssociarLevel(doc, instancia, equipamento, pontoInsercao, log);

            // ── 5. Preencher parâmetros ───────────────────────
            PreencherParametros(instancia, equipamento, log);

            // ── 6. Atualizar modelo do domínio ────────────────
            equipamento.RevitElementId = instancia.Id.Value;
            equipamento.Processado = true;

            // ── 7. Log de sucesso ─────────────────────────────
            var posMetros = ConverterPonto(pontoInsercao);
            log.Info(ETAPA, COMPONENTE,
                $"Equipamento inserido: {equipamento.Tipo} " +
                $"('{symbol.FamilyName}' / '{symbol.Name}') " +
                $"em ({posMetros.X:F3}, {posMetros.Y:F3}, {posMetros.Z:F3}) m. " +
                $"InstanceId={instancia.Id.Value}",
                instancia.Id.Value);

            return instancia;
        }

        // ══════════════════════════════════════════════════════════
        //  INSERÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado da inserção em lote.
        /// </summary>
        public class ResultadoInsercaoLote
        {
            public int Total { get; set; }
            public int Inseridos { get; set; }
            public int Falhas { get; set; }
            public List<FamilyInstance> Instancias { get; set; } = new();
            public List<string> Erros { get; set; } = new();

            public override string ToString() =>
                $"{Inseridos}/{Total} inseridos, {Falhas} falhas";
        }

        /// <summary>
        /// Insere múltiplos equipamentos dentro de uma Transaction.
        /// </summary>
        public ResultadoInsercaoLote InserirLote(
            Document doc,
            List<(EquipamentoHidraulico Equip, FamilySymbol Symbol, XYZ Ponto)> itens,
            ILogService log)
        {
            var resultado = new ResultadoInsercaoLote { Total = itens.Count };

            log.Info(ETAPA, COMPONENTE,
                $"Iniciando inserção em lote: {itens.Count} equipamentos...");

            using var trans = new Transaction(doc, "Inserir Equipamentos Hidráulicos");

            try
            {
                trans.Start();

                foreach (var (equip, symbol, ponto) in itens)
                {
                    var instancia = InserirEquipamento(doc, equip, symbol, ponto, log);

                    if (instancia != null)
                    {
                        resultado.Inseridos++;
                        resultado.Instancias.Add(instancia);
                    }
                    else
                    {
                        resultado.Falhas++;
                        resultado.Erros.Add(
                            $"{equip.Tipo} (Id={equip.Id}): inserção falhou");
                    }
                }

                if (resultado.Inseridos > 0)
                {
                    trans.Commit();
                    log.Info(ETAPA, COMPONENTE,
                        $"Transaction committed: {resultado}");
                }
                else
                {
                    trans.RollBack();
                    log.Medio(ETAPA, COMPONENTE,
                        "Transaction rolled back — nenhum equipamento inserido.");
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                log.Critico(ETAPA, COMPONENTE,
                    $"Transaction de inserção falhou: {ex.Message}",
                    detalhes: ex.StackTrace);

                resultado.Falhas = resultado.Total;
                resultado.Erros.Add($"Transaction: {ex.Message}");
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  LEVEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Associa o Level ao FamilyInstance.
        /// Estratégia 1: Level explícito do equipamento.
        /// Estratégia 2: Level mais próximo pela elevação do ponto.
        /// </summary>
        private static void AssociarLevel(
            Document doc,
            FamilyInstance instancia,
            EquipamentoHidraulico equipamento,
            XYZ pontoInsercao,
            ILogService log)
        {
            try
            {
                // Tentar parâmetro de Level na instância
                var levelParam = instancia.get_Parameter(
                    BuiltInParameter.FAMILY_LEVEL_PARAM);

                if (levelParam == null || levelParam.IsReadOnly)
                {
                    levelParam = instancia.get_Parameter(
                        BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
                }

                if (levelParam == null || levelParam.IsReadOnly)
                    return;

                // Buscar Level mais próximo
                var level = BuscarLevelMaisProximo(doc, pontoInsercao);

                if (level != null)
                {
                    levelParam.Set(level.Id);
                }
                else
                {
                    log.Medio(ETAPA, COMPONENTE,
                        $"Level não encontrado para equipamento {equipamento.Id} " +
                        $"na elevação Z={pontoInsercao.Z:F3} pés.",
                        equipamento.RevitElementId);
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao associar Level ao equipamento {equipamento.Id}: {ex.Message}",
                    equipamento.RevitElementId);
            }
        }

        /// <summary>
        /// Busca Level mais próximo da elevação Z do ponto de inserção.
        /// </summary>
        private static Level? BuscarLevelMaisProximo(Document doc, XYZ ponto)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => Math.Abs(l.Elevation - ponto.Z))
                .ToList();

            return levels.FirstOrDefault();
        }

        // ══════════════════════════════════════════════════════════
        //  PARÂMETROS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Preenche parâmetros do equipamento na FamilyInstance.
        /// Busca parâmetros por BuiltInParameter e por nome.
        /// </summary>
        private static void PreencherParametros(
            FamilyInstance instancia,
            EquipamentoHidraulico equipamento,
            ILogService log)
        {
            // Mark
            TentarSetString(instancia, BuiltInParameter.ALL_MODEL_MARK,
                equipamento.Mark, log);

            // Comments — tipo e info do equipamento
            TentarSetString(instancia, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS,
                $"Tipo={equipamento.Tipo} | Auto={true}", log);

            // Diâmetro AF
            if (equipamento.DiametroAF > 0)
            {
                TentarSetPorNome(instancia, "Diameter", equipamento.DiametroAF, log);
                TentarSetPorNome(instancia, "Diametro", equipamento.DiametroAF, log);
                TentarSetPorNome(instancia, "Nominal Diameter", equipamento.DiametroAF, log);
            }

            // Diâmetro ES
            if (equipamento.DiametroES > 0)
            {
                TentarSetPorNome(instancia, "Drain Size", equipamento.DiametroES, log);
                TentarSetPorNome(instancia, "Drain Diameter", equipamento.DiametroES, log);
            }
        }

        /// <summary>
        /// Tenta definir parâmetro String via BuiltInParameter.
        /// </summary>
        private static void TentarSetString(
            Element element, BuiltInParameter bip, string valor, ILogService log)
        {
            if (string.IsNullOrEmpty(valor))
                return;

            try
            {
                var param = element.get_Parameter(bip);
                if (param != null && !param.IsReadOnly &&
                    param.StorageType == StorageType.String)
                {
                    param.Set(valor);
                }
            }
            catch { /* parâmetro indisponível — silencioso */ }
        }

        /// <summary>
        /// Tenta definir parâmetro Double por nome (case-insensitive).
        /// Valor em mm → converte para pés (unidade interna).
        /// </summary>
        private static void TentarSetPorNome(
            Element element, string nome, double valorMm, ILogService log)
        {
            try
            {
                var param = element.LookupParameter(nome);
                if (param == null)
                    return;

                if (param.IsReadOnly)
                    return;

                if (param.StorageType == StorageType.Double)
                {
                    // Converter mm → unidade interna (pés)
                    var valorInterno = UnitUtils.ConvertToInternalUnits(
                        valorMm, UnitTypeId.Millimeters);
                    param.Set(valorInterno);
                }
                else if (param.StorageType == StorageType.Integer)
                {
                    param.Set((int)valorMm);
                }
                else if (param.StorageType == StorageType.String)
                {
                    param.Set($"{valorMm:F0} mm");
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Parâmetro '{nome}' não pôde ser definido: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONVERSÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Converte ponto de pés (interno) para metros (display).
        /// </summary>
        private static (double X, double Y, double Z) ConverterPonto(XYZ point)
        {
            return (
                UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Meters),
                UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Meters),
                UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Meters)
            );
        }
    }
}
