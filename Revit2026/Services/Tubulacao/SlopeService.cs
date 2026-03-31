using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Tubulacao
{
    /// <summary>
    /// Serviço de aplicação de inclinação (slope) em tubulações de esgoto.
    /// Converte porcentagem para decimal e aplica via RBS_PIPE_SLOPE.
    ///
    /// Valores típicos NBR 8160:
    /// - Ø50mm: 2%
    /// - Ø75mm: 2%
    /// - Ø100mm: 1%
    /// - Ø150mm+: 0.5%
    /// </summary>
    public class SlopeService
    {
        private const string ETAPA = "05_Tubulacao";
        private const string COMPONENTE = "Slope";

        private const double SLOPE_MINIMO = 0.5;  // %
        private const double SLOPE_MAXIMO = 10.0;  // %

        // ══════════════════════════════════════════════════════════
        //  APLICAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica inclinação em uma tubulação.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="pipeId">ElementId do Pipe.</param>
        /// <param name="slopePercent">Inclinação em porcentagem (ex: 2.0 = 2%).</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>true se aplicado com sucesso.</returns>
        public bool AplicarInclinacao(
            Document doc,
            ElementId pipeId,
            double slopePercent,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar pipeId ─────────────────────────────
            if (pipeId == null || pipeId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao aplicar inclinação: PipeId inválido.");
                return false;
            }

            // ── 2. Obter elemento ─────────────────────────────
            var element = doc.GetElement(pipeId);
            if (element is not Pipe pipe)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao aplicar inclinação no pipe {pipeId.Value}: " +
                    $"elemento não é um Pipe.",
                    pipeId.Value);
                return false;
            }

            // ── 3. Validar faixa de slope ─────────────────────
            if (slopePercent < SLOPE_MINIMO)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Inclinação {slopePercent:F2}% abaixo do mínimo usual " +
                    $"({SLOPE_MINIMO}%). Pipe {pipeId.Value}. " +
                    $"Pode causar acúmulo de resíduos.",
                    pipeId.Value);
            }

            if (slopePercent > SLOPE_MAXIMO)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Inclinação {slopePercent:F2}% acima do máximo usual " +
                    $"({SLOPE_MAXIMO}%). Pipe {pipeId.Value}. " +
                    $"Valor atípico — verifique se está correto.",
                    pipeId.Value);
            }

            // ── 4. Converter % → decimal ─────────────────────
            double slopeDecimal = slopePercent / 100.0;

            // ── 5. Aplicar parâmetro ──────────────────────────
            try
            {
                var paramSlope = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_SLOPE);

                if (paramSlope == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao aplicar inclinação no pipe {pipeId.Value}: " +
                        $"parâmetro RBS_PIPE_SLOPE não encontrado.",
                        pipeId.Value);
                    return false;
                }

                if (paramSlope.IsReadOnly)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao aplicar inclinação no pipe {pipeId.Value}: " +
                        $"parâmetro RBS_PIPE_SLOPE é somente leitura.",
                        pipeId.Value);
                    return false;
                }

                paramSlope.Set(slopeDecimal);

                log.Info(ETAPA, COMPONENTE,
                    $"Inclinação aplicada: {slopePercent:F2}% no pipe {pipeId.Value}",
                    pipeId.Value);

                return true;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao aplicar inclinação no pipe {pipeId.Value}: " +
                    $"{ex.Message}",
                    pipeId.Value,
                    detalhes: ex.StackTrace);
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  APLICAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado da aplicação em lote.
        /// </summary>
        public class ResultadoSlopeLote
        {
            public int Total { get; set; }
            public int Sucesso { get; set; }
            public int Falhas { get; set; }

            public override string ToString() =>
                $"{Sucesso}/{Total} aplicados, {Falhas} falhas";
        }

        /// <summary>
        /// Aplica inclinação em múltiplos pipes com mesma porcentagem.
        /// </summary>
        public ResultadoSlopeLote AplicarEmLote(
            Document doc,
            List<ElementId> pipeIds,
            double slopePercent,
            ILogService log)
        {
            var resultado = new ResultadoSlopeLote { Total = pipeIds.Count };

            log.Info(ETAPA, COMPONENTE,
                $"Aplicando inclinação {slopePercent:F2}% em {pipeIds.Count} pipes...");

            foreach (var pipeId in pipeIds)
            {
                if (AplicarInclinacao(doc, pipeId, slopePercent, log))
                    resultado.Sucesso++;
                else
                    resultado.Falhas++;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Inclinação em lote concluída: {resultado}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  SLOPE POR DIÂMETRO (NBR 8160)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica inclinação automática baseada no diâmetro da tubulação.
        /// Segue recomendações da NBR 8160.
        /// </summary>
        public bool AplicarInclinacaoPorDiametro(
            Document doc,
            ElementId pipeId,
            ILogService log)
        {
            if (doc == null || log == null)
                return false;

            if (pipeId == null || pipeId == ElementId.InvalidElementId)
                return false;

            var element = doc.GetElement(pipeId);
            if (element is not Pipe pipe)
                return false;

            // Obter diâmetro em mm
            double diametroMm = 0;
            try
            {
                var paramDiam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (paramDiam != null)
                {
                    diametroMm = UnitUtils.ConvertFromInternalUnits(
                        paramDiam.AsDouble(), UnitTypeId.Millimeters);
                }
            }
            catch { /* fallback abaixo */ }

            if (diametroMm <= 0)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Diâmetro não detectado para pipe {pipeId.Value}. " +
                    $"Usando slope padrão 2%.",
                    pipeId.Value);
                return AplicarInclinacao(doc, pipeId, 2.0, log);
            }

            // Slope por diâmetro (NBR 8160)
            double slopePercent = ObterSlopePorDiametro(diametroMm);

            log.Info(ETAPA, COMPONENTE,
                $"Slope automático: Ø{diametroMm:F0} mm → {slopePercent:F1}% " +
                $"(NBR 8160). Pipe {pipeId.Value}.",
                pipeId.Value);

            return AplicarInclinacao(doc, pipeId, slopePercent, log);
        }

        /// <summary>
        /// Retorna inclinação recomendada pela NBR 8160 baseada no diâmetro.
        /// </summary>
        public static double ObterSlopePorDiametro(double diametroMm)
        {
            return diametroMm switch
            {
                <= 40 => 3.0,   // Ø40 mm: 3%
                <= 50 => 2.0,   // Ø50 mm: 2%
                <= 75 => 2.0,   // Ø75 mm: 2%
                <= 100 => 1.0,  // Ø100 mm: 1%
                <= 150 => 0.65, // Ø150 mm: 0.65%
                _ => 0.5        // Ø200+ mm: 0.5%
            };
        }
    }
}
