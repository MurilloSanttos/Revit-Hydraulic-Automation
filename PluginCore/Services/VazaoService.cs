using PluginCore.Data;

namespace PluginCore.Services
{
    /// <summary>
    /// Serviço de cálculo de vazão provável conforme NBR 5626:2020.
    /// Fórmula: Q = 0.3 × √(ΣP)
    /// Onde: Q = vazão provável (L/s), P = peso relativo dos aparelhos.
    /// </summary>
    public static class VazaoService
    {
        /// <summary>Coeficiente da fórmula NBR 5626.</summary>
        private const double COEFICIENTE = 0.3;

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula a vazão provável a partir de uma lista de nomes de aparelhos.
        /// Q = 0.3 × √(ΣP)
        /// </summary>
        /// <param name="aparelhos">Nomes dos aparelhos sanitários.</param>
        /// <returns>Vazão provável em L/s.</returns>
        public static double CalcularVazaoProvavel(IEnumerable<string> aparelhos)
        {
            if (aparelhos == null)
                return 0.0;

            var somaPesos = aparelhos.Sum(a => PesosAparelhos5626.GetPeso(a));

            return CalcularVazaoPorPeso(somaPesos);
        }

        /// <summary>
        /// Calcula a vazão provável a partir da soma de pesos já calculada.
        /// Q = 0.3 × √(ΣP)
        /// </summary>
        /// <param name="somaPesos">Soma dos pesos relativos.</param>
        /// <returns>Vazão provável em L/s.</returns>
        public static double CalcularVazaoPorPeso(double somaPesos)
        {
            if (somaPesos <= 0)
                return 0.0;

            return COEFICIENTE * Math.Sqrt(somaPesos);
        }

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO DETALHADO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula vazão com resultado detalhado.
        /// </summary>
        public static VazaoResult CalcularDetalhado(IEnumerable<string> aparelhos)
        {
            if (aparelhos == null)
                return VazaoResult.Vazio;

            var lista = aparelhos.ToList();

            if (lista.Count == 0)
                return VazaoResult.Vazio;

            var itens = lista
                .Select(a => new VazaoItem
                {
                    Aparelho = a,
                    Peso = PesosAparelhos5626.GetPeso(a),
                    VazaoMinima = PesosAparelhos5626.GetInfo(a)?.VazaoMinL_s ?? 0,
                    DiametroMinimo = PesosAparelhos5626.GetDiametroMinimo(a),
                    Reconhecido = PesosAparelhos5626.Existe(a),
                })
                .ToList();

            var somaPesos = itens.Sum(i => i.Peso);
            var vazaoProvavel = CalcularVazaoPorPeso(somaPesos);
            var vazaoMinima = itens.Sum(i => i.VazaoMinima);

            return new VazaoResult
            {
                Itens = itens,
                TotalAparelhos = lista.Count,
                AparelhosReconhecidos = itens.Count(i => i.Reconhecido),
                SomaPesos = somaPesos,
                VazaoProvavelL_s = vazaoProvavel,
                VazaoProvavelM3_h = vazaoProvavel * 3.6,
                VazaoMinimaL_s = vazaoMinima,
                DiametroMaximoMm = itens.Max(i => i.DiametroMinimo),
            };
        }

