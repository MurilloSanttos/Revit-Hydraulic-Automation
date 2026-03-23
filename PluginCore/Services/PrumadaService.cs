using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado do dimensionamento de uma prumada.
    /// </summary>
    public class ResultadoPrumada
    {
        /// <summary>ID da prumada.</summary>
        public string PrumadaId { get; set; } = string.Empty;

        /// <summary>Sistema (AF, AQ, ES).</summary>
        public HydraulicSystem Sistema { get; set; }

        /// <summary>Soma dos pesos (UHC).</summary>
        public double SomaPesos { get; set; }

        /// <summary>Vazão provável (L/s).</summary>
        public double VazaoL_s { get; set; }

        /// <summary>Vazão (m³/h).</summary>
        public double VazaoM3_h => VazaoL_s * 3.6;

        /// <summary>Diâmetro nominal dimensionado (mm).</summary>
        public int DiametroNominal { get; set; }

        /// <summary>Diâmetro interno (mm).</summary>
        public double DiametroInterno { get; set; }

        /// <summary>Velocidade resultante (m/s).</summary>
        public double Velocidade { get; set; }

        /// <summary>Perda de carga na prumada (mCA).</summary>
        public double PerdaCarga { get; set; }

        /// <summary>Altura da prumada (m).</summary>
        public double Altura { get; set; }

        /// <summary>Pontos atendidos.</summary>
        public int TotalPontos { get; set; }

        /// <summary>Se o dimensionamento atende aos critérios.</summary>
        public bool Aprovado { get; set; }

        /// <summary>Alertas gerados.</summary>
        public List<string> Alertas { get; set; } = new();

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            return $"{status} Prumada '{PrumadaId}': DN{DiametroNominal} | " +
                   $"Q={VazaoL_s:F3} L/s | V={Velocidade:F2} m/s | " +
                   $"ΣP={SomaPesos:F1} | H={PerdaCarga:F3} mCA";
        }
    }

    /// <summary>
    /// Serviço de dimensionamento de prumadas (colunas verticais).
    /// Integra vazão, diâmetro, velocidade e perda de carga.
    /// </summary>
    public class PrumadaService
    {
        private readonly DimensionamentoService _dimensionamento;
        private readonly ILogService? _log;

        private const string ETAPA = "06_Prumada";
        private const string COMPONENTE = "PrumadaService";

        /// <summary>Velocidade máxima (m/s) — NBR 5626.</summary>
        private const double VELOCIDADE_MAXIMA = 3.0;

        /// <summary>Velocidade mínima recomendada (m/s).</summary>
        private const double VELOCIDADE_MINIMA = 0.5;

        public PrumadaService()
        {
            _dimensionamento = new DimensionamentoService();
        }

        public PrumadaService(ILogService log)
        {
            _log = log;
            _dimensionamento = new DimensionamentoService();
        }

        public PrumadaService(ILogService log, DimensionamentoService dimensionamento)
        {
            _log = log;
            _dimensionamento = dimensionamento;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona uma prumada e preenche suas propriedades.
        /// </summary>
        /// <returns>Resultado com diâmetro, vazão, velocidade e alertas.</returns>
        public ResultadoPrumada DimensionarPrumada(Prumada prumada)
        {
            var resultado = new ResultadoPrumada();

            if (prumada == null)
                return resultado;

            resultado.PrumadaId = prumada.Id;
            resultado.Sistema = prumada.Sistema;
            resultado.Altura = prumada.Altura;
            resultado.TotalPontos = prumada.PontosConectados.Count;
            resultado.SomaPesos = prumada.SomaPesos;

            // ── 1. Calcular vazão provável ────────────────────
            var vazao = _dimensionamento.CalcularVazaoProvavel(prumada.SomaPesos);
            resultado.VazaoL_s = vazao;

            if (vazao <= 0)
            {
                resultado.Alertas.Add("Vazão zero — sem pontos com peso.");
                _log?.Leve(ETAPA, COMPONENTE,
                    $"Prumada '{prumada.Id}': vazão zero (ΣP={prumada.SomaPesos:F1}).");
                resultado.Aprovado = true;
                return resultado;
            }

            // ── 2. Determinar diâmetro ────────────────────────
            var dn = _dimensionamento.DeterminarDiametro(vazao, prumada.Sistema);
            resultado.DiametroNominal = dn;

            // Diâmetro interno
            var di = GetDiametroInterno(dn);
            resultado.DiametroInterno = di;

            // ── 3. Calcular velocidade ────────────────────────
            var velocidade = _dimensionamento.CalcularVelocidade(vazao, di);
            resultado.Velocidade = velocidade;

            // ── 4. Calcular perda de carga na prumada ─────────
            // Criar trecho virtual para a prumada
            var trechoVirtual = new TrechoTubulacao
            {
                Id = $"PRU-{prumada.Id}",
                Sistema = prumada.Sistema,
                Material = prumada.Material,
                DiametroNominal = dn,
                DiametroInterno = di,
                Comprimento = prumada.Altura,
                Vazao = vazao,
                SomaPesos = prumada.SomaPesos,
                Velocidade = velocidade,
            };

            var perdaCarga = PerdaCargaService.CalcularPerdaCarga(
                trechoVirtual, MetodoPerdaCarga.FairWhippleHsiao);
            resultado.PerdaCarga = perdaCarga;

            // ── 5. Validar ────────────────────────────────────
            resultado.Aprovado = true;

            if (velocidade > VELOCIDADE_MAXIMA)
            {
                resultado.Aprovado = false;
                resultado.Alertas.Add(
                    $"Velocidade excedida: {velocidade:F2} m/s > {VELOCIDADE_MAXIMA} m/s. " +
                    $"Aumentar diâmetro.");
                _log?.Critico(ETAPA, COMPONENTE,
                    $"Prumada '{prumada.Id}': velocidade {velocidade:F2} m/s " +
                    $"excede {VELOCIDADE_MAXIMA} m/s (DN{dn}).");
            }
            else if (velocidade < VELOCIDADE_MINIMA && velocidade > 0)
            {
                resultado.Alertas.Add(
                    $"Velocidade baixa: {velocidade:F2} m/s. Risco de sedimentação.");
                _log?.Leve(ETAPA, COMPONENTE,
                    $"Prumada '{prumada.Id}': velocidade baixa {velocidade:F2} m/s (DN{dn}).");
            }

            // ── 6. Atualizar a prumada ────────────────────────
            prumada.DiametroNominal = dn;
            prumada.VazaoTotal = vazao;

            _log?.Info(ETAPA, COMPONENTE,
                $"Prumada '{prumada.Id}' dimensionada: DN{dn}, " +
                $"Q={vazao:F3} L/s, V={velocidade:F2} m/s, " +
                $"H={perdaCarga:F3} mCA. " +
                $"{(resultado.Aprovado ? "✅ Aprovado" : "❌ Reprovado")}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona todas as prumadas de um sistema.
        /// </summary>
        public List<ResultadoPrumada> DimensionarTodas(IEnumerable<Prumada> prumadas)
        {
            if (prumadas == null)
                return new List<ResultadoPrumada>();

            var lista = prumadas.Where(p => p != null).ToList();

            _log?.Info(ETAPA, COMPONENTE,
                $"Dimensionando {lista.Count} prumadas.");

            var resultados = lista
                .Select(p => DimensionarPrumada(p))
                .ToList();

            var aprovadas = resultados.Count(r => r.Aprovado);
            var reprovadas = resultados.Count - aprovadas;

            _log?.Info(ETAPA, COMPONENTE,
                $"Dimensionamento concluído: " +
                $"{aprovadas} aprovadas, {reprovadas} reprovadas.");

            return resultados;
        }

        /// <summary>
        /// Dimensiona prumadas de um SistemaMEP.
        /// </summary>
        public List<ResultadoPrumada> DimensionarSistema(SistemaMEP sistema)
        {
            if (sistema == null)
                return new List<ResultadoPrumada>();

            return DimensionarTodas(sistema.Prumadas);
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera resumo textual do dimensionamento.
        /// </summary>
        public static string GerarResumo(List<ResultadoPrumada> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhuma prumada dimensionada.";

            var aprovadas = resultados.Count(r => r.Aprovado);
            var vazaoTotal = resultados.Sum(r => r.VazaoL_s);
            var perdaMax = resultados.Max(r => r.PerdaCarga);
            var dnMax = resultados.Max(r => r.DiametroNominal);

            var lines = new List<string>
            {
                "══ Dimensionamento de Prumadas ══",
                $"  Total:          {resultados.Count}",
                $"  Aprovadas:      {aprovadas}",
                $"  Reprovadas:     {resultados.Count - aprovadas}",
                "──────────────────────────────────",
                $"  Q total:        {vazaoTotal:F3} L/s ({vazaoTotal * 3.6:F2} m³/h)",
                $"  DN máximo:      {dnMax} mm",
                $"  Perda máxima:   {perdaMax:F3} mCA",
                "──────────────────────────────────",
            };

            foreach (var r in resultados.OrderByDescending(r => r.VazaoL_s))
            {
                lines.Add($"  {r}");
            }

            lines.Add("══════════════════════════════════");

            return string.Join("\n", lines);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Tabela de diâmetros internos PVC soldável (mm).</summary>
        private static readonly Dictionary<int, double> _diametroInterno = new()
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

        private static double GetDiametroInterno(int dn)
        {
            return _diametroInterno.TryGetValue(dn, out var di) ? di : dn * 0.85;
        }

        // Exemplos:
        // var service = new PrumadaService(logService);
        //
        // var resultado = service.DimensionarPrumada(prumada);
        // resultado.DiametroNominal → 32
        // resultado.VazaoL_s       → 0.520
        // resultado.Velocidade     → 0.86 m/s
        // resultado.PerdaCarga     → 0.045 mCA
        // resultado.Aprovado       → true
        //
        // var todos = service.DimensionarTodas(prumadas);
        // Console.WriteLine(PrumadaService.GerarResumo(todos));
    }
}
