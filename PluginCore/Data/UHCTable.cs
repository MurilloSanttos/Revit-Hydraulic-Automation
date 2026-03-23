using PluginCore.Common;
using PluginCore.Domain.Enums;

namespace PluginCore.Data
{
    /// <summary>
    /// Informações de UHC de um aparelho sanitário.
    /// </summary>
    public class UHCInfo
    {
        /// <summary>Nome do aparelho.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>UHC — Unidade Hunter de Contribuição.</summary>
        public double UHC { get; set; }

        /// <summary>Diâmetro mínimo do ramal de descarga (mm).</summary>
        public int DiametroRamalMm { get; set; }

        /// <summary>Diâmetro mínimo do ramal de esgoto (mm).</summary>
        public int DiametroEsgotoMm { get; set; }

        /// <summary>Necessita ventilação individual.</summary>
        public bool VentilacaoIndividual { get; set; }

        /// <summary>Tipo de equipamento (enum).</summary>
        public EquipmentType Tipo { get; set; }

        public override string ToString()
        {
            return $"{Nome} | UHC={UHC} | Ø Ramal={DiametroRamalMm}mm | Ø Esgoto={DiametroEsgotoMm}mm";
        }
    }

    /// <summary>
    /// Tabela de UHC (Unidades Hunter de Contribuição) por aparelho sanitário.
    /// Conforme NBR 8160:1999 — Tabela 3.
    /// Utilizada no dimensionamento de ramais, sub-coletores e coletores de esgoto.
    /// </summary>
    public static class UHCTable
    {
        // ══════════════════════════════════════════════════════════
        //  TABELA NBR 8160 — Tabela 3
        // ══════════════════════════════════════════════════════════

        private static readonly Dictionary<string, UHCInfo> _tabela = new(StringComparer.OrdinalIgnoreCase)
        {
            // ── VASOS SANITÁRIOS ───────────────────────────────
            ["vaso sanitario"] = new()
            {
                Nome = "Vaso Sanitário",
                UHC = 6.0,
                DiametroRamalMm = 100,
                DiametroEsgotoMm = 100,
                VentilacaoIndividual = true,
                Tipo = EquipmentType.Toilet,
            },
            ["vaso sanitario com caixa acoplada"] = new()
            {
                Nome = "Vaso Sanitário com Caixa Acoplada",
                UHC = 6.0,
                DiametroRamalMm = 100,
                DiametroEsgotoMm = 100,
                VentilacaoIndividual = true,
                Tipo = EquipmentType.Toilet,
            },
            ["vaso sanitario com valvula de descarga"] = new()
            {
                Nome = "Vaso Sanitário com Válvula de Descarga",
                UHC = 6.0,
                DiametroRamalMm = 100,
                DiametroEsgotoMm = 100,
                VentilacaoIndividual = true,
                Tipo = EquipmentType.Toilet,
            },

            // ── LAVATÓRIOS ─────────────────────────────────────
            ["lavatorio"] = new()
            {
                Nome = "Lavatório",
                UHC = 1.0,
                DiametroRamalMm = 40,
                DiametroEsgotoMm = 40,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Sink,
            },
            ["lavatorio de coluna"] = new()
            {
                Nome = "Lavatório de Coluna",
                UHC = 1.0,
                DiametroRamalMm = 40,
                DiametroEsgotoMm = 40,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Sink,
            },

            // ── CHUVEIROS ──────────────────────────────────────
            ["chuveiro"] = new()
            {
                Nome = "Chuveiro",
                UHC = 2.0,
                DiametroRamalMm = 40,
                DiametroEsgotoMm = 40,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Shower,
            },
            ["chuveiro eletrico"] = new()
            {
                Nome = "Chuveiro Elétrico",
                UHC = 2.0,
                DiametroRamalMm = 40,
                DiametroEsgotoMm = 40,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Shower,
            },

            // ── BANHEIRA ───────────────────────────────────────
            ["banheira"] = new()
            {
                Nome = "Banheira",
                UHC = 3.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = true,
                Tipo = EquipmentType.Bathtub,
            },

            // ── BIDÊ ───────────────────────────────────────────
            ["bide"] = new()
            {
                Nome = "Bidê",
                UHC = 1.0,
                DiametroRamalMm = 40,
                DiametroEsgotoMm = 40,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Bidet,
            },

            // ── PIAS ───────────────────────────────────────────
            ["pia de cozinha"] = new()
            {
                Nome = "Pia de Cozinha",
                UHC = 3.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = true,
                Tipo = EquipmentType.KitchenSink,
            },
            ["pia de cozinha com torneira eletrica"] = new()
            {
                Nome = "Pia de Cozinha com Torneira Elétrica",
                UHC = 3.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = true,
                Tipo = EquipmentType.KitchenSink,
            },

            // ── TANQUES E LAVANDERIAS ─────────────────────────
            ["tanque"] = new()
            {
                Nome = "Tanque",
                UHC = 3.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = true,
                Tipo = EquipmentType.LaundryTub,
            },
            ["maquina de lavar roupa"] = new()
            {
                Nome = "Máquina de Lavar Roupa",
                UHC = 3.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.WashingMachine,
            },
            ["maquina de lavar"] = new()
            {
                Nome = "Máquina de Lavar",
                UHC = 3.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.WashingMachine,
            },

            // ── LAVA-LOUÇAS ───────────────────────────────────
            ["lava loucas"] = new()
            {
                Nome = "Lava-louças",
                UHC = 2.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Dishwasher,
            },
            ["maquina de lavar loucas"] = new()
            {
                Nome = "Máquina de Lavar Louças",
                UHC = 2.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Dishwasher,
            },

            // ── MICTÓRIO ──────────────────────────────────────
            ["mictorio"] = new()
            {
                Nome = "Mictório",
                UHC = 2.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Urinal,
            },
            ["mictorio com valvula"] = new()
            {
                Nome = "Mictório com Válvula",
                UHC = 2.0,
                DiametroRamalMm = 50,
                DiametroEsgotoMm = 50,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.Urinal,
            },

            // ── RALOS ─────────────────────────────────────────
            ["ralo sifonado"] = new()
            {
                Nome = "Ralo Sifonado",
                UHC = 1.0,
                DiametroRamalMm = 40,
                DiametroEsgotoMm = 40,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.FloorDrain,
            },
            ["ralo seco"] = new()
            {
                Nome = "Ralo Seco",
                UHC = 1.0,
                DiametroRamalMm = 40,
                DiametroEsgotoMm = 40,
                VentilacaoIndividual = false,
                Tipo = EquipmentType.FloorDrain,
            },
        };

