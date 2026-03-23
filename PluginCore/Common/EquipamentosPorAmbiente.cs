using PluginCore.Models;

namespace PluginCore.Common
{
    /// <summary>
    /// Regra de equipamento hidráulico para um tipo de ambiente.
    /// </summary>
    public class EquipamentoRegra
    {
        /// <summary>Nome do equipamento.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Se é obrigatório no ambiente.</summary>
        public bool Obrigatorio { get; set; }

        /// <summary>Quantidade padrão esperada.</summary>
        public int QuantidadePadrao { get; set; } = 1;

        /// <summary>Família Revit sugerida (se disponível).</summary>
        public string FamiliaRevit { get; set; } = string.Empty;

        /// <summary>Diâmetro de entrada em mm (água fria).</summary>
        public int DiametroEntradaMm { get; set; }

        /// <summary>Diâmetro de saída em mm (esgoto).</summary>
        public int DiametroSaidaMm { get; set; }

        /// <summary>Peso UHC (Unidade Hunter de Contribuição).</summary>
        public double PesoUHC { get; set; }

        public override string ToString()
        {
            var tipo = Obrigatorio ? "OBR" : "OPC";
            return $"[{tipo}] {Nome} (Ø entrada:{DiametroEntradaMm}mm, " +
                   $"Ø saída:{DiametroSaidaMm}mm, UHC:{PesoUHC})";
        }
    }

    /// <summary>
    /// Tabela de equipamentos hidráulicos esperados por TipoAmbiente.
    /// Base para validação, inserção automática e dimensionamento.
    /// </summary>
    public static class EquipamentosPorAmbiente
    {
        // ══════════════════════════════════════════════════════════
        //  DICIONÁRIO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        private static readonly Dictionary<TipoAmbiente, List<EquipamentoRegra>> _tabela = new()
        {
            // ── BANHEIRO ───────────────────────────────────────
            [TipoAmbiente.Banheiro] = new()
            {
                new() { Nome = "Vaso Sanitário",  Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 100, PesoUHC = 6.0 },
                new() { Nome = "Lavatório",       Obrigatorio = true,  DiametroEntradaMm = 20, DiametroSaidaMm = 40,  PesoUHC = 1.0 },
                new() { Nome = "Chuveiro",        Obrigatorio = false, DiametroEntradaMm = 25, DiametroSaidaMm = 40,  PesoUHC = 2.0 },
                new() { Nome = "Ralo Sifonado",   Obrigatorio = true,  DiametroEntradaMm = 0,  DiametroSaidaMm = 50,  PesoUHC = 1.0 },
                new() { Nome = "Ralo Seco",       Obrigatorio = false, DiametroEntradaMm = 0,  DiametroSaidaMm = 40,  PesoUHC = 0.0 },
            },

            // ── SUÍTE ──────────────────────────────────────────
            [TipoAmbiente.Suite] = new()
            {
                new() { Nome = "Vaso Sanitário",  Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 100, PesoUHC = 6.0 },
                new() { Nome = "Lavatório",       Obrigatorio = true,  DiametroEntradaMm = 20, DiametroSaidaMm = 40,  PesoUHC = 1.0 },
                new() { Nome = "Chuveiro",        Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 40,  PesoUHC = 2.0 },
                new() { Nome = "Ralo Sifonado",   Obrigatorio = true,  DiametroEntradaMm = 0,  DiametroSaidaMm = 50,  PesoUHC = 1.0 },
                new() { Nome = "Ralo Seco",       Obrigatorio = false, DiametroEntradaMm = 0,  DiametroSaidaMm = 40,  PesoUHC = 0.0 },
                new() { Nome = "Banheira",        Obrigatorio = false, DiametroEntradaMm = 25, DiametroSaidaMm = 40,  PesoUHC = 2.0 },
                new() { Nome = "Bidê",            Obrigatorio = false, DiametroEntradaMm = 20, DiametroSaidaMm = 40,  PesoUHC = 1.0 },
            },

            // ── LAVABO ─────────────────────────────────────────
            [TipoAmbiente.Lavabo] = new()
            {
                new() { Nome = "Vaso Sanitário",  Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 100, PesoUHC = 6.0 },
                new() { Nome = "Lavatório",       Obrigatorio = true,  DiametroEntradaMm = 20, DiametroSaidaMm = 40,  PesoUHC = 1.0 },
                new() { Nome = "Ralo Seco",       Obrigatorio = false, DiametroEntradaMm = 0,  DiametroSaidaMm = 40,  PesoUHC = 0.0 },
            },

            // ── COZINHA ────────────────────────────────────────
            [TipoAmbiente.Cozinha] = new()
            {
                new() { Nome = "Pia de Cozinha",  Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 50,  PesoUHC = 3.0 },
                new() { Nome = "Ralo Sifonado",   Obrigatorio = false, DiametroEntradaMm = 0,  DiametroSaidaMm = 50,  PesoUHC = 1.0 },
                new() { Nome = "Lava-louças",     Obrigatorio = false, DiametroEntradaMm = 20, DiametroSaidaMm = 50,  PesoUHC = 2.0 },
                new() { Nome = "Filtro",          Obrigatorio = false, DiametroEntradaMm = 20, DiametroSaidaMm = 0,   PesoUHC = 0.5 },
            },

            // ── COZINHA GOURMET ────────────────────────────────
            [TipoAmbiente.CozinhaGourmet] = new()
            {
                new() { Nome = "Pia de Cozinha",  Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 50,  PesoUHC = 3.0 },
                new() { Nome = "Ralo Sifonado",   Obrigatorio = false, DiametroEntradaMm = 0,  DiametroSaidaMm = 50,  PesoUHC = 1.0 },
                new() { Nome = "Lava-louças",     Obrigatorio = false, DiametroEntradaMm = 20, DiametroSaidaMm = 50,  PesoUHC = 2.0 },
            },

            // ── LAVANDERIA / ÁREA DE SERVIÇO ───────────────────
            [TipoAmbiente.Lavanderia] = new()
            {
                new() { Nome = "Tanque",           Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 50,  PesoUHC = 3.0 },
                new() { Nome = "Máquina de Lavar", Obrigatorio = false, DiametroEntradaMm = 25, DiametroSaidaMm = 50,  PesoUHC = 3.0 },
                new() { Nome = "Ralo Sifonado",    Obrigatorio = true,  DiametroEntradaMm = 0,  DiametroSaidaMm = 50,  PesoUHC = 1.0 },
                new() { Nome = "Ralo Seco",        Obrigatorio = false, DiametroEntradaMm = 0,  DiametroSaidaMm = 40,  PesoUHC = 0.0 },
            },

            // ── ÁREA DE SERVIÇO ────────────────────────────────
            [TipoAmbiente.AreaDeServico] = new()
            {
                new() { Nome = "Tanque",           Obrigatorio = true,  DiametroEntradaMm = 25, DiametroSaidaMm = 50,  PesoUHC = 3.0 },
                new() { Nome = "Máquina de Lavar", Obrigatorio = false, DiametroEntradaMm = 25, DiametroSaidaMm = 50,  PesoUHC = 3.0 },
                new() { Nome = "Ralo Sifonado",    Obrigatorio = true,  DiametroEntradaMm = 0,  DiametroSaidaMm = 50,  PesoUHC = 1.0 },
            },

            // ── ÁREA EXTERNA ───────────────────────────────────
            [TipoAmbiente.AreaExterna] = new()
            {
                new() { Nome = "Torneira de Jardim", Obrigatorio = false, DiametroEntradaMm = 20, DiametroSaidaMm = 0,  PesoUHC = 0.5 },
                new() { Nome = "Ralo Sifonado",      Obrigatorio = false, DiametroEntradaMm = 0,  DiametroSaidaMm = 50, PesoUHC = 1.0 },
            },

            // ── NÃO IDENTIFICADO ───────────────────────────────
            [TipoAmbiente.NaoIdentificado] = new(),
        };

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS PÚBLICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Retorna todos os equipamentos de um tipo de ambiente.</summary>
        public static IReadOnlyList<EquipamentoRegra> Get(TipoAmbiente tipo)
        {
            return _tabela.TryGetValue(tipo, out var lista)
                ? lista.AsReadOnly()
                : new List<EquipamentoRegra>().AsReadOnly();
        }

