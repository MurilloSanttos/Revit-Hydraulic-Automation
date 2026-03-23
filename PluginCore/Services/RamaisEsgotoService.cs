using PluginCore.Data;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado do dimensionamento de um ramal de esgoto.
    /// </summary>
    public class ResultadoRamalEsgoto
    {
        /// <summary>ID do ramal.</summary>
        public string RamalId { get; set; } = string.Empty;

        /// <summary>UHC dos aparelhos diretos.</summary>
        public double UHCDireto { get; set; }

        /// <summary>UHC dos sub-ramais de descarga contribuintes.</summary>
        public double UHCContribuicao { get; set; }

        /// <summary>UHC total acumulado.</summary>
        public double UHCTotal => UHCDireto + UHCContribuicao;

        /// <summary>Diâmetro nominal dimensionado (mm).</summary>
        public int DiametroNominal { get; set; }

        /// <summary>Declividade aplicada (%).</summary>
        public double Declividade { get; set; }

        /// <summary>Vazão estimada (L/s).</summary>
        public double VazaoL_s { get; set; }

        /// <summary>Velocidade estimada (m/s).</summary>
        public double Velocidade { get; set; }

        /// <summary>Quantidade de ramais de descarga contribuintes.</summary>
        public int RamaisContribuintes { get; set; }

        /// <summary>Necessita ventilação.</summary>
        public bool NecessitaVentilacao { get; set; }

        /// <summary>Aprovado pela validação.</summary>
        public bool Aprovado { get; set; }

        /// <summary>Alertas gerados.</summary>
        public List<string> Alertas { get; set; } = new();

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            return $"{status} Esgoto '{RamalId}': DN{DiametroNominal} | " +
                   $"UHC={UHCTotal:F0} ({UHCDireto:F0}+{UHCContribuicao:F0}) | " +
                   $"Dec={Declividade}% | V={Velocidade:F2} m/s";
        }
    }

    /// <summary>
    /// Serviço de dimensionamento de ramais de esgoto.
    /// Agrupa ramais de descarga e dimensiona conforme NBR 8160.
    /// </summary>
    public class RamaisEsgotoService
    {
        private readonly ILogService? _log;

        private const string ETAPA = "07_Esgoto";
        private const string COMPONENTE = "RamaisEsgoto";

        // ══════════════════════════════════════════════════════════
        //  CONSTANTES NBR 8160
        // ══════════════════════════════════════════════════════════

        /// <summary>Velocidade mínima — autolimpeza (m/s).</summary>
        private const double VELOCIDADE_MINIMA = 0.6;

        /// <summary>Velocidade máxima (m/s).</summary>
        private const double VELOCIDADE_MAXIMA = 4.0;

        /// <summary>Declividades mínimas por DN (%).</summary>
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

        /// <summary>Manning PVC liso.</summary>
        private const double MANNING_N = 0.010;

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public RamaisEsgotoService() { }

        public RamaisEsgotoService(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona um ramal de esgoto considerando aparelhos diretos
        /// e contribuições de sub-ramais de descarga.
        /// </summary>
        /// <param name="aparelhosDiretos">Aparelhos conectados diretamente.</param>
        /// <param name="subRamais">Resultados de ramais de descarga contribuintes.</param>
        /// <param name="ramalId">ID do ramal.</param>
        public ResultadoRamalEsgoto DimensionarRamalEsgoto(
            IEnumerable<string>? aparelhosDiretos,
            IEnumerable<ResultadoRamal>? subRamais = null,
            string ramalId = "")
        {
            var resultado = new ResultadoRamalEsgoto { RamalId = ramalId };

            // ── 1. UHC direto ─────────────────────────────────
            var listaDiretos = aparelhosDiretos?.ToList() ?? new();
            resultado.UHCDireto = UHCTable.SomaUHC(listaDiretos);

            // ── 2. UHC contribuição dos sub-ramais ────────────
            var listaSubRamais = subRamais?.ToList() ?? new();
            resultado.UHCContribuicao = listaSubRamais.Sum(r => r.UHCTotal);
            resultado.RamaisContribuintes = listaSubRamais.Count;

            var uhcTotal = resultado.UHCTotal;

            if (uhcTotal <= 0)
            {
                resultado.DiametroNominal = DIAMETROS_ESGOTO[0];
                resultado.Aprovado = true;
                return resultado;
            }

            // ── 3. Diâmetro pelo UHC acumulado ────────────────
            var dn = UHCTable.DeterminarDiametroEsgoto(uhcTotal);

            // Diâmetro nunca menor que o maior sub-ramal
            if (listaSubRamais.Count > 0)
            {
                var dnMaxSub = listaSubRamais.Max(r => r.DiametroNominal);
                if (dnMaxSub > dn)
                    dn = dnMaxSub;
            }

            // Verificar diâmetro mínimo por aparelho
            foreach (var aparelho in listaDiretos)
            {
                var dnRamal = UHCTable.DiametroRamal(aparelho);
                if (dnRamal > dn)
                    dn = dnRamal;

                if (UHCTable.NecessitaVentilacao(aparelho))
                    resultado.NecessitaVentilacao = true;
            }

            // Ventilação dos sub-ramais
            if (listaSubRamais.Any(r => r.NecessitaVentilacao))
                resultado.NecessitaVentilacao = true;

            resultado.DiametroNominal = dn;

            // ── 4. Declividade ────────────────────────────────
            resultado.Declividade = GetDeclividade(dn);

            // ── 5. Vazão (Manning) ────────────────────────────
            var vazao = EstimarVazaoManning(dn, resultado.Declividade);
            resultado.VazaoL_s = vazao;

            // ── 6. Velocidade ─────────────────────────────────
            var velocidade = CalcularVelocidade(vazao, dn);
            resultado.Velocidade = velocidade;

            // ── 7. Validação ──────────────────────────────────
            resultado.Aprovado = true;

            if (velocidade > VELOCIDADE_MAXIMA)
            {
                resultado.Aprovado = false;
                resultado.Alertas.Add(
                    $"Velocidade excedida: {velocidade:F2} > {VELOCIDADE_MAXIMA} m/s.");
                _log?.Critico(ETAPA, COMPONENTE,
                    $"Ramal '{ramalId}': V={velocidade:F2} m/s excede limite " +
                    $"(DN{dn}, UHC={uhcTotal:F0}).");
            }
            else if (velocidade > 0 && velocidade < VELOCIDADE_MINIMA)
            {
                resultado.Alertas.Add(
                    $"Velocidade baixa: {velocidade:F2} < {VELOCIDADE_MINIMA} m/s. " +
                    $"Risco de sedimentação.");
                _log?.Leve(ETAPA, COMPONENTE,
                    $"Ramal '{ramalId}': V={velocidade:F2} m/s abaixo da autolimpeza.");
            }

            _log?.Info(ETAPA, COMPONENTE,
                $"Ramal esgoto '{ramalId}': DN{dn}, UHC={uhcTotal:F0} " +
                $"({resultado.UHCDireto:F0}+{resultado.UHCContribuicao:F0}), " +
                $"Dec={resultado.Declividade}%, V={velocidade:F2} m/s, " +
                $"{listaSubRamais.Count} sub-ramais. " +
                $"{(resultado.Aprovado ? "✅" : "❌")}");

            return resultado;
        }

        /// <summary>
        /// Dimensiona e atualiza um TrechoTubulacao de esgoto.
        /// </summary>
        public ResultadoRamalEsgoto DimensionarTrecho(TrechoTubulacao trecho,
            IEnumerable<string>? aparelhos = null,
            IEnumerable<ResultadoRamal>? subRamais = null)
        {
            if (trecho == null)
                return new ResultadoRamalEsgoto();

            var resultado = DimensionarRamalEsgoto(aparelhos, subRamais, trecho.Id);

            trecho.DiametroNominal = resultado.DiametroNominal;
            trecho.DiametroInterno = resultado.DiametroNominal * 0.92;
            trecho.Vazao = resultado.VazaoL_s;
            trecho.Velocidade = resultado.Velocidade;
            trecho.Declividade = resultado.Declividade;
            trecho.SomaPesos = resultado.UHCTotal;

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO DE COLETOR / SUB-COLETOR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dimensiona coletor ou sub-coletor a partir de múltiplos ramais de esgoto.
        /// </summary>
        public ResultadoRamalEsgoto DimensionarColetor(
            IEnumerable<ResultadoRamalEsgoto> ramaisEsgoto, string coletorId = "")
        {
            var resultado = new ResultadoRamalEsgoto { RamalId = coletorId };

            var lista = ramaisEsgoto?.ToList() ?? new();

            if (lista.Count == 0)
            {
                resultado.Aprovado = true;
                return resultado;
            }

            var uhcTotal = lista.Sum(r => r.UHCTotal);
            resultado.UHCContribuicao = uhcTotal;
            resultado.RamaisContribuintes = lista.Count;

            // Diâmetro
            var dn = UHCTable.DeterminarDiametroEsgoto(uhcTotal);

            // Nunca menor que o maior ramal
            var dnMax = lista.Max(r => r.DiametroNominal);
            if (dnMax > dn) dn = dnMax;

            resultado.DiametroNominal = dn;
            resultado.Declividade = GetDeclividade(dn);
            resultado.VazaoL_s = EstimarVazaoManning(dn, resultado.Declividade);
            resultado.Velocidade = CalcularVelocidade(resultado.VazaoL_s, dn);
            resultado.NecessitaVentilacao = lista.Any(r => r.NecessitaVentilacao);

            // Validação
            resultado.Aprovado = resultado.Velocidade <= VELOCIDADE_MAXIMA;

            if (!resultado.Aprovado)
            {
                resultado.Alertas.Add($"Velocidade: {resultado.Velocidade:F2} > {VELOCIDADE_MAXIMA} m/s.");
                _log?.Critico(ETAPA, COMPONENTE,
                    $"Coletor '{coletorId}': V={resultado.Velocidade:F2} m/s, DN{dn}, UHC={uhcTotal:F0}.");
            }

            _log?.Info(ETAPA, COMPONENTE,
                $"Coletor '{coletorId}': DN{dn}, UHC={uhcTotal:F0}, " +
                $"Dec={resultado.Declividade}%, {lista.Count} ramais. " +
                $"{(resultado.Aprovado ? "✅" : "❌")}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual.</summary>
        public static string GerarResumo(List<ResultadoRamalEsgoto> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhum ramal de esgoto dimensionado.";

            var aprovados = resultados.Count(r => r.Aprovado);
            var uhcTotal = resultados.Sum(r => r.UHCTotal);
            var dnMax = resultados.Max(r => r.DiametroNominal);

            var lines = new List<string>
            {
                "══ Ramais de Esgoto (NBR 8160) ══",
                $"  Total:          {resultados.Count}",
                $"  Aprovados:      {aprovados}",
                $"  Reprovados:     {resultados.Count - aprovados}",
                $"  UHC acumulado:  {uhcTotal:F0}",
                $"  DN máximo:      {dnMax} mm",
                "──────────────────────────────────",
            };

            foreach (var r in resultados.OrderByDescending(r => r.UHCTotal))
            {
                lines.Add($"  {r}");
            }

            lines.Add("══════════════════════════════════");

            return string.Join("\n", lines);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private static double GetDeclividade(int dn)
        {
            return _declividades.TryGetValue(dn, out var dec) ? dec : 1.0;
        }

        /// <summary>
        /// Manning — meia seção, PVC (n=0.010).
        /// Q = (1/n) × A × R^(2/3) × i^(1/2)
        /// </summary>
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

            return q * 1000.0; // m³/s → L/s
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
        // var service = new RamaisEsgotoService(logService);
        //
        // // Ramal com aparelhos + sub-ramais
        // var r = service.DimensionarRamalEsgoto(
        //     aparelhosDiretos: new[] { "lavatorio" },
        //     subRamais: new[] { resultadoRamalDescarga },
        //     ramalId: "RE-01");
        //
        // // Coletor unindo múltiplos ramais
        // var coletor = service.DimensionarColetor(ramaisEsgoto, "COL-01");
    }
}
