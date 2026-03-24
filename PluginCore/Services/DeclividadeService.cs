using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado da verificação de declividade.
    /// </summary>
    public class ResultadoDeclividade
    {
        /// <summary>ID do trecho.</summary>
        public string TrechoId { get; set; } = string.Empty;

        /// <summary>Diâmetro nominal (mm).</summary>
        public int DiametroNominal { get; set; }

        /// <summary>Declividade mínima exigida (%).</summary>
        public double DeclividadeMinima { get; set; }

        /// <summary>Declividade aplicada (%).</summary>
        public double DeclividadeAplicada { get; set; }

        /// <summary>Declividade recomendada (%).</summary>
        public double DeclividadeRecomendada { get; set; }

        /// <summary>Se atende à norma.</summary>
        public bool Aprovado { get; set; }

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            return $"{status} '{TrechoId}': DN{DiametroNominal} | " +
                   $"Min={DeclividadeMinima}% | Aplicada={DeclividadeAplicada}%";
        }
    }

    /// <summary>
    /// Serviço de cálculo e verificação de declividade para redes de esgoto.
    /// Conforme NBR 8160:1999.
    /// </summary>
    public static class DeclividadeService
    {
        // ══════════════════════════════════════════════════════════
        //  TABELA DE DECLIVIDADES NBR 8160
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Declividade mínima por diâmetro nominal (%).
        /// NBR 8160 — § 5.1.1.4.
        /// </summary>
        private static readonly (int dnMax, double declividade)[] _tabelaDeclividade =
        {
            (40,  3.0),  // DN ≤ 40  → 3% (ramais de descarga curtos)
            (50,  2.5),  // DN 50    → 2.5%
            (75,  2.0),  // DN 75    → 2%
            (100, 1.0),  // DN 100   → 1%
            (150, 0.5),  // DN ≥ 150 → 0.5%
        };

        /// <summary>Declividade máxima recomendada (%).</summary>
        private const double DECLIVIDADE_MAXIMA = 5.0;

        // ══════════════════════════════════════════════════════════
        //  OBTER DECLIVIDADE MÍNIMA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna declividade mínima (%) para um diâmetro nominal.
        /// </summary>
        public static double ObterDeclividadeMinima(int diametroMm)
        {
            if (diametroMm <= 0)
                return 2.0; // Default seguro

            foreach (var (dnMax, dec) in _tabelaDeclividade)
            {
                if (diametroMm <= dnMax)
                    return dec;
            }

            // DN > 150 → 0.5%
            return 0.5;
        }

        /// <summary>
        /// Retorna declividade recomendada (%) — valor ideal para bom escoamento.
        /// Geralmente 1.5× a mínima, limitada pela máxima.
        /// </summary>
        public static double ObterDeclividadeRecomendada(int diametroMm)
        {
            var min = ObterDeclividadeMinima(diametroMm);
            var recomendada = min * 1.5;
            return Math.Min(recomendada, DECLIVIDADE_MAXIMA);
        }

        /// <summary>
        /// Retorna declividade em fração (m/m) — para uso em cálculos.
        /// </summary>
        public static double ObterDeclividadeFracao(int diametroMm)
        {
            return ObterDeclividadeMinima(diametroMm) / 100.0;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se a declividade aplicada atende à norma.
        /// </summary>
        public static bool VerificarDeclividade(int diametroMm, double declividadeAplicada)
        {
            return declividadeAplicada >= ObterDeclividadeMinima(diametroMm);
        }

        /// <summary>
        /// Verifica declividade de um trecho e retorna resultado detalhado.
        /// </summary>
        public static ResultadoDeclividade VerificarTrecho(TrechoTubulacao trecho)
        {
            if (trecho == null)
                return new ResultadoDeclividade();

            var min = ObterDeclividadeMinima(trecho.DiametroNominal);
            var rec = ObterDeclividadeRecomendada(trecho.DiametroNominal);

            return new ResultadoDeclividade
            {
                TrechoId = trecho.Id,
                DiametroNominal = trecho.DiametroNominal,
                DeclividadeMinima = min,
                DeclividadeAplicada = trecho.Declividade,
                DeclividadeRecomendada = rec,
                Aprovado = trecho.Declividade >= min,
            };
        }

        /// <summary>
        /// Verifica declividade de múltiplos trechos e gera logs.
        /// </summary>
        public static List<ResultadoDeclividade> VerificarTodos(
            IEnumerable<TrechoTubulacao> trechos, ILogService? log = null)
        {
            if (trechos == null)
                return new List<ResultadoDeclividade>();

            var lista = trechos.Where(t => t != null).ToList();
            var resultados = lista.Select(VerificarTrecho).ToList();

            var aprovados = resultados.Count(r => r.Aprovado);
            var reprovados = resultados.Count - aprovados;

            foreach (var r in resultados)
            {
                if (!r.Aprovado)
                {
                    log?.Critico("07_Esgoto", "Declividade",
                        $"Trecho '{r.TrechoId}': declividade {r.DeclividadeAplicada}% " +
                        $"abaixo do mínimo {r.DeclividadeMinima}% (DN{r.DiametroNominal}). " +
                        $"Ação: Ajustar para ≥ {r.DeclividadeMinima}%.");
                }
                else if (r.DeclividadeAplicada > DECLIVIDADE_MAXIMA)
                {
                    log?.Medio("07_Esgoto", "Declividade",
                        $"Trecho '{r.TrechoId}': declividade {r.DeclividadeAplicada}% " +
                        $"excede recomendação de {DECLIVIDADE_MAXIMA}%. " +
                        $"Risco de separação líquido-sólido.");
                }
            }

            log?.Info("07_Esgoto", "Declividade",
                $"Verificação de declividade: {aprovados} OK, {reprovados} insuficientes.");

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  APLICAR DECLIVIDADE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica declividade mínima a um trecho (se não estiver definida).
        /// </summary>
        public static void AplicarMinima(TrechoTubulacao trecho)
        {
            if (trecho == null)
                return;

            var min = ObterDeclividadeMinima(trecho.DiametroNominal);

            if (trecho.Declividade < min)
                trecho.Declividade = min;
        }

        /// <summary>
        /// Aplica declividade mínima a todos os trechos de esgoto.
        /// </summary>
        public static void AplicarMinimaTodos(IEnumerable<TrechoTubulacao> trechos)
        {
            if (trechos == null)
                return;

            foreach (var trecho in trechos)
            {
                if (trecho?.Sistema == HydraulicSystem.Sewer)
                    AplicarMinima(trecho);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CÁLCULOS AUXILIARES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula desnível (m) dado comprimento e declividade.
        /// </summary>
        public static double CalcularDesnivel(double comprimentoM, double declividadePercent)
        {
            return comprimentoM * (declividadePercent / 100.0);
        }

        /// <summary>
        /// Calcula declividade (%) dado comprimento e desnível.
        /// </summary>
        public static double CalcularDeclividade(double comprimentoM, double desnivelM)
        {
            if (comprimentoM <= 0)
                return 0.0;

            return (desnivelM / comprimentoM) * 100.0;
        }

        /// <summary>
        /// Calcula comprimento máximo possível dado desnível e declividade.
        /// </summary>
        public static double CalcularComprimentoMaximo(double desnivelM, double declividadePercent)
        {
            if (declividadePercent <= 0)
                return 0.0;

            return desnivelM / (declividadePercent / 100.0);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual.</summary>
        public static string GerarResumo(List<ResultadoDeclividade> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhum trecho verificado.";

            var aprovados = resultados.Count(r => r.Aprovado);

            var lines = new List<string>
            {
                "══ Verificação de Declividade ══",
                $"  Trechos:     {resultados.Count}",
                $"  Aprovados:   {aprovados}",
                $"  Reprovados:  {resultados.Count - aprovados}",
                "────────────────────────────────",
            };

            // Tabela rápida
            lines.Add("  DN     | Mínima | Recomendada");
            lines.Add("  ───────┼────────┼───────────");
            lines.Add("  ≤ 40   | 3.0%   | 4.5%");
            lines.Add("  50     | 2.5%   | 3.75%");
            lines.Add("  75     | 2.0%   | 3.0%");
            lines.Add("  100    | 1.0%   | 1.5%");
            lines.Add("  ≥ 150  | 0.5%   | 0.75%");
            lines.Add("────────────────────────────────");

            foreach (var r in resultados.Where(r => !r.Aprovado))
            {
                lines.Add($"  {r}");
            }

            lines.Add("════════════════════════════════");

            return string.Join("\n", lines);
        }

        // Exemplos:
        // DeclividadeService.ObterDeclividadeMinima(50)        → 2.5%
        // DeclividadeService.ObterDeclividadeMinima(100)       → 1.0%
        // DeclividadeService.ObterDeclividadeMinima(150)       → 0.5%
        // DeclividadeService.ObterDeclividadeRecomendada(100)  → 1.5%
        // DeclividadeService.ObterDeclividadeFracao(100)       → 0.01
        //
        // DeclividadeService.CalcularDesnivel(10.0, 1.0)       → 0.10 m
        // DeclividadeService.CalcularDeclividade(10.0, 0.15)   → 1.5%
        //
        // DeclividadeService.AplicarMinimaTodos(trechosEsgoto);
        // var resultados = DeclividadeService.VerificarTodos(trechos, logService);
    }
}
