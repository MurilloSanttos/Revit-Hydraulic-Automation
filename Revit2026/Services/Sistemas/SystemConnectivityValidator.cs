using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Sistemas
{
    /// <summary>
    /// Relatório de conectividade de um sistema MEP.
    /// </summary>
    public class ConnectivityReport
    {
        public bool SistemaConectado { get; set; }
        public int TotalElementos { get; set; }
        public int ElementosConectados { get; set; }
        public List<ElementId> ElementosDesconectados { get; set; } = new();
        public List<ElementId> ElementosComConectoresAbertos { get; set; } = new();
        public int TotalConectores { get; set; }
        public int ConectoresConectados { get; set; }
        public int ConectoresAbertos { get; set; }
        public List<string> Mensagens { get; set; } = new();

        public override string ToString() =>
            $"Conectividade: {(SistemaConectado ? "OK" : "FALHA")} | " +
            $"{ElementosConectados}/{TotalElementos} elementos, " +
            $"{ConectoresConectados}/{TotalConectores} conectores, " +
            $"{ElementosDesconectados.Count} desconectados, " +
            $"{ConectoresAbertos} abertos";
    }

    /// <summary>
    /// Validador de conectividade de sistemas MEP.
    /// Verifica se todos os elementos do sistema estão conectados
    /// usando BFS (busca em largura) no grafo de conectores.
    ///
    /// Detecta:
    /// - Elementos sem MEPModel ou conectores
    /// - Conectores abertos (não conectados)
    /// - Ilhas desconectadas no grafo
    /// </summary>
    public class SystemConnectivityValidator
    {
        private const string ETAPA = "06_Sistemas";
        private const string COMPONENTE = "Connectivity";

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida a conectividade de um sistema MEP.
        /// Verifica conexões entre todos os elementos do sistema.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="sistema">Sistema MEP a validar.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>Relatório de conectividade.</returns>
        public ConnectivityReport ValidarConectividade(
            Document doc,
            MEPSystem sistema,
            ILogService log)
        {
            var report = new ConnectivityReport();

            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar sistema ────────────────────────────
            if (sistema == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Validação de conectividade falhou: sistema é nulo.");
                report.Mensagens.Add("Sistema nulo.");
                return report;
            }

            // ── 2. Obter elementos do sistema ─────────────────
            var elementIds = ObterElementosDoSistema(sistema);

            if (elementIds.Count == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Validação de conectividade falhou: sistema " +
                    $"'{sistema.Name}' não possui elementos.",
                    sistema.Id.Value);
                report.Mensagens.Add("Sistema sem elementos.");
                return report;
            }

            report.TotalElementos = elementIds.Count;

            // ── 3. Validar conectores de cada elemento ────────
            var elementosComConectores = new Dictionary<long, List<Connector>>();
            var todosConectores = new List<Connector>();

            foreach (var id in elementIds)
            {
                var elem = doc.GetElement(id);
                if (elem == null)
                {
                    report.ElementosDesconectados.Add(id);
                    report.Mensagens.Add(
                        $"Elemento {id.Value} não encontrado no documento.");
                    continue;
                }

                var connSet = ObterConectores(elem);
                if (connSet == null || connSet.Size == 0)
                {
                    report.ElementosDesconectados.Add(id);
                    report.Mensagens.Add(
                        $"Elemento {id.Value} sem MEPModel ou conectores.");
                    log.Medio(ETAPA, COMPONENTE,
                        $"Elemento {id.Value} sem conectores.",
                        id.Value);
                    continue;
                }

                var conectoresDoElemento = new List<Connector>();
                bool temConectorAberto = false;

                foreach (Connector conn in connSet)
                {
                    if (conn.ConnectorType != ConnectorType.End &&
                        conn.ConnectorType != ConnectorType.Curve)
                        continue;

                    conectoresDoElemento.Add(conn);
                    todosConectores.Add(conn);
                    report.TotalConectores++;

                    if (conn.IsConnected)
                    {
                        report.ConectoresConectados++;
                    }
                    else
                    {
                        report.ConectoresAbertos++;
                        temConectorAberto = true;
                    }
                }

                if (temConectorAberto)
                {
                    report.ElementosComConectoresAbertos.Add(id);
                    report.Mensagens.Add(
                        $"Elemento {id.Value} possui conectores abertos.");
                    log.Leve(ETAPA, COMPONENTE,
                        $"Elemento {id.Value} possui conectores abertos.",
                        id.Value);
                }

                if (conectoresDoElemento.Count > 0)
                    elementosComConectores[id.Value] = conectoresDoElemento;
            }

            // ── 4. Construir grafo e executar BFS ─────────────
            var elementosVisitados = ExecutarBFS(elementosComConectores);

            // ── 5. Identificar elementos não visitados ────────
            foreach (var kvp in elementosComConectores)
            {
                var elemId = kvp.Key;

                if (!elementosVisitados.Contains(elemId))
                {
                    var id = new ElementId(elemId);
                    if (!report.ElementosDesconectados.Any(
                        e => e.Value == elemId))
                    {
                        report.ElementosDesconectados.Add(id);
                        report.Mensagens.Add(
                            $"Elemento {elemId} é uma ilha desconectada.");
                        log.Medio(ETAPA, COMPONENTE,
                            $"Elemento {elemId} isolado no grafo de conectividade.",
                            elemId);
                    }
                }
            }

            // ── 6. Calcular resultado ─────────────────────────
            report.ElementosConectados =
                report.TotalElementos - report.ElementosDesconectados.Count;

            report.SistemaConectado =
                report.ElementosDesconectados.Count == 0;

            // ── 7. Log final ──────────────────────────────────
            if (report.SistemaConectado)
            {
                log.Info(ETAPA, COMPONENTE,
                    $"Sistema '{sistema.Name}' validado. " +
                    $"Conectividade OK. {report}",
                    sistema.Id.Value);
            }
            else
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Sistema '{sistema.Name}' possui " +
                    $"{report.ElementosDesconectados.Count} elementos " +
                    $"desconectados. {report}",
                    sistema.Id.Value);
            }

            return report;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida conectividade de múltiplos sistemas.
        /// </summary>
        public Dictionary<ElementId, ConnectivityReport> ValidarMultiplos(
            Document doc,
            List<MEPSystem> sistemas,
            ILogService log)
        {
            var resultado = new Dictionary<ElementId, ConnectivityReport>();

            log.Info(ETAPA, COMPONENTE,
                $"Validando conectividade de {sistemas.Count} sistemas...");

            foreach (var sistema in sistemas)
            {
                var report = ValidarConectividade(doc, sistema, log);
                resultado[sistema.Id] = report;
            }

            var conectados = resultado.Values.Count(r => r.SistemaConectado);
            log.Info(ETAPA, COMPONENTE,
                $"Validação em lote concluída: " +
                $"{conectados}/{sistemas.Count} sistemas OK.");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  BFS — BUSCA EM LARGURA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa BFS no grafo de conectores para identificar
        /// quais elementos estão interconectados.
        /// </summary>
        private static HashSet<long> ExecutarBFS(
            Dictionary<long, List<Connector>> elementosComConectores)
        {
            var visitados = new HashSet<long>();

            if (elementosComConectores.Count == 0)
                return visitados;

            // Construir grafo: elementId → set de elementIds adjacentes
            var grafo = new Dictionary<long, HashSet<long>>();

            foreach (var kvp in elementosComConectores)
            {
                if (!grafo.ContainsKey(kvp.Key))
                    grafo[kvp.Key] = new HashSet<long>();

                foreach (var conn in kvp.Value)
                {
                    if (!conn.IsConnected) continue;

                    try
                    {
                        foreach (Connector other in conn.AllRefs)
                        {
                            if (other.Owner == null) continue;

                            var otherId = other.Owner.Id.Value;
                            if (otherId == kvp.Key) continue;

                            grafo[kvp.Key].Add(otherId);

                            if (!grafo.ContainsKey(otherId))
                                grafo[otherId] = new HashSet<long>();
                            grafo[otherId].Add(kvp.Key);
                        }
                    }
                    catch { /* conector sem AllRefs */ }
                }
            }

            // BFS a partir do primeiro elemento
            var primeiro = elementosComConectores.Keys.First();
            var fila = new Queue<long>();
            fila.Enqueue(primeiro);
            visitados.Add(primeiro);

            while (fila.Count > 0)
            {
                var atual = fila.Dequeue();

                if (!grafo.ContainsKey(atual))
                    continue;

                foreach (var vizinho in grafo[atual])
                {
                    if (visitados.Contains(vizinho))
                        continue;

                    visitados.Add(vizinho);
                    fila.Enqueue(vizinho);
                }
            }

            return visitados;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém lista de ElementIds dos elementos do sistema.
        /// </summary>
        private static List<ElementId> ObterElementosDoSistema(MEPSystem sistema)
        {
            var ids = new List<ElementId>();

            try
            {
                var elements = sistema.Elements;
                if (elements == null) return ids;

                foreach (Element elem in elements)
                {
                    if (elem?.Id != null)
                        ids.Add(elem.Id);
                }
            }
            catch { /* sistema sem elementos acessíveis */ }

            return ids;
        }

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
