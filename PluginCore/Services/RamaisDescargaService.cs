using PluginCore.Data;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado do dimensionamento de um ramal de descarga/esgoto.
    /// </summary>
    public class ResultadoRamal
    {
        /// <summary>ID do trecho.</summary>
        public string TrechoId { get; set; } = string.Empty;

        /// <summary>UHC total dos aparelhos conectados.</summary>
        public double UHCTotal { get; set; }

        /// <summary>Diâmetro nominal dimensionado (mm).</summary>
        public int DiametroNominal { get; set; }

        /// <summary>Declividade aplicada (%).</summary>
        public double Declividade { get; set; }

        /// <summary>Vazão estimada (L/s).</summary>
        public double VazaoL_s { get; set; }

        /// <summary>Velocidade estimada (m/s).</summary>
        public double Velocidade { get; set; }

        /// <summary>Aparelhos conectados.</summary>
        public List<string> Aparelhos { get; set; } = new();

        /// <summary>Se o dimensionamento atende aos critérios.</summary>
        public bool Aprovado { get; set; }

        /// <summary>Alertas gerados.</summary>
        public List<string> Alertas { get; set; } = new();

        /// <summary>Necessita ventilação.</summary>
        public bool NecessitaVentilacao { get; set; }

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            return $"{status} Ramal '{TrechoId}': DN{DiametroNominal} | " +
                   $"UHC={UHCTotal:F0} | Dec={Declividade}% | V={Velocidade:F2} m/s";
        }
    }

    /// <summary>
    /// Serviço de dimensionamento de ramais de descarga e esgoto.
    /// Conforme NBR 8160:1999.
    /// </summary>
    public class RamaisDescargaService
    {
        private readonly ILogService? _log;

        private const string ETAPA = "07_Esgoto";
        private const string COMPONENTE = "RamaisDescarga";

        // ══════════════════════════════════════════════════════════
        //  CONSTANTES NBR 8160
        // ══════════════════════════════════════════════════════════

        /// <summary>Velocidade mínima de autolimpeza (m/s).</summary>
        private const double VELOCIDADE_MINIMA = 0.6;

        /// <summary>Velocidade máxima (m/s).</summary>
        private const double VELOCIDADE_MAXIMA = 4.0;

        /// <summary>Declividades mínimas por diâmetro (%).</summary>
        private static readonly Dictionary<int, double> _declividades = new()
        {
            [40] = 2.0,
            [50] = 2.0,
            [75] = 2.0,
            [100] = 1.0,
            [150] = 0.5,
            [200] = 0.5,
            [250] = 0.5,
            [300] = 0.5,
        };

        /// <summary>Diâmetros comerciais de esgoto PVC (mm).</summary>
        private static readonly int[] DIAMETROS_ESGOTO = { 40, 50, 75, 100, 150, 200, 250, 300 };

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public RamaisDescargaService() { }

        public RamaisDescargaService(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona um ramal de descarga a partir do UHC acumulado.
        /// </summary>
        public ResultadoRamal DimensionarRamal(double uhcAcumulado,
            string trechoId = "", List<string>? aparelhos = null)
        {
            var resultado = new ResultadoRamal
            {
                TrechoId = trechoId,
                UHCTotal = uhcAcumulado,
                Aparelhos = aparelhos ?? new(),
            };

            if (uhcAcumulado <= 0)
            {
                resultado.DiametroNominal = DIAMETROS_ESGOTO[0];
                resultado.Aprovado = true;
                return resultado;
            }

            // ── 1. Determinar diâmetro pelo UHC ──────────────
            var dn = UHCTable.DeterminarDiametroEsgoto(uhcAcumulado);

            // Verificar se algum aparelho exige diâmetro maior
            if (aparelhos != null)
            {
                foreach (var aparelho in aparelhos)
                {
                    var dnRamal = UHCTable.DiametroRamal(aparelho);
                    if (dnRamal > dn)
                        dn = dnRamal;

                    if (UHCTable.NecessitaVentilacao(aparelho))
                        resultado.NecessitaVentilacao = true;
                }
            }

            resultado.DiametroNominal = dn;

            // ── 2. Aplicar declividade ────────────────────────
            resultado.Declividade = GetDeclividade(dn);

            // ── 3. Estimar vazão (Manning simplificado) ───────
            var vazao = EstimarVazaoEsgoto(dn, resultado.Declividade);
            resultado.VazaoL_s = vazao;

            // ── 4. Calcular velocidade ────────────────────────
            var velocidade = CalcularVelocidade(vazao, dn);
            resultado.Velocidade = velocidade;

            // ── 5. Validar ────────────────────────────────────
            resultado.Aprovado = true;

            if (velocidade > VELOCIDADE_MAXIMA)
            {
                resultado.Aprovado = false;
                resultado.Alertas.Add(
                    $"Velocidade excedida: {velocidade:F2} m/s > {VELOCIDADE_MAXIMA} m/s.");
                _log?.Critico(ETAPA, COMPONENTE,
                    $"Ramal '{trechoId}': velocidade {velocidade:F2} m/s excede o limite.");
            }
            else if (velocidade < VELOCIDADE_MINIMA && velocidade > 0)
            {
                resultado.Alertas.Add(
                    $"Velocidade abaixo da autolimpeza: {velocidade:F2} m/s < {VELOCIDADE_MINIMA} m/s.");
                _log?.Leve(ETAPA, COMPONENTE,
                    $"Ramal '{trechoId}': velocidade {velocidade:F2} m/s " +
                    $"abaixo da autolimpeza ({VELOCIDADE_MINIMA} m/s).");
            }

            if (resultado.NecessitaVentilacao)
            {
                _log?.Info(ETAPA, COMPONENTE,
                    $"Ramal '{trechoId}': necessita ventilação individual.");
            }

            _log?.Info(ETAPA, COMPONENTE,
                $"Ramal '{trechoId}' dimensionado: DN{dn}, " +
                $"UHC={uhcAcumulado:F0}, Dec={resultado.Declividade}%, " +
                $"V={velocidade:F2} m/s. " +
                $"{(resultado.Aprovado ? "✅" : "❌")}");

            return resultado;
        }

        /// <summary>
        /// Dimensiona ramal a partir de uma lista de nomes de aparelhos.
        /// </summary>
        public ResultadoRamal DimensionarPorAparelhos(IEnumerable<string> aparelhos,
            string trechoId = "")
        {
            var lista = aparelhos?.ToList() ?? new();
            var uhc = UHCTable.SomaUHC(lista);
            return DimensionarRamal(uhc, trechoId, lista);
        }

        /// <summary>
        /// Dimensiona e atualiza um TrechoTubulacao de esgoto.
        /// </summary>
        public ResultadoRamal DimensionarTrecho(TrechoTubulacao trecho,
            IEnumerable<string> aparelhos)
        {
            if (trecho == null)
                return new ResultadoRamal();

            var lista = aparelhos?.ToList() ?? new();
            var uhc = UHCTable.SomaUHC(lista);
            var resultado = DimensionarRamal(uhc, trecho.Id, lista);

            // Atualizar o trecho
            trecho.DiametroNominal = resultado.DiametroNominal;
            trecho.DiametroInterno = resultado.DiametroNominal * 0.92; // PVC esgoto
            trecho.Vazao = resultado.VazaoL_s;
            trecho.Velocidade = resultado.Velocidade;
            trecho.Declividade = resultado.Declividade;
            trecho.SomaPesos = uhc;

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona múltiplos ramais.
        /// </summary>
        public List<ResultadoRamal> DimensionarTodos(
            Dictionary<string, List<string>> ramaisComAparelhos)
        {
            if (ramaisComAparelhos == null)
                return new List<ResultadoRamal>();

            _log?.Info(ETAPA, COMPONENTE,
                $"Dimensionando {ramaisComAparelhos.Count} ramais de descarga.");

            var resultados = ramaisComAparelhos
                .Select(kv => DimensionarPorAparelhos(kv.Value, kv.Key))
                .ToList();

            var aprovados = resultados.Count(r => r.Aprovado);
            var comVentilacao = resultados.Count(r => r.NecessitaVentilacao);

            _log?.Info(ETAPA, COMPONENTE,
                $"Ramais dimensionados: {aprovados}/{resultados.Count} aprovados, " +
                $"{comVentilacao} necessitam ventilação.");

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera resumo textual do dimensionamento.
        /// </summary>
        public static string GerarResumo(List<ResultadoRamal> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhum ramal dimensionado.";

            var aprovados = resultados.Count(r => r.Aprovado);
            var uhcTotal = resultados.Sum(r => r.UHCTotal);
            var comVent = resultados.Count(r => r.NecessitaVentilacao);

            var lines = new List<string>
            {
                "══ Ramais de Descarga (NBR 8160) ══",
                $"  Total:           {resultados.Count}",
                $"  Aprovados:       {aprovados}",
                $"  Reprovados:      {resultados.Count - aprovados}",
                $"  UHC total:       {uhcTotal:F0}",
                $"  Com ventilação:  {comVent}",
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

        /// <summary>Retorna declividade mínima por diâmetro.</summary>
        public static double GetDeclividade(int diametroNominal)
        {
            return _declividades.TryGetValue(diametroNominal, out var dec) ? dec : 1.0;
        }

        /// <summary>
        /// Estima vazão em tubo de esgoto (Manning simplificado, meia seção).
        /// Q = (1/n) × A × R^(2/3) × i^(1/2)
        /// Para PVC n=0.010, meia seção.
        /// </summary>
        private static double EstimarVazaoEsgoto(int dnMm, double declividade)
        {
            if (dnMm <= 0 || declividade <= 0)
                return 0.0;

            var n = 0.010; // Manning PVC
            var d = dnMm / 1000.0; // mm → m
            var i = declividade / 100.0; // % → m/m

            // Meia seção (y/D = 0.5)
            var area = (Math.PI * d * d) / 8.0;
            var perimetro = (Math.PI * d) / 2.0;
            var rh = area / perimetro;

            var q = (1.0 / n) * area * Math.Pow(rh, 2.0 / 3.0) * Math.Sqrt(i);

            return q * 1000.0; // m³/s → L/s
        }

        /// <summary>Calcula velocidade no tubo (meia seção).</summary>
        private static double CalcularVelocidade(double vazaoL_s, int dnMm)
        {
            if (vazaoL_s <= 0 || dnMm <= 0)
                return 0.0;

            var d = dnMm / 1000.0;
            var area = (Math.PI * d * d) / 8.0; // Meia seção
            var q = vazaoL_s / 1000.0; // L/s → m³/s

            return q / area;
        }

        // Exemplos:
        // var service = new RamaisDescargaService(logService);
        //
        // var r = service.DimensionarPorAparelhos(
        //     new[] { "vaso sanitario", "lavatorio", "chuveiro" }, "R-01");
        // r.DiametroNominal   → 100  (vaso exige Ø100)
        // r.UHCTotal          → 9.0
        // r.Declividade       → 1.0%
        // r.NecessitaVentilacao → true
        //
        // var r2 = service.DimensionarPorAparelhos(
        //     new[] { "lavatorio", "chuveiro" }, "R-02");
        // r2.DiametroNominal  → 50
        // r2.UHCTotal         → 3.0
    }
}
