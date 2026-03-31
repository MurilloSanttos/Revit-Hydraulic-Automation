using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Sistemas
{
    /// <summary>
    /// Serviço de atribuição de elementos MEP a sistemas hidráulicos.
    /// Vincula conectores de Pipes, Fittings e equipamentos a
    /// PipingSystems existentes (Água Fria, Esgoto, Ventilação).
    ///
    /// Também executa auto-conexão de conectores próximos (≤ 5mm)
    /// com diâmetro e Domain compatíveis.
    /// </summary>
    public class SystemAssignmentService
    {
        private const string ETAPA = "06_Sistemas";
        private const string COMPONENTE = "SystemAssign";

        // Tolerância de proximidade para auto-conexão: 5 mm em pés
        private static readonly double ToleranciaProximidade =
            UnitUtils.ConvertToInternalUnits(5, UnitTypeId.Millimeters);

        // Tolerância de diâmetro: 1 mm em pés
        private static readonly double ToleranciaDiametro =
            UnitUtils.ConvertToInternalUnits(1, UnitTypeId.Millimeters);

        // ══════════════════════════════════════════════════════════
        //  ATRIBUIÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Atribui elementos a um sistema MEP existente.
        /// Vincula conectores compatíveis e executa auto-conexão.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="sistema">Sistema MEP de destino.</param>
        /// <param name="elementos">ElementIds a serem atribuídos.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>true se pelo menos um elemento foi atribuído.</returns>
        public bool AtribuirElementosAoSistema(
            Document doc,
            MEPSystem sistema,
            IList<ElementId> elementos,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar sistema ────────────────────────────
            if (sistema == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao atribuir elementos: sistema é nulo.");
                return false;
            }

            // ── 2. Validar elementos ──────────────────────────
            if (elementos == null || elementos.Count == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao atribuir elementos ao sistema " +
                    $"'{sistema.Name}': lista de elementos vazia.");
                return false;
            }

            int adicionados = 0;
            int ignorados = 0;
            var conectoresDoSistema = new List<Connector>();

            try
            {
                // ── 3. Processar cada elemento ────────────────
                foreach (var id in elementos)
                {
                    if (id == null || id == ElementId.InvalidElementId)
                        continue;

                    var elem = doc.GetElement(id);
                    if (elem == null)
                    {
                        log.Medio(ETAPA, COMPONENTE,
                            $"Elemento {id.Value} ignorado " +
                            $"(não encontrado no documento).");
                        ignorados++;
                        continue;
                    }

                    // Obter conectores do elemento
                    var connSet = ObterConectores(elem);
                    if (connSet == null || connSet.Size == 0)
                    {
                        log.Medio(ETAPA, COMPONENTE,
                            $"Elemento {id.Value} ignorado " +
                            $"(sem MEPModel ou conectores compatíveis).",
                            id.Value);
                        ignorados++;
                        continue;
                    }

                    // Tentar adicionar conectores ao sistema
                    bool elementoAdicionado = false;

                    foreach (Connector conn in connSet)
                    {
                        if (!EhConectorCompativel(conn, sistema))
                            continue;

                        try
                        {
                            // Adicionar conector ao sistema
                            if (!conn.IsConnected)
                            {
                                sistema.Add(conn);
                                elementoAdicionado = true;
                                conectoresDoSistema.Add(conn);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Leve(ETAPA, COMPONENTE,
                                $"Conector do elemento {id.Value} " +
                                $"não pôde ser adicionado: {ex.Message}",
                                id.Value);
                        }
                    }

                    if (elementoAdicionado)
                    {
                        adicionados++;
                    }
                    else
                    {
                        log.Medio(ETAPA, COMPONENTE,
                            $"Elemento {id.Value} ignorado " +
                            $"(conectores incompatíveis com sistema " +
                            $"'{sistema.Name}').",
                            id.Value);
                        ignorados++;
                    }
                }

                // ── 4. Auto-conexão de conectores próximos ────
                int conexoes = AutoConectarProximos(doc, elementos, log);

                // ── 5. Resumo ─────────────────────────────────
                if (adicionados > 0)
                {
                    log.Info(ETAPA, COMPONENTE,
                        $"Atribuição concluída: {adicionados} elementos " +
                        $"adicionados ao sistema '{sistema.Name}' " +
                        $"(Id={sistema.Id.Value}). " +
                        $"{ignorados} ignorados, {conexoes} auto-conexões.",
                        sistema.Id.Value);
                    return true;
                }

                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao atribuir elementos ao sistema " +
                    $"'{sistema.Name}': nenhum elemento adicionado " +
                    $"({ignorados} ignorados de {elementos.Count} fornecidos).",
                    sistema.Id.Value);
                return false;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao atribuir elementos ao sistema " +
                    $"'{sistema.Name}': {ex.Message}",
                    sistema.Id.Value,
                    detalhes: ex.StackTrace);
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ATRIBUIÇÃO EM LOTE (MÚLTIPLOS SISTEMAS)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado da atribuição em lote.
        /// </summary>
        public class ResultadoAtribuicaoLote
        {
            public int SistemasProcessados { get; set; }
            public int Sucesso { get; set; }
            public int Falhas { get; set; }

            public override string ToString() =>
                $"{Sucesso}/{SistemasProcessados} sistemas atribuídos, " +
                $"{Falhas} falhas";
        }

        /// <summary>
        /// Atribui elementos a múltiplos sistemas em lote.
        /// </summary>
        public ResultadoAtribuicaoLote AtribuirLote(
            Document doc,
            List<(MEPSystem Sistema, IList<ElementId> Elementos)> atribuicoes,
            ILogService log)
        {
            var resultado = new ResultadoAtribuicaoLote
            {
                SistemasProcessados = atribuicoes.Count
            };

            log.Info(ETAPA, COMPONENTE,
                $"Atribuição em lote: {atribuicoes.Count} sistemas...");

            foreach (var (sistema, elementos) in atribuicoes)
            {
                if (AtribuirElementosAoSistema(doc, sistema, elementos, log))
                    resultado.Sucesso++;
                else
                    resultado.Falhas++;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Atribuição em lote concluída: {resultado}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  AUTO-CONEXÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Conecta automaticamente conectores próximos (≤ 5mm)
        /// que sejam compatíveis em Domain, Shape e diâmetro.
        /// </summary>
        private int AutoConectarProximos(
            Document doc,
            IList<ElementId> elementIds,
            ILogService log)
        {
            // Coletar todos os conectores livres
            var conectoresLivres = new List<Connector>();

            foreach (var id in elementIds)
            {
                if (id == null || id == ElementId.InvalidElementId)
                    continue;

                var elem = doc.GetElement(id);
                if (elem == null) continue;

                var connSet = ObterConectores(elem);
                if (connSet == null) continue;

                foreach (Connector conn in connSet)
                {
                    if (!conn.IsConnected &&
                        (conn.ConnectorType == ConnectorType.End ||
                         conn.ConnectorType == ConnectorType.Curve))
                    {
                        conectoresLivres.Add(conn);
                    }
                }
            }

            if (conectoresLivres.Count < 2)
                return 0;

            int conexoes = 0;

            // Buscar pares próximos e compatíveis
            for (int i = 0; i < conectoresLivres.Count; i++)
            {
                if (conectoresLivres[i].IsConnected)
                    continue;

                for (int j = i + 1; j < conectoresLivres.Count; j++)
                {
                    if (conectoresLivres[j].IsConnected)
                        continue;

                    // Mesmo elemento → ignorar
                    if (conectoresLivres[i].Owner.Id.Value ==
                        conectoresLivres[j].Owner.Id.Value)
                        continue;

                    // Verificar proximidade
                    var dist = conectoresLivres[i].Origin
                        .DistanceTo(conectoresLivres[j].Origin);

                    if (dist > ToleranciaProximidade)
                        continue;

                    // Verificar compatibilidade
                    if (!SaoConectoresCompativeis(
                        conectoresLivres[i], conectoresLivres[j]))
                        continue;

                    // Conectar
                    try
                    {
                        conectoresLivres[i].ConnectTo(conectoresLivres[j]);
                        conexoes++;
                    }
                    catch (Exception ex)
                    {
                        log.Leve(ETAPA, COMPONENTE,
                            $"Auto-conexão falhou entre " +
                            $"{conectoresLivres[i].Owner.Id.Value} e " +
                            $"{conectoresLivres[j].Owner.Id.Value}: " +
                            $"{ex.Message}");
                    }
                }
            }

            if (conexoes > 0)
            {
                log.Info(ETAPA, COMPONENTE,
                    $"{conexoes} auto-conexões realizadas (≤ 5mm).");
            }

            return conexoes;
        }

        // ══════════════════════════════════════════════════════════
        //  COMPATIBILIDADE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se um conector é compatível com o sistema destino.
        /// </summary>
        private static bool EhConectorCompativel(Connector conn, MEPSystem sistema)
        {
            try
            {
                if (conn == null) return false;

                // Aceitar End e Curve
                if (conn.ConnectorType != ConnectorType.End &&
                    conn.ConnectorType != ConnectorType.Curve)
                    return false;

                // Domain deve ser piping para sistemas hidráulicos
                if (conn.Domain != Domain.DomainPiping)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica compatibilidade entre dois conectores para auto-conexão.
        /// </summary>
        private static bool SaoConectoresCompativeis(Connector a, Connector b)
        {
            try
            {
                // Mesmo Domain
                if (a.Domain != b.Domain)
                    return false;

                // Mesmo Shape
                if (a.Shape != b.Shape)
                    return false;

                // Diâmetro compatível (±1mm)
                if (a.Shape == ConnectorProfileType.Round)
                {
                    var diamA = a.Radius * 2;
                    var diamB = b.Radius * 2;
                    if (Math.Abs(diamA - diamB) > ToleranciaDiametro)
                        return false;
                }

                // FlowDirection compatível
                if (a.Direction == FlowDirectionType.Out &&
                    b.Direction == FlowDirectionType.Out)
                    return false;

                if (a.Direction == FlowDirectionType.In &&
                    b.Direction == FlowDirectionType.In)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém ConnectorSet de qualquer elemento MEP.
        /// </summary>
        private static ConnectorSet? ObterConectores(Element element)
        {
            try
            {
                if (element is MEPCurve mep)
                    return mep.ConnectorManager?.Connectors;

                if (element is FamilyInstance fi)
                    return fi.MEPModel?.ConnectorManager?.Connectors;
            }
            catch { /* silencioso */ }

            return null;
        }
    }
}