        // ══════════════════════════════════════════════════════════
        //  TABELA DE DIÂMETROS POR UHC ACUMULADO (NBR 8160 Tab.4)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Diâmetro mínimo do ramal de esgoto em função do UHC acumulado.
        /// NBR 8160 — Tabela 4.
        /// </summary>
        private static readonly (double maxUHC, int diametroMm)[] _diametroPorUHC =
        {
            (3, 50),
            (6, 75),
            (20, 100),
            (160, 150),
            (620, 200),
            (1600, 250),
            (3600, 300),
        };

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS PÚBLICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o UHC de um aparelho pelo nome.
        /// </summary>
        public static double ObterUHC(string nomeAparelho)
        {
            var info = ObterInfo(nomeAparelho);
            return info?.UHC ?? 0.0;
        }

        /// <summary>
        /// Retorna informações completas de UHC de um aparelho.
        /// </summary>
        public static UHCInfo? ObterInfo(string nomeAparelho)
        {
            if (string.IsNullOrWhiteSpace(nomeAparelho))
                return null;

            var normalizado = TextNormalizer.Normalize(nomeAparelho);

            if (_tabela.TryGetValue(normalizado, out var info))
                return info;

            // Busca parcial
            var match = _tabela.FirstOrDefault(kv =>
                normalizado.Contains(kv.Key, StringComparison.OrdinalIgnoreCase) ||
                kv.Key.Contains(normalizado, StringComparison.OrdinalIgnoreCase));

            return match.Value;
        }

        /// <summary>
        /// Retorna o UHC de um aparelho pelo tipo (enum).
        /// </summary>
        public static double ObterUHC(EquipmentType tipo)
        {
            var match = _tabela.Values.FirstOrDefault(i => i.Tipo == tipo);
            return match?.UHC ?? 0.0;
        }

        /// <summary>
        /// Soma UHC de uma lista de aparelhos.
        /// </summary>
        public static double SomaUHC(IEnumerable<string> aparelhos)
        {
            if (aparelhos == null)
                return 0.0;

            return aparelhos.Sum(a => ObterUHC(a));
        }

        /// <summary>
        /// Determina diâmetro mínimo de esgoto pelo UHC acumulado.
        /// NBR 8160 — Tabela 4.
        /// </summary>
        public static int DeterminarDiametroEsgoto(double uhcAcumulado)
        {
            foreach (var (maxUHC, diametroMm) in _diametroPorUHC)
            {
                if (uhcAcumulado <= maxUHC)
                    return diametroMm;
            }

            return 300; // Maior disponível
        }

        /// <summary>
        /// Retorna o diâmetro mínimo do ramal de descarga de um aparelho.
        /// </summary>
        public static int DiametroRamal(string nomeAparelho)
        {
            return ObterInfo(nomeAparelho)?.DiametroRamalMm ?? 40;
        }

        /// <summary>
        /// Verifica se o aparelho necessita ventilação individual.
        /// </summary>
        public static bool NecessitaVentilacao(string nomeAparelho)
        {
            return ObterInfo(nomeAparelho)?.VentilacaoIndividual ?? false;
        }

        /// <summary>
        /// Verifica se existe o aparelho na tabela.
        /// </summary>
        public static bool Existe(string nomeAparelho)
        {
            return ObterInfo(nomeAparelho) != null;
        }

        /// <summary>
        /// Retorna todos os aparelhos cadastrados.
        /// </summary>
        public static IReadOnlyDictionary<string, UHCInfo> GetAll()
        {
            return _tabela;
        }

        // Exemplos:
        // UHCTable.ObterUHC("Vaso Sanitário")                   → 6.0
        // UHCTable.ObterUHC("Chuveiro")                         → 2.0
        // UHCTable.ObterUHC("Pia de Cozinha")                   → 3.0
        // UHCTable.ObterUHC(EquipmentType.Toilet)               → 6.0
        //
        // UHCTable.SomaUHC(new[] { "vaso sanitario", "lavatorio", "chuveiro" })
        // → 9.0
        //
        // UHCTable.DeterminarDiametroEsgoto(9.0)                → 100 mm
        // UHCTable.DeterminarDiametroEsgoto(25.0)               → 150 mm
        //
        // UHCTable.DiametroRamal("vaso sanitario")              → 100 mm
        // UHCTable.NecessitaVentilacao("vaso sanitario")         → true
    }
}
