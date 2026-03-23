using PluginCore.Data;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Serviço de dimensionamento hidráulico conforme NBR 5626 (AF) e NBR 8160 (ES).
    /// Implementa cálculo de vazão, diâmetro, velocidade e perda de carga.
    /// </summary>
    public class DimensionamentoService : IDimensionamentoService
    {
        // ══════════════════════════════════════════════════════════
        //  CONSTANTES
        // ══════════════════════════════════════════════════════════

        /// <summary>Coeficiente NBR 5626: Q = C × √(ΣP).</summary>
        private const double COEF_VAZAO = 0.3;

        /// <summary>Velocidade máxima permitida (m/s) — NBR 5626.</summary>
        private const double VELOCIDADE_MAXIMA = 3.0;

        /// <summary>Rugosidade PVC (mm) — Hazen-Williams C=140.</summary>
        private const double COEF_HAZEN_WILLIAMS_PVC = 140.0;

        /// <summary>Diâmetros comerciais disponíveis (mm).</summary>
        private static readonly int[] DIAMETROS_COMERCIAIS = { 20, 25, 32, 40, 50, 60, 75, 85, 100, 150 };

        /// <summary>Diâmetros internos por diâmetro nominal — PVC soldável (mm).</summary>
        private static readonly Dictionary<int, double> DIAMETRO_INTERNO = new()
        {
            [20] = 17.0,
            [25] = 21.6,
            [32] = 27.8,
            [40] = 35.2,
            [50] = 44.0,
            [60] = 53.0,
            [75] = 66.6,
            [85] = 75.6,
            [100] = 97.8,
            [150] = 144.0,
        };

        // ══════════════════════════════════════════════════════════
        //  IDimensionamentoService — IMPLEMENTAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona um sistema completo (todos os trechos).
        /// </summary>
        public ResultadoDimensionamento Dimensionar(SistemaMEP sistema)
        {
            if (sistema == null)
                return new ResultadoDimensionamento { Aprovado = false };

            var resultado = new ResultadoDimensionamento
            {
                ReferenciaId = sistema.Id,
                Sistema = sistema.Sistema,
                NormaUtilizada = sistema.Sistema == HydraulicSystem.ColdWater
                    ? "NBR 5626:2020"
                    : "NBR 8160:1999",
            };

            double perdaCargaTotal = 0;
            double vazaoMaxima = 0;

            foreach (var trecho in sistema.Trechos)
            {
                // Calcular vazão
                trecho.Vazao = CalcularVazaoProvavel(trecho.SomaPesos);

                // Determinar diâmetro
                trecho.DiametroNominal = DeterminarDiametro(trecho.Vazao, sistema.Sistema);
                trecho.DiametroInterno = GetDiametroInterno(trecho.DiametroNominal);

                // Calcular velocidade
                trecho.Velocidade = CalcularVelocidade(trecho.Vazao, trecho.DiametroInterno);

                // Calcular perda de carga
                trecho.PerdaCargaUnitaria = CalcularPerdaCargaUnitaria(trecho);
                trecho.PerdaCargaTotal = CalcularPerdaCarga(trecho);

                perdaCargaTotal += trecho.PerdaCargaTotal;

                if (trecho.Vazao > vazaoMaxima)
                    vazaoMaxima = trecho.Vazao;

                // Alertas
                if (trecho.Velocidade > VELOCIDADE_MAXIMA)
                {
                    resultado.Alertas.Add(
                        $"Trecho {trecho.Id}: velocidade {trecho.Velocidade:F2} m/s " +
                        $"excede o limite de {VELOCIDADE_MAXIMA} m/s.");
                }
            }

            resultado.VazaoProjeto = vazaoMaxima;
            resultado.DiametroRecomendado = sistema.Trechos.Count > 0
                ? sistema.Trechos.Max(t => t.DiametroNominal)
                : 0;
            resultado.Velocidade = sistema.Trechos.Count > 0
                ? sistema.Trechos.Max(t => t.Velocidade)
                : 0;
            resultado.PerdaCargaTotal = perdaCargaTotal;
            resultado.Aprovado = resultado.VelocidadeOk &&
                                  resultado.Alertas.Count == 0;

            return resultado;
        }

        /// <summary>
        /// Calcula vazão provável pelo método dos pesos.
        /// Q = 0.3 × √(ΣP)
        /// </summary>
        public double CalcularVazaoProvavel(double somaPesos)
        {
            if (somaPesos <= 0)
                return 0.0;

            return COEF_VAZAO * Math.Sqrt(somaPesos);
        }

        /// <summary>
        /// Determina o diâmetro comercial adequado para a vazão.
        /// Critério: velocidade ≤ 3.0 m/s.
        /// </summary>
        public int DeterminarDiametro(double vazao, HydraulicSystem sistema)
        {
            if (vazao <= 0)
                return DiametroMinimo(sistema);

            foreach (var dn in DIAMETROS_COMERCIAIS)
            {
                var di = GetDiametroInterno(dn);
                var velocidade = CalcularVelocidade(vazao, di);

                if (velocidade <= VELOCIDADE_MAXIMA)
                    return dn;
            }

            // Se nenhum atendeu, retorna o maior
            return DIAMETROS_COMERCIAIS[^1];
        }

        /// <summary>
        /// Calcula perda de carga total em um trecho.
        /// Método de Fair-Whipple-Hsiao para PVC.
        /// J = (10.643 × Q^1.85) / (C^1.85 × D^4.87)
        /// </summary>
        public double CalcularPerdaCarga(TrechoTubulacao trecho)
        {
            if (trecho == null || trecho.Vazao <= 0 || trecho.DiametroInterno <= 0)
                return 0.0;

            var j = CalcularPerdaCargaUnitaria(trecho);
            return j * trecho.ComprimentoTotal;
        }

        /// <summary>
        /// Calcula velocidade no trecho.
        /// V = Q / A = Q / (π/4 × D²)
        /// </summary>
        public double CalcularVelocidade(double vazao, double diametroInterno)
        {
            if (vazao <= 0 || diametroInterno <= 0)
                return 0.0;

            // Converter mm → m
            var d = diametroInterno / 1000.0;
            var area = Math.PI / 4.0 * d * d;

            // vazao em L/s → m³/s
            var vazaoM3s = vazao / 1000.0;

            return vazaoM3s / area;
        }

        /// <summary>
        /// Verifica pressão disponível no ponto mais desfavorável.
        /// </summary>
        public double VerificarPressao(SistemaMEP sistema, double pressaoAlimentacao)
        {
            if (sistema == null)
                return 0.0;

            var perdaTotal = sistema.Trechos.Sum(t => t.PerdaCargaTotal);
            return pressaoAlimentacao - perdaTotal;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO POR TRECHO (estático)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona um trecho individual e preenche suas propriedades.
        /// </summary>
        public static TrechoTubulacao DimensionarTrecho(TrechoTubulacao trecho)
        {
            if (trecho == null)
                return trecho!;

            var service = new DimensionamentoService();

            trecho.Vazao = service.CalcularVazaoProvavel(trecho.SomaPesos);
            trecho.DiametroNominal = service.DeterminarDiametro(trecho.Vazao, trecho.Sistema);
            trecho.DiametroInterno = GetDiametroInterno(trecho.DiametroNominal);
            trecho.Velocidade = service.CalcularVelocidade(trecho.Vazao, trecho.DiametroInterno);
            trecho.PerdaCargaUnitaria = CalcularPerdaCargaUnitaria(trecho);
            trecho.PerdaCargaTotal = trecho.PerdaCargaUnitaria * trecho.ComprimentoTotal;

            return trecho;
        }

        /// <summary>
        /// Retorna diâmetros comerciais disponíveis.
        /// </summary>
        public static int[] GetDiametrosComerciais() => DIAMETROS_COMERCIAIS;

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Retorna diâmetro interno por DN.</summary>
        private static double GetDiametroInterno(int diametroNominal)
        {
            return DIAMETRO_INTERNO.TryGetValue(diametroNominal, out var di) ? di : diametroNominal * 0.85;
        }

        /// <summary>Diâmetro mínimo por sistema.</summary>
        private static int DiametroMinimo(HydraulicSystem sistema)
        {
            return sistema switch
            {
                HydraulicSystem.ColdWater => 20,
                HydraulicSystem.HotWater => 22,
                HydraulicSystem.Sewer => 40,
                HydraulicSystem.Ventilation => 50,
                HydraulicSystem.Rainwater => 50,
                _ => 20,
            };
        }

        /// <summary>
        /// Perda de carga unitária — Fair-Whipple-Hsiao.
        /// J = (10.643 × Q^1.85) / (C^1.85 × D^4.87)
        /// </summary>
        private static double CalcularPerdaCargaUnitaria(TrechoTubulacao trecho)
        {
            if (trecho.Vazao <= 0 || trecho.DiametroInterno <= 0)
                return 0.0;

            var q = trecho.Vazao / 1000.0;        // L/s → m³/s
            var d = trecho.DiametroInterno / 1000.0; // mm → m
            var c = COEF_HAZEN_WILLIAMS_PVC;

            return (10.643 * Math.Pow(q, 1.85)) /
                   (Math.Pow(c, 1.85) * Math.Pow(d, 4.87));
        }

        // Exemplos:
        // var service = new DimensionamentoService();
        //
        // service.CalcularVazaoProvavel(1.0)    → 0.300 L/s
        // service.DeterminarDiametro(0.3, ColdWater) → 20 mm
        // service.CalcularVelocidade(0.3, 17.0) → 1.32 m/s
        //
        // var trecho = new TrechoTubulacao { SomaPesos = 2.4, Comprimento = 5.0, Sistema = ColdWater };
        // DimensionamentoService.DimensionarTrecho(trecho);
        // trecho.Vazao           → 0.465 L/s
        // trecho.DiametroNominal → 20 mm
        // trecho.Velocidade      → 2.05 m/s
    }
}
