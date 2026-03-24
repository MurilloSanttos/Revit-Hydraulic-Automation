using PluginCore.Data;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado do dimensionamento de uma coluna de ventilação.
    /// </summary>
    public class ResultadoColunaVentilacao
    {
        /// <summary>ID da coluna.</summary>
        public string ColunaId { get; set; } = string.Empty;

        /// <summary>UHC total das prumadas atendidas.</summary>
        public double UHCTotal { get; set; }

        /// <summary>Diâmetro da prumada de esgoto associada (mm).</summary>
        public int DiametroPrumadaMm { get; set; }

        /// <summary>Diâmetro da coluna de ventilação (mm).</summary>
        public int DiametroNominal { get; set; }

        /// <summary>Altura da coluna (m).</summary>
        public double Altura { get; set; }

        /// <summary>Comprimento máximo permitido (m).</summary>
        public double ComprimentoMaximo { get; set; }

        /// <summary>Prumadas atendidas.</summary>
        public int TotalPrumadas { get; set; }

        /// <summary>Aprovado.</summary>
        public bool Aprovado { get; set; }

        /// <summary>Alertas.</summary>
        public List<string> Alertas { get; set; } = new();

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            return $"{status} Vent '{ColunaId}': DN{DiametroNominal} | " +
                   $"UHC={UHCTotal:F0} | Prumada DN{DiametroPrumadaMm} | " +
                   $"H={Altura:F1}m | {TotalPrumadas} prumadas";
        }
    }

    /// <summary>
    /// Serviço de dimensionamento de colunas de ventilação de esgoto.
    /// Conforme NBR 8160:1999 — Tabelas 7 e 9.
    /// </summary>
    public class ColunasVentilacaoService
    {
        private readonly ILogService? _log;

        private const string ETAPA = "08_Ventilacao";
        private const string COMPONENTE = "ColunasVentilacao";

        // ══════════════════════════════════════════════════════════
        //  TABELA NBR 8160 — COLUNAS DE VENTILAÇÃO (Tab. 9)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Diâmetro e comprimento máximo da coluna de ventilação.
        /// (DN prumada, DN ventilação, UHC máx, comprimento máx m)
        /// </summary>
        private static readonly (int dnPrumada, int dnVent, int uhcMax, double compMaxM)[] _tabelaVentilacao =
        {
            // DN Prumada → DN Ventilação, UHC máx, Comprimento máx
            (50,  40,    4,    7.5),
            (50,  50,    4,    15.0),
            (75,  50,    12,   9.0),
            (75,  75,    12,   30.0),
            (100, 50,    20,   3.0),
            (100, 75,    20,   9.0),
            (100, 100,   20,   45.0),
            (150, 75,    120,  6.0),
            (150, 100,   120,  24.0),
            (150, 150,   120,  75.0),
            (200, 100,   480,  15.0),
            (200, 150,   480,  45.0),
            (200, 200,   480,  105.0),
            (300, 150,   2340, 30.0),
            (300, 200,   2340, 75.0),
            (300, 300,   2340, 150.0),
        };

        /// <summary>
        /// Diâmetro mínimo da coluna de ventilação por DN da prumada (Tab. 7).
        /// </summary>
        private static readonly Dictionary<int, int> _dnMinimoPorPrumada = new()
        {
            [50] = 40,
            [75] = 50,
            [100] = 75,
            [150] = 100,
            [200] = 100,
            [250] = 150,
            [300] = 150,
        };

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public ColunasVentilacaoService() { }

        public ColunasVentilacaoService(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona coluna de ventilação para uma prumada de esgoto.
        /// </summary>
        public ResultadoColunaVentilacao DimensionarColunaVentilacao(
            Prumada prumada, double uhcTotal)
        {
            var resultado = new ResultadoColunaVentilacao();

            if (prumada == null)
                return resultado;

            resultado.ColunaId = $"VENT-{prumada.Id}";
            resultado.DiametroPrumadaMm = prumada.DiametroNominal;
            resultado.Altura = prumada.Altura;
            resultado.UHCTotal = uhcTotal;
            resultado.TotalPrumadas = 1;

            // ── 1. DN mínimo pela tabela 7 ────────────────────
            var dnMin = GetDNMinimo(prumada.DiametroNominal);

            // ── 2. DN pela tabela 9 (considerando altura) ─────
            var dn = DeterminarDNPorTabela(
                prumada.DiametroNominal, uhcTotal, prumada.Altura);

            // Garantir mínimo
            if (dn < dnMin)
                dn = dnMin;

            resultado.DiametroNominal = dn;

            // ── 3. Comprimento máximo ─────────────────────────
            resultado.ComprimentoMaximo = GetComprimentoMaximo(
                prumada.DiametroNominal, dn, uhcTotal);

            // ── 4. Validação ──────────────────────────────────
            resultado.Aprovado = true;

            if (prumada.Altura > resultado.ComprimentoMaximo && resultado.ComprimentoMaximo > 0)
            {
                resultado.Alertas.Add(
                    $"Altura ({prumada.Altura:F1}m) excede comprimento máximo " +
                    $"({resultado.ComprimentoMaximo:F1}m) para DN{dn}. " +
                    $"Aumentar diâmetro da ventilação.");

                // Tentar próximo DN
                var dnMaior = ProximoDN(dn);
                if (dnMaior > dn)
                {
                    var compMaior = GetComprimentoMaximo(
                        prumada.DiametroNominal, dnMaior, uhcTotal);

                    if (prumada.Altura <= compMaior)
                    {
                        resultado.DiametroNominal = dnMaior;
                        resultado.ComprimentoMaximo = compMaior;
                        resultado.Alertas.Add(
                            $"Ajustado para DN{dnMaior} (comp máx: {compMaior:F1}m).");
                    }
                    else
                    {
                        resultado.Aprovado = false;
                        _log?.Critico(ETAPA, COMPONENTE,
                            $"Coluna '{resultado.ColunaId}': altura {prumada.Altura:F1}m " +
                            $"excede limite para DN{dnMaior}.");
                    }
                }
            }

            _log?.Info(ETAPA, COMPONENTE,
                $"Coluna ventilação '{resultado.ColunaId}': DN{resultado.DiametroNominal}, " +
                $"Prumada DN{prumada.DiametroNominal}, UHC={uhcTotal:F0}, " +
                $"H={prumada.Altura:F1}m (máx: {resultado.ComprimentoMaximo:F1}m). " +
                $"{(resultado.Aprovado ? "✅" : "❌")}");

            return resultado;
        }

        /// <summary>
        /// Dimensiona coluna de ventilação agrupando múltiplas prumadas.
        /// </summary>
        public ResultadoColunaVentilacao DimensionarPorPrumadas(
            IEnumerable<Prumada> prumadas, string colunaId = "")
        {
            var lista = prumadas?.Where(p => p != null).ToList() ?? new();
            var resultado = new ResultadoColunaVentilacao { ColunaId = colunaId };

            if (lista.Count == 0)
            {
                resultado.Aprovado = true;
                return resultado;
            }

            var uhcTotal = lista.Sum(p => p.SomaPesos);
            var dnMaxPrumada = lista.Max(p => p.DiametroNominal);
            var alturaMax = lista.Max(p => p.Altura);

            resultado.UHCTotal = uhcTotal;
            resultado.DiametroPrumadaMm = dnMaxPrumada;
            resultado.Altura = alturaMax;
            resultado.TotalPrumadas = lista.Count;

            // DN mínimo
            var dnMin = GetDNMinimo(dnMaxPrumada);
            var dn = DeterminarDNPorTabela(dnMaxPrumada, uhcTotal, alturaMax);
            if (dn < dnMin) dn = dnMin;

            resultado.DiametroNominal = dn;
            resultado.ComprimentoMaximo = GetComprimentoMaximo(dnMaxPrumada, dn, uhcTotal);
            resultado.Aprovado = alturaMax <= resultado.ComprimentoMaximo ||
                                  resultado.ComprimentoMaximo == 0;

            if (!resultado.Aprovado)
            {
                resultado.Alertas.Add(
                    $"Altura {alturaMax:F1}m > máx {resultado.ComprimentoMaximo:F1}m.");
                _log?.Critico(ETAPA, COMPONENTE,
                    $"Coluna '{colunaId}': altura excede limite para DN{dn}.");
            }

            _log?.Info(ETAPA, COMPONENTE,
                $"Coluna '{colunaId}': DN{dn}, {lista.Count} prumadas, " +
                $"UHC={uhcTotal:F0}, H={alturaMax:F1}m. " +
                $"{(resultado.Aprovado ? "✅" : "❌")}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona colunas de ventilação para todas as prumadas de esgoto.
        /// Uma coluna por prumada.
        /// </summary>
        public List<ResultadoColunaVentilacao> DimensionarTodas(
            IEnumerable<Prumada> prumadas)
        {
            var lista = prumadas?
                .Where(p => p != null && p.Sistema == HydraulicSystem.Sewer)
                .ToList() ?? new();

            _log?.Info(ETAPA, COMPONENTE,
                $"Dimensionando {lista.Count} colunas de ventilação.");

            var resultados = lista
                .Select(p => DimensionarColunaVentilacao(p, p.SomaPesos))
                .ToList();

            var aprovados = resultados.Count(r => r.Aprovado);

            _log?.Info(ETAPA, COMPONENTE,
                $"Colunas ventilação: {aprovados}/{resultados.Count} aprovadas.");

            return resultados;
        }

        /// <summary>
        /// Dimensiona colunas de ventilação de um SistemaMEP.
        /// </summary>
        public List<ResultadoColunaVentilacao> DimensionarSistema(SistemaMEP sistema)
        {
            if (sistema == null)
                return new List<ResultadoColunaVentilacao>();

            return DimensionarTodas(sistema.Prumadas);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual.</summary>
        public static string GerarResumo(List<ResultadoColunaVentilacao> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhuma coluna de ventilação dimensionada.";

            var aprovados = resultados.Count(r => r.Aprovado);
            var dnMax = resultados.Max(r => r.DiametroNominal);

            var lines = new List<string>
            {
                "══ Colunas de Ventilação (NBR 8160) ══",
                $"  Total:       {resultados.Count}",
                $"  Aprovadas:   {aprovados}",
                $"  Reprovadas:  {resultados.Count - aprovados}",
                $"  DN máximo:   {dnMax} mm",
                "───────────────────────────────────────",
            };

            foreach (var r in resultados.OrderByDescending(r => r.UHCTotal))
            {
                lines.Add($"  {r}");
            }

            lines.Add("═══════════════════════════════════════");

            return string.Join("\n", lines);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>DN mínimo da ventilação por DN da prumada (Tab. 7).</summary>
        private static int GetDNMinimo(int dnPrumada)
        {
            if (_dnMinimoPorPrumada.TryGetValue(dnPrumada, out var dn))
                return dn;

            // Buscar próximo
            foreach (var kv in _dnMinimoPorPrumada.OrderBy(k => k.Key))
            {
                if (kv.Key >= dnPrumada)
                    return kv.Value;
            }

            return 150;
        }

        /// <summary>Determina DN pela tabela 9 considerando altura.</summary>
        private static int DeterminarDNPorTabela(int dnPrumada, double uhc, double altura)
        {
            var candidatos = _tabelaVentilacao
                .Where(t => t.dnPrumada >= dnPrumada && t.uhcMax >= uhc && t.compMaxM >= altura)
                .OrderBy(t => t.dnVent)
                .ThenBy(t => t.dnPrumada)
                .ToList();

            if (candidatos.Count > 0)
                return candidatos[0].dnVent;

            // Fallback: DN mínimo pela tabela 7
            return GetDNMinimo(dnPrumada);
        }

        /// <summary>Comprimento máximo pela tabela 9.</summary>
        private static double GetComprimentoMaximo(int dnPrumada, int dnVent, double uhc)
        {
            var match = _tabelaVentilacao
                .Where(t => t.dnPrumada == dnPrumada && t.dnVent == dnVent && t.uhcMax >= uhc)
                .OrderBy(t => t.uhcMax)
                .FirstOrDefault();

            return match.compMaxM;
        }

        /// <summary>Próximo DN comercial de ventilação.</summary>
        private static int ProximoDN(int dnAtual)
        {
            int[] dns = { 40, 50, 75, 100, 150, 200, 300 };
            foreach (var dn in dns)
            {
                if (dn > dnAtual)
                    return dn;
            }
            return dnAtual;
        }

        // Exemplos:
        // var service = new ColunasVentilacaoService(logService);
        //
        // // Por prumada individual
        // var r = service.DimensionarColunaVentilacao(prumada, uhcTotal: 20);
        // r.DiametroNominal    → 75
        // r.ComprimentoMaximo  → 9.0 m
        //
        // // Agrupar prumadas
        // var r2 = service.DimensionarPorPrumadas(prumadas, "VENT-01");
        //
        // // Sistema completo
        // var todos = service.DimensionarSistema(sistemaMEP);
        // Console.WriteLine(ColunasVentilacaoService.GerarResumo(todos));
    }
}
