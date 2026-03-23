using PluginCore.Data;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado do dimensionamento de uma prumada de esgoto (tubo de queda).
    /// </summary>
    public class ResultadoPrumadaEsgoto
    {
        /// <summary>ID da prumada.</summary>
        public string PrumadaId { get; set; } = string.Empty;

        /// <summary>UHC total acumulado.</summary>
        public double UHCTotal { get; set; }

        /// <summary>Diâmetro nominal dimensionado (mm).</summary>
        public int DiametroNominal { get; set; }

        /// <summary>Altura da prumada (m).</summary>
        public double Altura { get; set; }

        /// <summary>Número de pavimentos atendidos.</summary>
        public int Pavimentos { get; set; }

        /// <summary>Total de ramais de esgoto conectados.</summary>
        public int TotalRamais { get; set; }

        /// <summary>Total de pontos conectados.</summary>
        public int TotalPontos { get; set; }

        /// <summary>Necessita tubo de ventilação paralelo.</summary>
        public bool NecessitaVentilacao { get; set; }

        /// <summary>Diâmetro do tubo de ventilação (mm).</summary>
        public int DiametroVentilacao { get; set; }

        /// <summary>Aprovado pela validação.</summary>
        public bool Aprovado { get; set; }

        /// <summary>Alertas gerados.</summary>
        public List<string> Alertas { get; set; } = new();

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            var vent = NecessitaVentilacao ? $" | Vent DN{DiametroVentilacao}" : "";
            return $"{status} Prumada '{PrumadaId}': DN{DiametroNominal} | " +
                   $"UHC={UHCTotal:F0} | {TotalPontos} pontos | " +
                   $"{Pavimentos} pav{vent}";
        }
    }

    /// <summary>
    /// Serviço de dimensionamento de prumadas (tubos de queda) de esgoto.
    /// Conforme NBR 8160:1999 — Tabelas 5, 6 e 7.
    /// </summary>
    public class PrumadasEsgotoService
    {
        private readonly ILogService? _log;

        private const string ETAPA = "07_Esgoto";
        private const string COMPONENTE = "PrumadasEsgoto";

        // ══════════════════════════════════════════════════════════
        //  TABELA NBR 8160 — TUBOS DE QUEDA (Tab. 5)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Número máximo de UHC por diâmetro de tubo de queda.
        /// (prédio > 3 pavimentos)
        /// </summary>
        private static readonly (int dnMm, int maxUHC_3pav, int maxUHC_mais3pav)[] _tabelaTuboQueda =
        {
            (50,   4,    2),
            (75,   12,   6),
            (100,  36,   20),
            (150,  240,  120),
            (200,  960,  480),
            (250,  2200, 1100),
            (300,  4680, 2340),
        };

        /// <summary>
        /// Diâmetro do tubo de ventilação por diâmetro do tubo de queda.
        /// NBR 8160 — Tabela 7.
        /// </summary>
        private static readonly Dictionary<int, int> _ventilacaoPorDN = new()
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

        public PrumadasEsgotoService() { }

        public PrumadasEsgotoService(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona uma prumada de esgoto usando a soma de UHC.
        /// </summary>
        /// <param name="prumada">Modelo da prumada.</param>
        /// <param name="uhcTotal">UHC acumulado de todos os ramais conectados.</param>
        /// <param name="pavimentos">Número de pavimentos atendidos.</param>
        /// <param name="totalPontos">Total de pontos hidráulicos.</param>
        public ResultadoPrumadaEsgoto DimensionarPrumadaEsgoto(Prumada prumada,
            double uhcTotal, int pavimentos = 1, int totalPontos = 0)
        {
            var resultado = new ResultadoPrumadaEsgoto();

            if (prumada == null)
                return resultado;

            resultado.PrumadaId = prumada.Id;
            resultado.Altura = prumada.Altura;
            resultado.UHCTotal = uhcTotal;
            resultado.Pavimentos = pavimentos;
            resultado.TotalPontos = totalPontos > 0
                ? totalPontos
                : prumada.PontosConectados.Count;

            if (uhcTotal <= 0)
            {
                resultado.DiametroNominal = 50;
                resultado.Aprovado = true;
                return resultado;
            }

            // ── 1. Determinar DN pela tabela NBR 8160 ─────────
            var maisDe3Pav = pavimentos > 3;
            var dn = DeterminarDNTuboQueda(uhcTotal, maisDe3Pav);
            resultado.DiametroNominal = dn;

            // ── 2. Regra: DN nunca menor que o maior ramal ────
            // Mínimo absoluto para esgoto com vaso = 100mm
            if (uhcTotal >= 6 && dn < 100)
                dn = 100;

            resultado.DiametroNominal = dn;

            // ── 3. Ventilação ─────────────────────────────────
            resultado.NecessitaVentilacao = pavimentos > 1 || uhcTotal > 6;
            resultado.DiametroVentilacao = GetDNVentilacao(dn);

            // ── 4. Validação ──────────────────────────────────
            resultado.Aprovado = true;

            // Verificar se UHC excede o máximo da tabela
            var maxUHC = GetMaxUHC(dn, maisDe3Pav);
            if (uhcTotal > maxUHC)
            {
                resultado.Alertas.Add(
                    $"UHC ({uhcTotal:F0}) excede capacidade do DN{dn} " +
                    $"(max: {maxUHC}). Considere aumentar o diâmetro.");
                _log?.Medio(ETAPA, COMPONENTE,
                    $"Prumada '{prumada.Id}': UHC={uhcTotal:F0} > max={maxUHC} para DN{dn}.");
            }

            // Verificar altura vs ventilação
            if (prumada.Altura > 6.0 && !resultado.NecessitaVentilacao)
            {
                resultado.NecessitaVentilacao = true;
                resultado.Alertas.Add(
                    $"Altura ({prumada.Altura:F1}m) exige ventilação.");
            }

            // ── 5. Atualizar prumada ──────────────────────────
            prumada.DiametroNominal = dn;

            _log?.Info(ETAPA, COMPONENTE,
                $"Prumada esgoto '{prumada.Id}': DN{dn}, UHC={uhcTotal:F0}, " +
                $"{pavimentos} pav, {resultado.TotalPontos} pontos" +
                $"{(resultado.NecessitaVentilacao ? $", Vent DN{resultado.DiametroVentilacao}" : "")}. " +
                $"{(resultado.Aprovado ? "✅" : "❌")}");

            return resultado;
        }

        /// <summary>
        /// Dimensiona prumada a partir de resultados de ramais de esgoto já calculados.
        /// </summary>
        public ResultadoPrumadaEsgoto DimensionarPorRamais(Prumada prumada,
            IEnumerable<ResultadoRamalEsgoto> ramais, int pavimentos = 1)
        {
            var lista = ramais?.ToList() ?? new();
            var uhcTotal = lista.Sum(r => r.UHCTotal);
            var totalPontos = lista.Sum(r => r.RamaisContribuintes);

            var resultado = DimensionarPrumadaEsgoto(prumada, uhcTotal, pavimentos, totalPontos);
            resultado.TotalRamais = lista.Count;

            // DN nunca menor que o maior ramal conectado
            if (lista.Count > 0)
            {
                var dnMaxRamal = lista.Max(r => r.DiametroNominal);
                if (dnMaxRamal > resultado.DiametroNominal)
                {
                    resultado.DiametroNominal = dnMaxRamal;
                    if (prumada != null)
                        prumada.DiametroNominal = dnMaxRamal;
                }
            }

            // Ventilação se algum ramal exigir
            if (lista.Any(r => r.NecessitaVentilacao))
            {
                resultado.NecessitaVentilacao = true;
                resultado.DiametroVentilacao = GetDNVentilacao(resultado.DiametroNominal);
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona todas as prumadas de esgoto de um sistema.
        /// </summary>
        public List<ResultadoPrumadaEsgoto> DimensionarTodas(
            IEnumerable<Prumada> prumadas, double uhcPorPrumada, int pavimentos = 1)
        {
            var lista = prumadas?.Where(p => p != null).ToList() ?? new();

            _log?.Info(ETAPA, COMPONENTE,
                $"Dimensionando {lista.Count} prumadas de esgoto.");

            var resultados = lista
                .Select(p => DimensionarPrumadaEsgoto(p, uhcPorPrumada, pavimentos))
                .ToList();

            var aprovados = resultados.Count(r => r.Aprovado);

            _log?.Info(ETAPA, COMPONENTE,
                $"Prumadas esgoto: {aprovados}/{resultados.Count} aprovadas.");

            return resultados;
        }

        /// <summary>
        /// Dimensiona prumadas de esgoto de um SistemaMEP.
        /// </summary>
        public List<ResultadoPrumadaEsgoto> DimensionarSistema(SistemaMEP sistema, int pavimentos = 1)
        {
            if (sistema == null)
                return new List<ResultadoPrumadaEsgoto>();

            var prumadas = sistema.Prumadas
                .Where(p => p.Sistema == HydraulicSystem.Sewer)
                .ToList();

            var uhcTotal = sistema.Pontos
                .Where(p => p.Sistema == HydraulicSystem.Sewer)
                .Sum(p => p.PesoRelativo);

            // Distribuir UHC proporcionalmente
            var uhcPorPrumada = prumadas.Count > 0 ? uhcTotal / prumadas.Count : 0;

            return DimensionarTodas(prumadas, uhcPorPrumada, pavimentos);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual.</summary>
        public static string GerarResumo(List<ResultadoPrumadaEsgoto> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhuma prumada de esgoto dimensionada.";

            var aprovados = resultados.Count(r => r.Aprovado);
            var uhcTotal = resultados.Sum(r => r.UHCTotal);
            var dnMax = resultados.Max(r => r.DiametroNominal);
            var comVent = resultados.Count(r => r.NecessitaVentilacao);

            var lines = new List<string>
            {
                "══ Prumadas de Esgoto (NBR 8160) ══",
                $"  Total:          {resultados.Count}",
                $"  Aprovadas:      {aprovados}",
                $"  Reprovadas:     {resultados.Count - aprovados}",
                $"  UHC total:      {uhcTotal:F0}",
                $"  DN máximo:      {dnMax} mm",
                $"  Com ventilação: {comVent}",
                "────────────────────────────────────",
            };

            foreach (var r in resultados.OrderByDescending(r => r.UHCTotal))
            {
                lines.Add($"  {r}");
            }

            lines.Add("════════════════════════════════════");

            return string.Join("\n", lines);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Determina DN do tubo de queda pela tabela NBR 8160 (Tab. 5).
        /// </summary>
        private static int DeterminarDNTuboQueda(double uhcTotal, bool maisDe3Pavimentos)
        {
            foreach (var (dnMm, max3, maxMais3) in _tabelaTuboQueda)
            {
                var maxUHC = maisDe3Pavimentos ? maxMais3 : max3;
                if (uhcTotal <= maxUHC)
                    return dnMm;
            }

            return 300; // Maior disponível
        }

        /// <summary>Retorna máximo UHC para um DN.</summary>
        private static int GetMaxUHC(int dn, bool maisDe3Pav)
        {
            foreach (var (dnMm, max3, maxMais3) in _tabelaTuboQueda)
            {
                if (dnMm == dn)
                    return maisDe3Pav ? maxMais3 : max3;
            }
            return 0;
        }

        /// <summary>Retorna DN do tubo de ventilação (Tab. 7).</summary>
        private static int GetDNVentilacao(int dnTuboQueda)
        {
            return _ventilacaoPorDN.TryGetValue(dnTuboQueda, out var dnVent) ? dnVent : 50;
        }

        // Exemplos:
        // var service = new PrumadasEsgotoService(logService);
        //
        // // Por UHC direto
        // var r = service.DimensionarPrumadaEsgoto(prumada, uhcTotal: 36, pavimentos: 5);
        // r.DiametroNominal    → 150 (Tab.5: 36 > 20 para >3 pav)
        // r.NecessitaVentilacao → true
        // r.DiametroVentilacao → 100
        //
        // // Por ramais já dimensionados
        // var r2 = service.DimensionarPorRamais(prumada, ramaisEsgoto, pavimentos: 8);
        //
        // // Sistema completo
        // var todos = service.DimensionarSistema(sistemaMEP, pavimentos: 10);
        // Console.WriteLine(PrumadasEsgotoService.GerarResumo(todos));
    }
}
