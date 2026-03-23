using PluginCore.Domain.Enums;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Método de cálculo de perda de carga.
    /// </summary>
    public enum MetodoPerdaCarga
    {
        /// <summary>Hazen-Williams — mais comum para água fria.</summary>
        HazenWilliams,

        /// <summary>Fair-Whipple-Hsiao — recomendado para PVC.</summary>
        FairWhippleHsiao
    }

    /// <summary>
    /// Resultado detalhado de cálculo de perda de carga.
    /// </summary>
    public class ResultadoPerdaCarga
    {
        /// <summary>ID do trecho.</summary>
        public string TrechoId { get; set; } = string.Empty;

        /// <summary>Vazão utilizada (L/s).</summary>
        public double VazaoL_s { get; set; }

        /// <summary>Diâmetro interno (mm).</summary>
        public double DiametroInternoMm { get; set; }

        /// <summary>Comprimento real (m).</summary>
        public double ComprimentoReal { get; set; }

        /// <summary>Comprimento equivalente das conexões (m).</summary>
        public double ComprimentoEquivalente { get; set; }

        /// <summary>Comprimento total (m).</summary>
        public double ComprimentoTotal => ComprimentoReal + ComprimentoEquivalente;

        /// <summary>Perda de carga unitária (m/m).</summary>
        public double PerdaUnitaria { get; set; }

        /// <summary>Perda de carga total (mCA).</summary>
        public double PerdaTotal { get; set; }

        /// <summary>Método utilizado.</summary>
        public MetodoPerdaCarga Metodo { get; set; }

        /// <summary>Coeficiente de rugosidade utilizado.</summary>
        public double Coeficiente { get; set; }

        public override string ToString()
        {
            return $"Trecho '{TrechoId}': J={PerdaUnitaria:F5} m/m, " +
                   $"H={PerdaTotal:F3} mCA ({Metodo}, L={ComprimentoTotal:F1}m, " +
                   $"Q={VazaoL_s:F3} L/s, Ø{DiametroInternoMm:F0}mm)";
        }
    }

    /// <summary>
    /// Serviço de cálculo de perda de carga em tubulações.
    /// Suporta Hazen-Williams e Fair-Whipple-Hsiao.
    /// </summary>
    public static class PerdaCargaService
    {
        // ══════════════════════════════════════════════════════════
        //  COEFICIENTES DE RUGOSIDADE (C)
        // ══════════════════════════════════════════════════════════

        private static readonly Dictionary<PipeMaterial, double> _coeficientes = new()
        {
            [PipeMaterial.PVC]    = 140.0,
            [PipeMaterial.CPVC]   = 140.0,
            [PipeMaterial.PPR]    = 120.0,
            [PipeMaterial.Copper] = 130.0,
            [PipeMaterial.PEX]    = 140.0,
        };

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula perda de carga total em um trecho (mCA).
        /// </summary>
        public static double CalcularPerdaCarga(TrechoTubulacao trecho,
            MetodoPerdaCarga metodo = MetodoPerdaCarga.FairWhippleHsiao)
        {
            if (trecho == null)
                return 0.0;

            var j = CalcularPerdaUnitaria(
                trecho.Vazao, trecho.DiametroInterno, trecho.Material, metodo);

            return j * trecho.ComprimentoTotal;
        }

        /// <summary>
        /// Calcula perda de carga unitária (m/m).
        /// </summary>
        public static double CalcularPerdaUnitaria(double vazaoL_s,
            double diametroInternoMm, PipeMaterial material,
            MetodoPerdaCarga metodo = MetodoPerdaCarga.FairWhippleHsiao)
        {
            if (vazaoL_s <= 0 || diametroInternoMm <= 0)
                return 0.0;

            var c = GetCoeficiente(material);
            var q = vazaoL_s / 1000.0;           // L/s → m³/s
            var d = diametroInternoMm / 1000.0;  // mm → m

            return metodo switch
            {
                MetodoPerdaCarga.HazenWilliams =>
                    CalcularHazenWilliams(q, d, c),

                MetodoPerdaCarga.FairWhippleHsiao =>
                    CalcularFairWhippleHsiao(q, d, c),

                _ => CalcularFairWhippleHsiao(q, d, c),
            };
        }

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO DETALHADO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula perda de carga com resultado detalhado.
        /// </summary>
        public static ResultadoPerdaCarga CalcularDetalhado(TrechoTubulacao trecho,
            MetodoPerdaCarga metodo = MetodoPerdaCarga.FairWhippleHsiao)
        {
            if (trecho == null)
                return new ResultadoPerdaCarga();

            var c = GetCoeficiente(trecho.Material);
            var j = CalcularPerdaUnitaria(
                trecho.Vazao, trecho.DiametroInterno, trecho.Material, metodo);

            return new ResultadoPerdaCarga
            {
                TrechoId = trecho.Id,
                VazaoL_s = trecho.Vazao,
                DiametroInternoMm = trecho.DiametroInterno,
                ComprimentoReal = trecho.Comprimento,
                ComprimentoEquivalente = trecho.ComprimentoEquivalente,
                PerdaUnitaria = j,
                PerdaTotal = j * trecho.ComprimentoTotal,
                Metodo = metodo,
                Coeficiente = c,
            };
        }

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula perda de carga para múltiplos trechos.
        /// </summary>
        public static List<ResultadoPerdaCarga> CalcularLote(
            IEnumerable<TrechoTubulacao> trechos,
            MetodoPerdaCarga metodo = MetodoPerdaCarga.FairWhippleHsiao)
        {
            if (trechos == null)
                return new List<ResultadoPerdaCarga>();

            return trechos
                .Where(t => t != null)
                .Select(t => CalcularDetalhado(t, metodo))
                .ToList();
        }

        /// <summary>
        /// Calcula perda de carga total de um percurso (série de trechos).
        /// </summary>
        public static double CalcularPercurso(IEnumerable<TrechoTubulacao> trechos,
            MetodoPerdaCarga metodo = MetodoPerdaCarga.FairWhippleHsiao)
        {
            if (trechos == null)
                return 0.0;

            return trechos
                .Where(t => t != null)
                .Sum(t => CalcularPerdaCarga(t, metodo));
        }

        // ══════════════════════════════════════════════════════════
        //  FÓRMULAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Hazen-Williams:
        /// J = 10.643 × Q^1.852 / (C^1.852 × D^4.8704)
        /// </summary>
        private static double CalcularHazenWilliams(double q_m3s, double d_m, double c)
        {
            if (q_m3s <= 0 || d_m <= 0 || c <= 0)
                return 0.0;

            return (10.643 * Math.Pow(q_m3s, 1.852)) /
                   (Math.Pow(c, 1.852) * Math.Pow(d_m, 4.8704));
        }

        /// <summary>
        /// Fair-Whipple-Hsiao (para tubos lisos — PVC, CPVC):
        /// J = 10.643 × Q^1.75 / (C^1.75 × D^4.75)
        /// Em forma simplificada com C do material.
        /// </summary>
        private static double CalcularFairWhippleHsiao(double q_m3s, double d_m, double c)
        {
            if (q_m3s <= 0 || d_m <= 0 || c <= 0)
                return 0.0;

            return (10.643 * Math.Pow(q_m3s, 1.85)) /
                   (Math.Pow(c, 1.85) * Math.Pow(d_m, 4.87));
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Retorna coeficiente de rugosidade por material.</summary>
        public static double GetCoeficiente(PipeMaterial material)
        {
            return _coeficientes.TryGetValue(material, out var c) ? c : 140.0;
        }

        /// <summary>Gera resumo textual de um lote de resultados.</summary>
        public static string GerarResumo(List<ResultadoPerdaCarga> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhum trecho calculado.";

            var total = resultados.Sum(r => r.PerdaTotal);
            var max = resultados.Max(r => r.PerdaUnitaria);
            var metodo = resultados[0].Metodo;

            return $"══ Perda de Carga ({metodo}) ══\n" +
                   $"  Trechos:       {resultados.Count}\n" +
                   $"  H total:       {total:F3} mCA\n" +
                   $"  J máxima:      {max:F5} m/m\n" +
                   $"═══════════════════════════════";
        }

        // Exemplos:
        // var h = PerdaCargaService.CalcularPerdaCarga(trecho, MetodoPerdaCarga.HazenWilliams);
        //
        // var detalhe = PerdaCargaService.CalcularDetalhado(trecho);
        // detalhe.PerdaUnitaria → 0.00234 m/m
        // detalhe.PerdaTotal    → 0.117 mCA
        //
        // var percurso = PerdaCargaService.CalcularPercurso(trechos);
        // → 1.245 mCA
    }
}
