using PluginCore.Data;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado do dimensionamento de um subcoletor/coletor de esgoto.
    /// </summary>
    public class ResultadoSubcoletor
    {
        /// <summary>ID do subcoletor.</summary>
        public string SubcoletorId { get; set; } = string.Empty;

        /// <summary>UHC total acumulado.</summary>
        public double UHCTotal { get; set; }

        /// <summary>Diâmetro nominal dimensionado (mm).</summary>
        public int DiametroNominal { get; set; }

        /// <summary>Declividade aplicada (%).</summary>
        public double Declividade { get; set; }

        /// <summary>Vazão estimada (L/s).</summary>
        public double VazaoL_s { get; set; }

        /// <summary>Velocidade estimada (m/s).</summary>
        public double Velocidade { get; set; }

        /// <summary>Comprimento do subcoletor (m).</summary>
        public double Comprimento { get; set; }

        /// <summary>Prumadas conectadas.</summary>
        public int TotalPrumadas { get; set; }

        /// <summary>Aprovado.</summary>
        public bool Aprovado { get; set; }

        /// <summary>Alertas.</summary>
        public List<string> Alertas { get; set; } = new();

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            return $"{status} Subcoletor '{SubcoletorId}': DN{DiametroNominal} | " +
                   $"UHC={UHCTotal:F0} | Dec={Declividade}% | " +
                   $"V={Velocidade:F2} m/s | {TotalPrumadas} prumadas";
        }
    }

    /// <summary>
    /// Serviço de dimensionamento de subcoletores e coletores de esgoto.
    /// Conforme NBR 8160:1999 — Tabela 6.
    /// </summary>
    public class SubcoletoresService
    {
        private readonly ILogService? _log;

        private const string ETAPA = "07_Esgoto";
        private const string COMPONENTE = "Subcoletores";

        // ══════════════════════════════════════════════════════════
        //  CONSTANTES NBR 8160
        // ══════════════════════════════════════════════════════════

        /// <summary>Velocidade mínima — autolimpeza (m/s).</summary>
        private const double VELOCIDADE_MINIMA = 0.6;

        /// <summary>Velocidade máxima (m/s).</summary>
        private const double VELOCIDADE_MAXIMA = 4.0;

        /// <summary>Manning PVC.</summary>
        private const double MANNING_N = 0.010;

        /// <summary>
        /// Tabela NBR 8160 — Subcoletores e coletores (Tab. 6).
        /// (DN, maxUHC decl 0.5%, maxUHC decl 1%, maxUHC decl 2%, maxUHC decl 4%)
        /// </summary>
        private static readonly (int dn, int uhc_05, int uhc_10, int uhc_20, int uhc_40)[] _tabelaSubcoletor =
        {
            (100,  180,  216,  250,  300),
            (150,  700,  840,  1000, 1400),
            (200,  1400, 1680, 2000, 2800),
            (250,  2500, 3000, 3600, 5000),
            (300,  3900, 4600, 5600, 7800),
        };

        /// <summary>Declividades mínimas por DN (%).</summary>
        private static readonly Dictionary<int, double> _declividades = new()
        {
            [100] = 1.0,
            [150] = 0.5,
            [200] = 0.5,
            [250] = 0.5,
            [300] = 0.5,
        };

        /// <summary>Diâmetros comerciais de subcoletores (mm).</summary>
        private static readonly int[] DIAMETROS = { 100, 150, 200, 250, 300 };

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public SubcoletoresService() { }

        public SubcoletoresService(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona subcoletor pelo UHC total acumulado.
        /// </summary>
        public ResultadoSubcoletor DimensionarSubcoletor(double uhcTotal,
            string subcoletorId = "", double comprimento = 0)
        {
            var resultado = new ResultadoSubcoletor
            {
                SubcoletorId = subcoletorId,
                UHCTotal = uhcTotal,
                Comprimento = comprimento,
            };

            if (uhcTotal <= 0)
            {
                resultado.DiametroNominal = DIAMETROS[0];
                resultado.Declividade = 1.0;
                resultado.Aprovado = true;
                return resultado;
            }

            // ── 1. Determinar DN ──────────────────────────────
            var dn = DeterminarDN(uhcTotal);
            resultado.DiametroNominal = dn;

            // ── 2. Declividade ────────────────────────────────
            resultado.Declividade = GetDeclividade(dn);

            // ── 3. Vazão (Manning) ────────────────────────────
            var vazao = EstimarVazaoManning(dn, resultado.Declividade);
            resultado.VazaoL_s = vazao;

            // ── 4. Velocidade ─────────────────────────────────
            var velocidade = CalcularVelocidade(vazao, dn);
            resultado.Velocidade = velocidade;

            // ── 5. Validação ──────────────────────────────────
            resultado.Aprovado = true;

            if (velocidade > VELOCIDADE_MAXIMA)
            {
                resultado.Aprovado = false;
                resultado.Alertas.Add(
                    $"Velocidade excedida: {velocidade:F2} > {VELOCIDADE_MAXIMA} m/s.");
                _log?.Critico(ETAPA, COMPONENTE,
                    $"Subcoletor '{subcoletorId}': V={velocidade:F2} m/s, DN{dn}, UHC={uhcTotal:F0}.");
            }
            else if (velocidade > 0 && velocidade < VELOCIDADE_MINIMA)
            {
                resultado.Alertas.Add(
                    $"Velocidade baixa: {velocidade:F2} < {VELOCIDADE_MINIMA} m/s.");
                _log?.Leve(ETAPA, COMPONENTE,
                    $"Subcoletor '{subcoletorId}': V={velocidade:F2} m/s abaixo da autolimpeza.");
            }

            _log?.Info(ETAPA, COMPONENTE,
                $"Subcoletor '{subcoletorId}': DN{dn}, UHC={uhcTotal:F0}, " +
                $"Dec={resultado.Declividade}%, V={velocidade:F2} m/s. " +
                $"{(resultado.Aprovado ? "✅" : "❌")}");

            return resultado;
        }

        /// <summary>
        /// Dimensiona subcoletor a partir de prumadas de esgoto.
        /// </summary>
        public ResultadoSubcoletor DimensionarPorPrumadas(
            IEnumerable<Prumada> prumadas, string subcoletorId = "",
            double comprimento = 0)
        {
            var lista = prumadas?.Where(p => p != null).ToList() ?? new();
            var uhcTotal = lista.Sum(p => p.SomaPesos);

            var resultado = DimensionarSubcoletor(uhcTotal, subcoletorId, comprimento);
            resultado.TotalPrumadas = lista.Count;

            // DN nunca menor que a maior prumada
            if (lista.Count > 0)
            {
                var dnMax = lista.Max(p => p.DiametroNominal);
                if (dnMax > resultado.DiametroNominal)
                {
                    resultado.DiametroNominal = dnMax;
                    resultado.Declividade = GetDeclividade(dnMax);
                }
            }

            return resultado;
        }

        /// <summary>
        /// Dimensiona subcoletor a partir de resultados de prumadas já dimensionadas.
        /// </summary>
        public ResultadoSubcoletor DimensionarPorResultados(
            IEnumerable<ResultadoPrumadaEsgoto> prumadas,
            string subcoletorId = "", double comprimento = 0)
        {
            var lista = prumadas?.ToList() ?? new();
            var uhcTotal = lista.Sum(r => r.UHCTotal);

            var resultado = DimensionarSubcoletor(uhcTotal, subcoletorId, comprimento);
            resultado.TotalPrumadas = lista.Count;

            // DN nunca menor que a maior prumada
            if (lista.Count > 0)
            {
                var dnMax = lista.Max(r => r.DiametroNominal);
                if (dnMax > resultado.DiametroNominal)
                {
                    resultado.DiametroNominal = dnMax;
                    resultado.Declividade = GetDeclividade(dnMax);
                }
            }

            return resultado;
        }

        /// <summary>
        /// Dimensiona e atualiza TrechoTubulacao.
        /// </summary>
        public ResultadoSubcoletor DimensionarTrecho(TrechoTubulacao trecho,
            double uhcTotal)
        {
            if (trecho == null)
                return new ResultadoSubcoletor();

            var resultado = DimensionarSubcoletor(uhcTotal, trecho.Id, trecho.Comprimento);

            trecho.DiametroNominal = resultado.DiametroNominal;
            trecho.DiametroInterno = resultado.DiametroNominal * 0.92;
            trecho.Vazao = resultado.VazaoL_s;
            trecho.Velocidade = resultado.Velocidade;
            trecho.Declividade = resultado.Declividade;
            trecho.SomaPesos = uhcTotal;

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona múltiplos subcoletores, cada um com sua lista de prumadas.
        /// </summary>
        public List<ResultadoSubcoletor> DimensionarTodos(
            Dictionary<string, List<Prumada>> subcoletoresComPrumadas)
        {
            if (subcoletoresComPrumadas == null)
                return new List<ResultadoSubcoletor>();

            _log?.Info(ETAPA, COMPONENTE,
                $"Dimensionando {subcoletoresComPrumadas.Count} subcoletores.");

            var resultados = subcoletoresComPrumadas
                .Select(kv => DimensionarPorPrumadas(kv.Value, kv.Key))
                .ToList();

            var aprovados = resultados.Count(r => r.Aprovado);

            _log?.Info(ETAPA, COMPONENTE,
                $"Subcoletores: {aprovados}/{resultados.Count} aprovados.");

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  COLETOR PREDIAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona o coletor predial a partir de todos os subcoletores.
        /// </summary>
        public ResultadoSubcoletor DimensionarColetorPredial(
            IEnumerable<ResultadoSubcoletor> subcoletores, double comprimento = 0)
        {
            var lista = subcoletores?.ToList() ?? new();
            var uhcTotal = lista.Sum(r => r.UHCTotal);

            var resultado = DimensionarSubcoletor(uhcTotal, "COLETOR-PREDIAL", comprimento);
            resultado.TotalPrumadas = lista.Sum(r => r.TotalPrumadas);

            // DN nunca menor que o maior subcoletor
            if (lista.Count > 0)
            {
                var dnMax = lista.Max(r => r.DiametroNominal);
                if (dnMax > resultado.DiametroNominal)
                {
                    resultado.DiametroNominal = dnMax;
                    resultado.Declividade = GetDeclividade(dnMax);
                }
            }

            _log?.Info(ETAPA, COMPONENTE,
                $"Coletor predial: DN{resultado.DiametroNominal}, " +
                $"UHC={uhcTotal:F0}, {lista.Count} subcoletores.");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual.</summary>
        public static string GerarResumo(List<ResultadoSubcoletor> resultados,
            ResultadoSubcoletor? coletorPredial = null)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhum subcoletor dimensionado.";

            var aprovados = resultados.Count(r => r.Aprovado);
            var uhcTotal = resultados.Sum(r => r.UHCTotal);

            var lines = new List<string>
            {
                "══ Subcoletores / Coletor (NBR 8160) ══",
                $"  Subcoletores:   {resultados.Count}",
                $"  Aprovados:      {aprovados}",
                $"  UHC total:      {uhcTotal:F0}",
                "────────────────────────────────────────",
            };

            foreach (var r in resultados.OrderByDescending(r => r.UHCTotal))
            {
                lines.Add($"  {r}");
            }

            if (coletorPredial != null)
            {
                lines.Add("────────────────────────────────────────");
                lines.Add($"  🏠 {coletorPredial}");
            }

            lines.Add("════════════════════════════════════════");

            return string.Join("\n", lines);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Determina DN pelo UHC acumulado (Tab. 6, decl 1%).</summary>
        private static int DeterminarDN(double uhcTotal)
        {
            if (uhcTotal <= 216) return 100;
            if (uhcTotal <= 840) return 150;
            if (uhcTotal <= 1680) return 200;
            if (uhcTotal <= 3000) return 250;
            return 300;
        }

        private static double GetDeclividade(int dn)
        {
            return _declividades.TryGetValue(dn, out var dec) ? dec : 1.0;
        }

        /// <summary>Manning — meia seção, PVC (n=0.010).</summary>
        private static double EstimarVazaoManning(int dnMm, double declividade)
        {
            if (dnMm <= 0 || declividade <= 0)
                return 0.0;

            var d = dnMm / 1000.0;
            var i = declividade / 100.0;

            var area = (Math.PI * d * d) / 8.0;
            var perimetro = (Math.PI * d) / 2.0;
            var rh = area / perimetro;

            var q = (1.0 / MANNING_N) * area * Math.Pow(rh, 2.0 / 3.0) * Math.Sqrt(i);

            return q * 1000.0;
        }

        private static double CalcularVelocidade(double vazaoL_s, int dnMm)
        {
            if (vazaoL_s <= 0 || dnMm <= 0)
                return 0.0;

            var d = dnMm / 1000.0;
            var area = (Math.PI * d * d) / 8.0;

            return (vazaoL_s / 1000.0) / area;
        }

        // Exemplos:
        // var service = new SubcoletoresService(logService);
        //
        // // Por UHC
        // var r = service.DimensionarSubcoletor(uhcTotal: 120, "SC-01");
        // r.DiametroNominal → 100
        //
        // // Por prumadas
        // var r2 = service.DimensionarPorPrumadas(prumadas, "SC-02");
        //
        // // Coletor predial
        // var coletor = service.DimensionarColetorPredial(subcoletores);
        // coletor.DiametroNominal → 200
    }
}
