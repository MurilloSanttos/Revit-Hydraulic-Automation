using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Tubulacao
{
    /// <summary>
    /// Serviço de criação de conexões de tubulação (Pipe Fittings):
    /// - Cotovelos (Elbow) — curvas 90° e 45°
    /// - Tees — derivações
    /// - Reduções (Transition) — mudança de diâmetro
    ///
    /// Usa doc.Create.NewElbowFitting, NewTeeFitting e NewTransitionFitting.
    /// Deve ser chamado dentro de uma Transaction ativa.
    /// </summary>
    public class PipeFittingService
    {
        private const string ETAPA = "05_Tubulacao";
        private const string COMPONENTE = "PipeFitting";

        // ══════════════════════════════════════════════════════════
        //  COTOVELO (ELBOW)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um cotovelo (elbow fitting) conectando dois conectores.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="origem">Conector de origem (extremidade do pipe 1).</param>
        /// <param name="destino">Conector de destino (extremidade do pipe 2).</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>ElementId do fitting criado, ou InvalidElementId.</returns>
        public ElementId CriarCurva(
            Document doc,
            Connector origem,
            Connector destino,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            if (origem == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar cotovelo: conector de origem é nulo.");
                return ElementId.InvalidElementId;
            }

            if (destino == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar cotovelo: conector de destino é nulo.");
                return ElementId.InvalidElementId;
            }

            try
            {
                var fitting = doc.Create.NewElbowFitting(origem, destino);

                if (fitting == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        "Falha ao criar cotovelo: Revit retornou null. " +
                        $"Verifique se os conectores são compatíveis " +
                        $"(mesmo sistema, diâmetros adequados).");
                    return ElementId.InvalidElementId;
                }

                var angulo = CalcularAngulo(origem, destino);

                log.Info(ETAPA, COMPONENTE,
                    $"Conexão criada: Id={fitting.Id.Value} (Cotovelo {angulo})",
                    fitting.Id.Value);

                return fitting.Id;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar cotovelo: {ex.Message}",
                    detalhes: ex.StackTrace);
                return ElementId.InvalidElementId;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  TEE (DERIVAÇÃO)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um tee (derivação) conectando tubo principal e derivação.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="principal1">Conector do trecho principal (lado 1).</param>
        /// <param name="principal2">Conector do trecho principal (lado 2).</param>
        /// <param name="derivacao">Conector do trecho de derivação.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>ElementId do fitting criado, ou InvalidElementId.</returns>
        public ElementId CriarTe(
            Document doc,
            Connector principal1,
            Connector principal2,
            Connector derivacao,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            if (principal1 == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar tee: conector principal 1 é nulo.");
                return ElementId.InvalidElementId;
            }

            if (principal2 == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar tee: conector principal 2 é nulo.");
                return ElementId.InvalidElementId;
            }

            if (derivacao == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar tee: conector de derivação é nulo.");
                return ElementId.InvalidElementId;
            }

            try
            {
                var fitting = doc.Create.NewTeeFitting(
                    principal1, principal2, derivacao);

                if (fitting == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        "Falha ao criar tee: Revit retornou null. " +
                        "Verifique conectores e alinhamento geométrico.");
                    return ElementId.InvalidElementId;
                }

                var diamPrinc = DiametroMm(principal1);
                var diamDeriv = DiametroMm(derivacao);

                log.Info(ETAPA, COMPONENTE,
                    $"Conexão criada: Id={fitting.Id.Value} " +
                    $"(Tee Ø{diamPrinc:F0}×Ø{diamDeriv:F0} mm)",
                    fitting.Id.Value);

                return fitting.Id;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar tee: {ex.Message}",
                    detalhes: ex.StackTrace);
                return ElementId.InvalidElementId;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  REDUÇÃO (TRANSITION)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria uma redução (transition) entre dois diâmetros diferentes.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="maior">Conector do lado de maior diâmetro.</param>
        /// <param name="menor">Conector do lado de menor diâmetro.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>ElementId do fitting criado, ou InvalidElementId.</returns>
        public ElementId CriarReducao(
            Document doc,
            Connector maior,
            Connector menor,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            if (maior == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar redução: conector maior é nulo.");
                return ElementId.InvalidElementId;
            }

            if (menor == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar redução: conector menor é nulo.");
                return ElementId.InvalidElementId;
            }

            try
            {
                var fitting = doc.Create.NewTransitionFitting(maior, menor);

                if (fitting == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        "Falha ao criar redução: Revit retornou null. " +
                        "Verifique diâmetros e alinhamento.");
                    return ElementId.InvalidElementId;
                }

                var diamMaior = DiametroMm(maior);
                var diamMenor = DiametroMm(menor);

                log.Info(ETAPA, COMPONENTE,
                    $"Conexão criada: Id={fitting.Id.Value} " +
                    $"(Redução Ø{diamMaior:F0} → Ø{diamMenor:F0} mm)",
                    fitting.Id.Value);

                return fitting.Id;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar redução: {ex.Message}",
                    detalhes: ex.StackTrace);
                return ElementId.InvalidElementId;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONEXÃO AUTOMÁTICA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Conecta dois pipes automaticamente, detectando o tipo de
        /// fitting necessário (cotovelo, redução ou direto).
        /// </summary>
        public ElementId ConectarAutomatico(
            Document doc,
            Pipe pipe1,
            Pipe pipe2,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (pipe1 == null || pipe2 == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha na conexão automática: pipe nulo.");
                return ElementId.InvalidElementId;
            }

            try
            {
                // Encontrar conectores mais próximos
                var (conn1, conn2) = EncontrarConectoresMaisProximos(pipe1, pipe2);

                if (conn1 == null || conn2 == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        $"Conectores não encontrados entre Pipe {pipe1.Id.Value} " +
                        $"e Pipe {pipe2.Id.Value}.");
                    return ElementId.InvalidElementId;
                }

                // Determinar tipo de conexão
                var diam1 = conn1.Radius * 2;
                var diam2 = conn2.Radius * 2;
                var angulo = AnguloEntreConectores(conn1, conn2);

                // Diâmetros diferentes → redução
                if (Math.Abs(diam1 - diam2) > 1e-6)
                {
                    var maior = diam1 > diam2 ? conn1 : conn2;
                    var menor = diam1 > diam2 ? conn2 : conn1;
                    return CriarReducao(doc, maior, menor, log);
                }

                // Ângulo significativo → cotovelo
                if (angulo > 0.1) // > ~5.7°
                {
                    return CriarCurva(doc, conn1, conn2, log);
                }

                // Alinhados e mesmo diâmetro → conexão direta
                try
                {
                    conn1.ConnectTo(conn2);
                    log.Info(ETAPA, COMPONENTE,
                        $"Conexão direta entre Pipe {pipe1.Id.Value} " +
                        $"e Pipe {pipe2.Id.Value}.");
                    return pipe2.Id;
                }
                catch
                {
                    return CriarCurva(doc, conn1, conn2, log);
                }
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha na conexão automática: {ex.Message}",
                    detalhes: ex.StackTrace);
                return ElementId.InvalidElementId;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Encontra o par de conectores mais próximos entre dois elementos.
        /// </summary>
        private static (Connector?, Connector?) EncontrarConectoresMaisProximos(
            Element elem1, Element elem2)
        {
            var connSet1 = ObterConectores(elem1);
            var connSet2 = ObterConectores(elem2);

            if (connSet1 == null || connSet2 == null)
                return (null, null);

            Connector? melhor1 = null;
            Connector? melhor2 = null;
            double menorDist = double.MaxValue;

            foreach (Connector c1 in connSet1)
            {
                if (!c1.IsConnected || c1.ConnectorType != ConnectorType.End)
                {
                    foreach (Connector c2 in connSet2)
                    {
                        if (!c2.IsConnected || c2.ConnectorType != ConnectorType.End)
                        {
                            var dist = c1.Origin.DistanceTo(c2.Origin);
                            if (dist < menorDist)
                            {
                                menorDist = dist;
                                melhor1 = c1;
                                melhor2 = c2;
                            }
                        }
                    }
                }
            }

            return (melhor1, melhor2);
        }

        /// <summary>
        /// Obtém ConnectorSet de um elemento MEP.
        /// </summary>
        private static ConnectorSet? ObterConectores(Element element)
        {
            try
            {
                if (element is MEPCurve mepCurve)
                    return mepCurve.ConnectorManager?.Connectors;

                if (element is FamilyInstance fi)
                    return fi.MEPModel?.ConnectorManager?.Connectors;
            }
            catch { /* silencioso */ }

            return null;
        }

        /// <summary>
        /// Calcula ângulo entre as direções de dois conectores (radianos).
        /// </summary>
        private static double AnguloEntreConectores(Connector c1, Connector c2)
        {
            try
            {
                var dir1 = c1.CoordinateSystem.BasisZ;
                var dir2 = c2.CoordinateSystem.BasisZ;

                var dot = dir1.DotProduct(dir2);
                dot = Math.Max(-1.0, Math.Min(1.0, dot));

                return Math.Acos(Math.Abs(dot));
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Calcula string de ângulo entre dois conectores para log.
        /// </summary>
        private static string CalcularAngulo(Connector c1, Connector c2)
        {
            try
            {
                var angRad = AnguloEntreConectores(c1, c2);
                var angGraus = angRad * 180.0 / Math.PI;
                return $"{angGraus:F0}°";
            }
            catch
            {
                return "?°";
            }
        }

        /// <summary>
        /// Obtém diâmetro de um conector em milímetros.
        /// </summary>
        private static double DiametroMm(Connector conn)
        {
            try
            {
                return UnitUtils.ConvertFromInternalUnits(
                    conn.Radius * 2, UnitTypeId.Millimeters);
            }
            catch
            {
                return 0;
            }
        }
    }
}