        /// <summary>Retorna apenas equipamentos obrigatórios.</summary>
        public static IEnumerable<EquipamentoRegra> GetObrigatorios(TipoAmbiente tipo)
        {
            return Get(tipo).Where(e => e.Obrigatorio);
        }

        /// <summary>Retorna apenas equipamentos opcionais.</summary>
        public static IEnumerable<EquipamentoRegra> GetOpcionais(TipoAmbiente tipo)
        {
            return Get(tipo).Where(e => !e.Obrigatorio);
        }

        /// <summary>Verifica se o tipo possui equipamentos definidos.</summary>
        public static bool TemEquipamentos(TipoAmbiente tipo)
        {
            return Get(tipo).Count > 0;
        }

        /// <summary>Retorna a soma de UHC de todos os equipamentos obrigatórios.</summary>
        public static double SomaUHCObrigatorios(TipoAmbiente tipo)
        {
            return GetObrigatorios(tipo).Sum(e => e.PesoUHC);
        }

        /// <summary>Retorna o dicionário completo.</summary>
        public static IReadOnlyDictionary<TipoAmbiente, List<EquipamentoRegra>> Tabela => _tabela;

        /// <summary>Retorna todos os tipos que possuem equipamentos.</summary>
        public static IEnumerable<TipoAmbiente> TiposComEquipamentos()
        {
            return _tabela
                .Where(kv => kv.Value.Count > 0)
                .Select(kv => kv.Key);
        }

        // Exemplos:
        // var equips = EquipamentosPorAmbiente.Get(TipoAmbiente.Banheiro);
        // → [Vaso Sanitário, Lavatório, Chuveiro, Ralo Sifonado, Ralo Seco]
        //
        // var obrig = EquipamentosPorAmbiente.GetObrigatorios(TipoAmbiente.Cozinha);
        // → [Pia de Cozinha]
        //
        // var uhc = EquipamentosPorAmbiente.SomaUHCObrigatorios(TipoAmbiente.Suite);
        // → 10.0
    }
}