        /// <summary>
        /// Calcula vazão para um ambiente completo.
        /// </summary>
        public static VazaoResult CalcularPorAmbiente(Models.AmbienteInfo ambiente)
        {
            if (ambiente == null || !ambiente.EhRelevante)
                return VazaoResult.Vazio;

            var equipamentos = Common.EquipamentosPorAmbiente
                .GetObrigatorios(ambiente.Classificacao.Tipo)
                .Select(e => e.Nome);

            return CalcularDetalhado(equipamentos);
        }

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula vazão total de múltiplos ambientes.
        /// </summary>
        public static VazaoResumo CalcularTotal(IEnumerable<Models.AmbienteInfo> ambientes)
        {
            if (ambientes == null)
                return new VazaoResumo();

            var resultados = ambientes
                .Where(a => a != null && a.EhRelevante)
                .Select(a => (Ambiente: a, Vazao: CalcularPorAmbiente(a)))
                .ToList();

            var somaPesosTotal = resultados.Sum(r => r.Vazao.SomaPesos);
            var vazaoTotal = CalcularVazaoPorPeso(somaPesosTotal);

            return new VazaoResumo
            {
                TotalAmbientes = resultados.Count,
                TotalAparelhos = resultados.Sum(r => r.Vazao.TotalAparelhos),
                SomaPesosTotal = somaPesosTotal,
                VazaoTotalL_s = vazaoTotal,
                VazaoTotalM3_h = vazaoTotal * 3.6,
                PorAmbiente = resultados
                    .ToDictionary(
                        r => $"{r.Ambiente.NomeOriginal} (#{r.Ambiente.Numero})",
                        r => r.Vazao),
            };
        }

        // ══════════════════════════════════════════════════════════
        //  MODELOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Item individual de cálculo de vazão.</summary>
        public class VazaoItem
        {
            public string Aparelho { get; set; } = string.Empty;
            public double Peso { get; set; }
            public double VazaoMinima { get; set; }
            public int DiametroMinimo { get; set; }
            public bool Reconhecido { get; set; }
        }

        /// <summary>Resultado detalhado de cálculo de vazão.</summary>
        public class VazaoResult
        {
            public List<VazaoItem> Itens { get; set; } = new();
            public int TotalAparelhos { get; set; }
            public int AparelhosReconhecidos { get; set; }
            public double SomaPesos { get; set; }
            public double VazaoProvavelL_s { get; set; }
            public double VazaoProvavelM3_h { get; set; }
            public double VazaoMinimaL_s { get; set; }
            public int DiametroMaximoMm { get; set; }

            public static readonly VazaoResult Vazio = new();

            public override string ToString()
            {
                return $"ΣP={SomaPesos:F1} → Q={VazaoProvavelL_s:F3} L/s " +
                       $"({VazaoProvavelM3_h:F2} m³/h) | " +
                       $"{TotalAparelhos} aparelhos, Ø max={DiametroMaximoMm}mm";
            }
        }

        /// <summary>Resumo de cálculo em lote.</summary>
        public class VazaoResumo
        {
            public int TotalAmbientes { get; set; }
            public int TotalAparelhos { get; set; }
            public double SomaPesosTotal { get; set; }
            public double VazaoTotalL_s { get; set; }
            public double VazaoTotalM3_h { get; set; }
            public Dictionary<string, VazaoResult> PorAmbiente { get; set; } = new();

            public override string ToString()
            {
                return $"══ Vazão Total ══\n" +
                       $"  Ambientes:  {TotalAmbientes}\n" +
                       $"  Aparelhos:  {TotalAparelhos}\n" +
                       $"  ΣP total:   {SomaPesosTotal:F1}\n" +
                       $"  Q total:    {VazaoTotalL_s:F3} L/s\n" +
                       $"             ({VazaoTotalM3_h:F2} m³/h)\n" +
                       $"═════════════════";
            }
        }

        // Exemplos:
        // VazaoService.CalcularVazaoProvavel(new[] { "lavatorio", "chuveiro", "vaso sanitario com caixa acoplada" })
        // ΣP = 0.3 + 0.4 + 0.3 = 1.0
        // Q  = 0.3 × √1.0 = 0.300 L/s
        //
        // var r = VazaoService.CalcularDetalhado(aparelhos);
        // r.SomaPesos          → 1.0
        // r.VazaoProvavelL_s   → 0.300
        // r.VazaoProvavelM3_h  → 1.08
        //
        // var total = VazaoService.CalcularTotal(ambientes);
        // total.VazaoTotalL_s  → 2.450
    }
}
