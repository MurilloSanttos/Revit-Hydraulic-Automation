using Autodesk.Revit.DB;
using PluginCore.Interfaces;

namespace Revit2026.Services.Posicionamento
{
    /// <summary>
    /// Serviço de rotação automática de equipamentos MEP.
    /// Alinha FamilyInstances com a orientação da parede mais próxima,
    /// garantindo que o equipamento fique "de frente" para o ambiente.
    ///
    /// Algoritmo:
    /// 1. Obtém vetor direcional da parede (LocationCurve)
    /// 2. Obtém vetor forward do equipamento (BasisX)
    /// 3. Calcula ângulo via DotProduct + CrossProduct
    /// 4. Aplica rotação via ElementTransformUtils.RotateElement
    /// </summary>
    public class RotationService
    {
        private const string ETAPA = "04_Insercao";
        private const string COMPONENTE = "Rotation";
        private const double TOLERANCIA_ANGULAR = 0.01; // ~0.57°

        // ══════════════════════════════════════════════════════════
        //  ROTAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Ajusta a rotação do equipamento para alinhá-lo com a parede.
        /// Deve ser chamado dentro de uma Transaction ativa,
        /// ou cria uma Transaction interna se necessário.
        /// </summary>
        public void AjustarRotacaoParaParede(
            Document doc,
            FamilyInstance equipamento,
            Wall parede,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (equipamento == null) throw new ArgumentNullException(nameof(equipamento));
            if (parede == null) throw new ArgumentNullException(nameof(parede));
            if (log == null) throw new ArgumentNullException(nameof(log));

            try
            {
                // ── 1. Vetor direcional da parede ─────────────
                var dirParede = ObterDirecaoParede(parede);
                if (dirParede == null)
                {
                    log.Medio(ETAPA, COMPONENTE,
                        $"Parede {parede.Id.Value} sem LocationCurve. " +
                        $"Rotação não aplicada.",
                        equipamento.Id.Value);
                    return;
                }

                // ── 2. Vetor forward do equipamento ───────────
                var transform = equipamento.GetTotalTransform();
                var forward = transform.BasisX.Normalize();
                var origin = transform.Origin;

                // ── 3. Calcular ângulo ────────────────────────
                var angulo = CalcularAngulo(forward, dirParede);

                // ── 4. Verificar se rotação é necessária ──────
                if (Math.Abs(angulo) < TOLERANCIA_ANGULAR)
                {
                    log.Leve(ETAPA, COMPONENTE,
                        $"Rotação não necessária: equipamento {equipamento.Id.Value} " +
                        $"já está alinhado (Δ={GrausStr(angulo)}).",
                        equipamento.Id.Value);
                    return;
                }

                // ── 5. Aplicar rotação ────────────────────────
                var eixo = Line.CreateBound(origin, origin + XYZ.BasisZ);

                bool transInterna = !doc.IsModifiable;

                if (transInterna)
                {
                    using var trans = new Transaction(doc, "Rotacionar Equipamento");
                    trans.Start();
                    ElementTransformUtils.RotateElement(doc, equipamento.Id, eixo, angulo);
                    trans.Commit();
                }
                else
                {
                    ElementTransformUtils.RotateElement(doc, equipamento.Id, eixo, angulo);
                }

                // ── 6. Log ────────────────────────────────────
                log.Info(ETAPA, COMPONENTE,
                    $"Rotação ajustada: equipamento {equipamento.Id.Value} " +
                    $"alinhado à parede {parede.Id.Value} " +
                    $"(ângulo={GrausStr(angulo)}).",
                    equipamento.Id.Value);
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao rotacionar equipamento {equipamento.Id.Value}: " +
                    $"{ex.Message}",
                    equipamento.Id.Value,
                    detalhes: ex.StackTrace);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ROTAÇÃO COM ORIENTAÇÃO PARA DENTRO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Ajusta rotação para que o equipamento fique de frente
        /// para o centro do ambiente (virado para dentro).
        /// </summary>
        public void AjustarRotacaoParaDentro(
            Document doc,
            FamilyInstance equipamento,
            Wall parede,
            XYZ centroAmbiente,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (equipamento == null) throw new ArgumentNullException(nameof(equipamento));
            if (parede == null) throw new ArgumentNullException(nameof(parede));
            if (centroAmbiente == null) throw new ArgumentNullException(nameof(centroAmbiente));
            if (log == null) throw new ArgumentNullException(nameof(log));

            try
            {
                var transform = equipamento.GetTotalTransform();
                var origin = transform.Origin;
                var forward = transform.BasisX.Normalize();

                // Vetor do equipamento para o centro (no plano XY)
                var paraCentro = new XYZ(
                    centroAmbiente.X - origin.X,
                    centroAmbiente.Y - origin.Y,
                    0).Normalize();

                // Normal da parede apontando para dentro
                var dirParede = ObterDirecaoParede(parede);
                if (dirParede == null)
                {
                    log.Medio(ETAPA, COMPONENTE,
                        $"Parede sem curva. Usando direção centro.",
                        equipamento.Id.Value);
                    dirParede = paraCentro;
                }

                // Normal perpendicular à parede
                var normalParede = new XYZ(-dirParede.Y, dirParede.X, 0).Normalize();

                // Garantir que a normal aponta para o centro
                if (normalParede.DotProduct(paraCentro) < 0)
                    normalParede = normalParede.Negate();

                // Ângulo entre forward e a normal (virar de frente)
                var angulo = CalcularAngulo(forward, normalParede);

                if (Math.Abs(angulo) < TOLERANCIA_ANGULAR)
                {
                    log.Leve(ETAPA, COMPONENTE,
                        $"Equipamento {equipamento.Id.Value} já orientado para dentro.",
                        equipamento.Id.Value);
                    return;
                }

                var eixo = Line.CreateBound(origin, origin + XYZ.BasisZ);

                bool transInterna = !doc.IsModifiable;

                if (transInterna)
                {
                    using var trans = new Transaction(doc, "Rotacionar Para Dentro");
                    trans.Start();
                    ElementTransformUtils.RotateElement(doc, equipamento.Id, eixo, angulo);
                    trans.Commit();
                }
                else
                {
                    ElementTransformUtils.RotateElement(doc, equipamento.Id, eixo, angulo);
                }

                log.Info(ETAPA, COMPONENTE,
                    $"Rotação ajustada: equipamento {equipamento.Id.Value} " +
                    $"orientado para dentro do ambiente " +
                    $"(ângulo={GrausStr(angulo)}).",
                    equipamento.Id.Value);
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao rotacionar para dentro: {ex.Message}",
                    equipamento.Id.Value,
                    detalhes: ex.StackTrace);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém vetor direcional da parede via LocationCurve.
        /// </summary>
        private static XYZ? ObterDirecaoParede(Wall parede)
        {
            if (parede.Location is not LocationCurve locationCurve)
                return null;

            var curve = locationCurve.Curve;
            var start = curve.GetEndPoint(0);
            var end = curve.GetEndPoint(1);

            var direcao = (end - start);
            if (direcao.GetLength() < 1e-9)
                return null;

            return direcao.Normalize();
        }

        /// <summary>
        /// Calcula ângulo com sinal entre dois vetores no plano XY.
        /// Positivo = anti-horário, Negativo = horário.
        /// </summary>
        private static double CalcularAngulo(XYZ de, XYZ para)
        {
            // Projetar no plano XY
            var deXY = new XYZ(de.X, de.Y, 0).Normalize();
            var paraXY = new XYZ(para.X, para.Y, 0).Normalize();

            // Dot product → ângulo absoluto
            var dot = deXY.DotProduct(paraXY);
            dot = Math.Max(-1.0, Math.Min(1.0, dot)); // Clamp [-1, 1]
            var angulo = Math.Acos(dot);

            // Cross product → sinal (Z positivo = anti-horário)
            var cross = deXY.CrossProduct(paraXY);
            if (cross.Z < 0)
                angulo = -angulo;

            return angulo;
        }

        /// <summary>
        /// Converte radianos para string em graus formatada.
        /// </summary>
        private static string GrausStr(double radianos)
        {
            var graus = radianos * 180.0 / Math.PI;
            return $"{graus:F1}°";
        }
    }
}
