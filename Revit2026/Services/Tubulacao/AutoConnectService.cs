using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Tubulacao
{
    /// <summary>
    /// Serviço de conexão automática entre elementos MEP (Pipes, Fittings).
    /// Busca pares de conectores compatíveis (Domain, Shape, Diâmetro)
    /// e conecta via ConnectTo.
    ///
    /// Suporta conexão individual, em cadeia e batch.
    /// </summary>
    public class AutoConnectService
    {
        private const string ETAPA = "05_Tubulacao";
        private const string COMPONENTE = "AutoConnect";

        // Tolerância de diâmetro: 1 mm em pés
        private static readonly double ToleranciaDialetro =
            UnitUtils.ConvertToInternalUnits(1, UnitTypeId.Millimeters);

        // Distância máxima entre conectores para auto-conexão
        private static readonly double DistanciaMaxima =
            UnitUtils.ConvertToInternalUnits(5, UnitTypeId.Millimeters);

        // ══════════════════════════════════════════════════════════
        //  CONEXÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Conecta automaticamente dois elementos MEP.
        /// Procura o melhor par de conectores compatíveis.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public bool Conectar(
            Document doc,
            ElementId idA,
            ElementId idB,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar IDs ────────────────────────────────
            if (idA == null || idA == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Não foi possível conectar: ElementId A é inválido.");
                return false;
            }

            if (idB == null || idB == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Não foi possível conectar: ElementId B é inválido.");
                return false;
            }

            if (idA.Value == idB.Value)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Tentativa de conectar elemento a si mesmo: {idA.Value}.");
                return false;
            }

            // ── 2. Obter elementos ────────────────────────────
            var elemA = doc.GetElement(idA);
            var elemB = doc.GetElement(idB);

            if (elemA == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Não foi possível conectar: elemento A ({idA.Value}) não encontrado.");
                return false;
            }

            if (elemB == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Não foi possível conectar: elemento B ({idB.Value}) não encontrado.");
                return false;
            }

            // ── 3. Obter conectores ───────────────────────────
            var connectorsA = ObterConectores(elemA);
            var connectorsB = ObterConectores(elemB);

            if (connectorsA == null || connectorsA.Size == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Não foi possível conectar {idA.Value} e {idB.Value}: " +
                    $"elemento A não possui conectores.",
                    idA.Value);
                return false;
            }

            if (connectorsB == null || connectorsB.Size == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Não foi possível conectar {idA.Value} e {idB.Value}: " +
                    $"elemento B não possui conectores.",
                    idB.Value);
                return false;
            }

            // ── 4. Buscar par compatível ──────────────────────
            var par = BuscarParCompativel(connectorsA, connectorsB);

            if (par == null)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Connectors incompatíveis entre {idA.Value} e {idB.Value}. " +
                    $"Verifique Domain, Shape e Diâmetro.",
                    idA.Value);
                return false;
            }

            // ── 5. Verificar se já conectados ─────────────────
            if (JaConectados(par.Value.A, par.Value.B))
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Elementos {idA.Value} e {idB.Value} já estão conectados.",
                    idA.Value);
                return true;
            }

            // ── 6. Conectar ───────────────────────────────────
            try
            {
                par.Value.A.ConnectTo(par.Value.B);

                if (par.Value.A.IsConnected)
                {
                    var diamMm = UnitUtils.ConvertFromInternalUnits(
                        par.Value.A.Radius * 2, UnitTypeId.Millimeters);

                    log.Info(ETAPA, COMPONENTE,
                        $"Conectados: {idA.Value} ↔ {idB.Value} " +
                        $"(Ø{diamMm:F0} mm, dist={par.Value.Distancia:F3} pés)",
                        idA.Value);
                    return true;
                }

                log.Critico(ETAPA, COMPONENTE,
                    $"Não foi possível conectar {idA.Value} e {idB.Value}: " +
                    $"ConnectTo não resultou em conexão.",
                    idA.Value);
                return false;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Não foi possível conectar {idA.Value} e {idB.Value}: " +
                    $"{ex.Message}",
                    idA.Value,
                    detalhes: ex.StackTrace);
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONEXÃO EM CADEIA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Conecta uma sequência de elementos em cadeia (A→B→C→D).
        /// </summary>
        public int ConectarCadeia(
            Document doc,
            List<ElementId> ids,
            ILogService log)
        {
            if (ids == null || ids.Count < 2)
                return 0;

            int conectados = 0;

            log.Info(ETAPA, COMPONENTE,
                $"Conectando cadeia: {ids.Count} elementos, " +
                $"{ids.Count - 1} conexões...");

            for (int i = 0; i < ids.Count - 1; i++)
            {
                if (Conectar(doc, ids[i], ids[i + 1], log))
                    conectados++;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Cadeia concluída: {conectados}/{ids.Count - 1} conexões.");

            return conectados;
        }

        // ══════════════════════════════════════════════════════════
        //  CONEXÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Conecta pares de elementos.
        /// </summary>
        public int ConectarLote(
            Document doc,
            List<(ElementId A, ElementId B)> pares,
            ILogService log)
        {
            int conectados = 0;

            log.Info(ETAPA, COMPONENTE,
                $"Conectando {pares.Count} pares...");

            foreach (var (a, b) in pares)
            {
                if (Conectar(doc, a, b, log))
                    conectados++;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Lote concluído: {conectados}/{pares.Count} conexões.");

            return conectados;
        }

        // ══════════════════════════════════════════════════════════
        //  BUSCA DE CONECTORES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado da busca de par compatível.
        /// </summary>
        private struct ParConectores
        {
            public Connector A;
            public Connector B;
            public double Distancia;
        }

        /// <summary>
        /// Busca o melhor par de conectores compatíveis entre dois conjuntos.
        /// Prioriza: menor distância → mesmo Domain → mesmo Shape → diâmetro compatível.
        /// </summary>
        private ParConectores? BuscarParCompativel(
            ConnectorSet setA, ConnectorSet setB)
        {
            ParConectores? melhor = null;

            foreach (Connector cA in setA)
            {
                if (!EhConectorValido(cA))
                    continue;

                foreach (Connector cB in setB)
                {
                    if (!EhConectorValido(cB))
                        continue;

                    // Verificar compatibilidade
                    if (!SaoCompativeis(cA, cB))
                        continue;

                    var distancia = cA.Origin.DistanceTo(cB.Origin);

                    if (melhor == null || distancia < melhor.Value.Distancia)
                    {
                        melhor = new ParConectores
                        {
                            A = cA,
                            B = cB,
                            Distancia = distancia
                        };
                    }
                }
            }

            return melhor;
        }

        /// <summary>
        /// Verifica se um conector é válido para conexão.
        /// </summary>
        private static bool EhConectorValido(Connector conn)
        {
            if (conn == null) return false;

            // Aceitar End, Curve e Physical
            return conn.ConnectorType == ConnectorType.End ||
                   conn.ConnectorType == ConnectorType.Curve ||
                   conn.ConnectorType == ConnectorType.Physical;
        }

        /// <summary>
        /// Verifica compatibilidade entre dois conectores.
        /// </summary>
        private static bool SaoCompativeis(Connector a, Connector b)
        {
            // Mesmo Domain
            if (a.Domain != b.Domain)
                return false;

            // Mesmo Shape
            if (a.Shape != b.Shape)
                return false;

            // Diâmetro compatível (tolerância ± 1mm)
            if (a.Shape == ConnectorProfileType.Round)
            {
                if (!DiametroCompativel(a.Radius * 2, b.Radius * 2))
                    return false;
            }

            // Nenhum já conectado ao outro elemento
            if (a.IsConnected && EhConectadoA(a, b.Owner))
                return false;

            return true;
        }

        /// <summary>
        /// Verifica se o diâmetro é compatível dentro da tolerância.
        /// </summary>
        private static bool DiametroCompativel(double d1, double d2)
        {
            return Math.Abs(d1 - d2) <= ToleranciaDialetro;
        }

        /// <summary>
        /// Verifica se um conector já está conectado a um elemento específico.
        /// </summary>
        private static bool EhConectadoA(Connector conn, Element target)
        {
            try
            {
                if (!conn.IsConnected || target == null)
                    return false;

                foreach (Connector other in conn.AllRefs)
                {
                    if (other.Owner.Id.Value == target.Id.Value)
                        return true;
                }
            }
            catch { /* silencioso */ }

            return false;
        }

        /// <summary>
        /// Verifica se dois conectores já estão conectados entre si.
        /// </summary>
        private static bool JaConectados(Connector a, Connector b)
        {
            try
            {
                if (!a.IsConnected) return false;

                foreach (Connector other in a.AllRefs)
                {
                    if (other.Owner.Id.Value == b.Owner.Id.Value)
                        return true;
                }
            }
            catch { /* silencioso */ }

            return false;
        }

        // ══════════════════════════════════════════════════════════
        //  OBTER CONECTORES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém ConnectorSet de qualquer elemento MEP.
        /// </summary>
        private static ConnectorSet? ObterConectores(Element element)
        {
            try
            {
                // MEPCurve (Pipe, Duct, etc.)
                if (element is MEPCurve mepCurve)
                    return mepCurve.ConnectorManager?.Connectors;

                // FamilyInstance (Fittings, Equipment)
                if (element is FamilyInstance fi)
                    return fi.MEPModel?.ConnectorManager?.Connectors;
            }
            catch { /* silencioso */ }

            return null;
        }
    }
}
