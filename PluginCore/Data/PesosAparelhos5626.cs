using PluginCore.Common;

namespace PluginCore.Data
{
    /// <summary>
    /// Tabela de pesos relativos de aparelhos sanitários conforme NBR 5626:2020.
    /// Utilizada para cálculo de vazão provável em instalações de água fria.
    /// Referência: ABNT NBR 5626 — Tabela A.1.
    /// </summary>
    public static class PesosAparelhos5626
    {
        // ══════════════════════════════════════════════════════════
        //  TABELA DE PESOS (NBR 5626 — Tabela A.1)
        // ══════════════════════════════════════════════════════════

        private static readonly Dictionary<string, AparelhoInfo> _tabela = new(StringComparer.OrdinalIgnoreCase)
        {
            // ── VASOS SANITÁRIOS ───────────────────────────────
            ["vaso sanitario com caixa acoplada"] = new()
            {
                Nome = "Vaso Sanitário com Caixa Acoplada",
                Peso = 0.3,
                VazaoMinL_s = 0.15,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.Esgoto,
            },
            ["vaso sanitario com valvula de descarga"] = new()
            {
                Nome = "Vaso Sanitário com Válvula de Descarga",
                Peso = 1.7,
                VazaoMinL_s = 1.70,
                DiametroMinimoMm = 50,
                Categoria = CategoriaAparelho.Esgoto,
            },
            ["vaso sanitario com caixa suspensa"] = new()
            {
                Nome = "Vaso Sanitário com Caixa Suspensa",
                Peso = 0.3,
                VazaoMinL_s = 0.15,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.Esgoto,
            },

            // ── LAVATÓRIOS ─────────────────────────────────────
            ["lavatorio"] = new()
            {
                Nome = "Lavatório",
                Peso = 0.3,
                VazaoMinL_s = 0.15,
                DiametroMinimoMm = 15,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["lavatorio de coluna"] = new()
            {
                Nome = "Lavatório de Coluna",
                Peso = 0.3,
                VazaoMinL_s = 0.15,
                DiametroMinimoMm = 15,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── CHUVEIROS ──────────────────────────────────────
            ["chuveiro"] = new()
            {
                Nome = "Chuveiro",
                Peso = 0.4,
                VazaoMinL_s = 0.20,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["chuveiro eletrico"] = new()
            {
                Nome = "Chuveiro Elétrico",
                Peso = 0.4,
                VazaoMinL_s = 0.10,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["ducha higienica"] = new()
            {
                Nome = "Ducha Higiênica",
                Peso = 0.1,
                VazaoMinL_s = 0.05,
                DiametroMinimoMm = 15,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── PIAS ───────────────────────────────────────────
            ["pia de cozinha"] = new()
            {
                Nome = "Pia de Cozinha",
                Peso = 0.7,
                VazaoMinL_s = 0.25,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["pia de cozinha com torneira eletrica"] = new()
            {
                Nome = "Pia de Cozinha com Torneira Elétrica",
                Peso = 0.7,
                VazaoMinL_s = 0.10,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── TANQUES E LAVANDERIAS ─────────────────────────
            ["tanque"] = new()
            {
                Nome = "Tanque",
                Peso = 0.7,
                VazaoMinL_s = 0.25,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["maquina de lavar roupa"] = new()
            {
                Nome = "Máquina de Lavar Roupa",
                Peso = 1.0,
                VazaoMinL_s = 0.30,
                DiametroMinimoMm = 25,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["maquina de lavar"] = new()
            {
                Nome = "Máquina de Lavar",
                Peso = 1.0,
                VazaoMinL_s = 0.30,
                DiametroMinimoMm = 25,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── LAVA-LOUÇAS ───────────────────────────────────
            ["lava loucas"] = new()
            {
                Nome = "Lava-louças",
                Peso = 1.0,
                VazaoMinL_s = 0.30,
                DiametroMinimoMm = 25,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["maquina de lavar loucas"] = new()
            {
                Nome = "Máquina de Lavar Louças",
                Peso = 1.0,
                VazaoMinL_s = 0.30,
                DiametroMinimoMm = 25,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── BIDÊ ───────────────────────────────────────────
            ["bide"] = new()
            {
                Nome = "Bidê",
                Peso = 0.3,
                VazaoMinL_s = 0.10,
                DiametroMinimoMm = 15,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── BANHEIRA ──────────────────────────────────────
            ["banheira"] = new()
            {
                Nome = "Banheira",
                Peso = 1.0,
                VazaoMinL_s = 0.30,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── MICTÓRIO ──────────────────────────────────────
            ["mictorio"] = new()
            {
                Nome = "Mictório",
                Peso = 0.5,
                VazaoMinL_s = 0.15,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["mictorio com valvula"] = new()
            {
                Nome = "Mictório com Válvula",
                Peso = 0.5,
                VazaoMinL_s = 0.50,
                DiametroMinimoMm = 32,
                Categoria = CategoriaAparelho.Esgoto,
            },

            // ── TORNEIRAS ─────────────────────────────────────
            ["torneira"] = new()
            {
                Nome = "Torneira",
                Peso = 0.2,
                VazaoMinL_s = 0.10,
                DiametroMinimoMm = 15,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["torneira de jardim"] = new()
            {
                Nome = "Torneira de Jardim",
                Peso = 0.2,
                VazaoMinL_s = 0.10,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },
            ["torneira de tanque"] = new()
            {
                Nome = "Torneira de Tanque",
                Peso = 0.7,
                VazaoMinL_s = 0.25,
                DiametroMinimoMm = 20,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── FILTRO ────────────────────────────────────────
            ["filtro"] = new()
            {
                Nome = "Filtro",
                Peso = 0.1,
                VazaoMinL_s = 0.05,
                DiametroMinimoMm = 15,
                Categoria = CategoriaAparelho.AguaFria,
            },

            // ── RALOS ─────────────────────────────────────────
            ["ralo sifonado"] = new()
            {
                Nome = "Ralo Sifonado",
                Peso = 0.0,
                VazaoMinL_s = 0.00,
                DiametroMinimoMm = 0,
                Categoria = CategoriaAparelho.Esgoto,
            },
            ["ralo seco"] = new()
            {
                Nome = "Ralo Seco",
                Peso = 0.0,
                VazaoMinL_s = 0.00,
                DiametroMinimoMm = 0,
                Categoria = CategoriaAparelho.Esgoto,
            },
        };

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS PÚBLICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o peso relativo de um aparelho.
        /// Normaliza o nome antes da busca.
        /// </summary>
        public static double GetPeso(string nomeAparelho)
        {
            var info = GetInfo(nomeAparelho);
            return info?.Peso ?? 0.0;
        }

        /// <summary>
        /// Retorna informações completas de um aparelho.
        /// </summary>
        public static AparelhoInfo? GetInfo(string nomeAparelho)
        {
            if (string.IsNullOrWhiteSpace(nomeAparelho))
                return null;

            var normalizado = TextNormalizer.Normalize(nomeAparelho);

            if (_tabela.TryGetValue(normalizado, out var info))
                return info;

            // Tentar busca parcial
            var match = _tabela.FirstOrDefault(kv =>
                normalizado.Contains(kv.Key, StringComparison.OrdinalIgnoreCase) ||
                kv.Key.Contains(normalizado, StringComparison.OrdinalIgnoreCase));

            return match.Value;
        }

        /// <summary>
        /// Retorna todos os aparelhos cadastrados.
        /// </summary>
        public static IReadOnlyDictionary<string, AparelhoInfo> GetAll()
        {
            return _tabela;
        }

        /// <summary>
        /// Retorna todos os aparelhos como dicionário nome→peso.
        /// </summary>
        public static Dictionary<string, double> GetAllPesos()
        {
            return _tabela.ToDictionary(kv => kv.Value.Nome, kv => kv.Value.Peso);
        }

        /// <summary>
        /// Verifica se existe o aparelho na tabela.
        /// </summary>
        public static bool Existe(string nomeAparelho)
        {
            return GetInfo(nomeAparelho) != null;
        }

        /// <summary>
        /// Calcula soma de pesos para uma lista de aparelhos.
        /// </summary>
        public static double SomaPesos(IEnumerable<string> aparelhos)
        {
            return aparelhos.Sum(a => GetPeso(a));
        }

        /// <summary>
        /// Retorna o diâmetro mínimo recomendado para um aparelho.
        /// </summary>
        public static int GetDiametroMinimo(string nomeAparelho)
        {
            return GetInfo(nomeAparelho)?.DiametroMinimoMm ?? 0;
        }

        // Exemplos:
        // PesosAparelhos5626.GetPeso("Vaso Sanitário com Caixa Acoplada") → 0.3
        // PesosAparelhos5626.GetPeso("chuveiro")                          → 0.4
        // PesosAparelhos5626.GetPeso("pia de cozinha")                    → 0.7
        // PesosAparelhos5626.GetPeso("desconhecido")                      → 0.0
        //
        // PesosAparelhos5626.SomaPesos(new[] { "lavatorio", "chuveiro", "vaso sanitario com caixa acoplada" })
        // → 1.0
        //
        // var info = PesosAparelhos5626.GetInfo("chuveiro");
        // info.Peso             → 0.4
        // info.VazaoMinL_s      → 0.20
        // info.DiametroMinimoMm → 20
    }

    /// <summary>
    /// Categoria do aparelho sanitário.
    /// </summary>
    public enum CategoriaAparelho
    {
        AguaFria,
        AguaQuente,
        Esgoto
    }

    /// <summary>
    /// Informações completas de um aparelho da NBR 5626.
    /// </summary>
    public class AparelhoInfo
    {
        /// <summary>Nome oficial do aparelho.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Peso relativo (adimensional).</summary>
        public double Peso { get; set; }

        /// <summary>Vazão mínima em litros/segundo.</summary>
        public double VazaoMinL_s { get; set; }

        /// <summary>Diâmetro mínimo do ramal em mm.</summary>
        public int DiametroMinimoMm { get; set; }

        /// <summary>Categoria do aparelho.</summary>
        public CategoriaAparelho Categoria { get; set; }

        public override string ToString()
        {
            return $"{Nome} | Peso: {Peso} | Q: {VazaoMinL_s:F2} L/s | Ø{DiametroMinimoMm}mm";
        }
    }
}
