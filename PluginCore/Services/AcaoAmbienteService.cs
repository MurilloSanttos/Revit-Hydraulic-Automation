using PluginCore.Common;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Tipo de ação a ser executada no ambiente.
    /// </summary>
    public enum TipoAcao
    {
        Inserir,
        Validar,
        Ajustar,
        Remover,
        Verificar
    }

    /// <summary>
    /// Prioridade da ação.
    /// </summary>
    public enum PrioridadeAcao
    {
        Alta,
        Media,
        Baixa
    }

    /// <summary>
    /// Ação a ser executada em um ambiente.
    /// </summary>
    public class AcaoAmbiente
    {
        /// <summary>Nome do ambiente.</summary>
        public string AmbienteNome { get; set; } = string.Empty;

        /// <summary>Número do ambiente.</summary>
        public string AmbienteNumero { get; set; } = string.Empty;

        /// <summary>ElementId do ambiente no Revit.</summary>
        public long ElementId { get; set; }

        /// <summary>Tipo de ação (Inserir, Validar, Ajustar).</summary>
        public TipoAcao Tipo { get; set; }

        /// <summary>Prioridade da ação.</summary>
        public PrioridadeAcao Prioridade { get; set; }

        /// <summary>Descrição da ação.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Nome do equipamento ou ponto relacionado.</summary>
        public string Equipamento { get; set; } = string.Empty;

        /// <summary>Categoria (Equipamento, PontoHidraulico, Validacao).</summary>
        public string Categoria { get; set; } = string.Empty;

        public override string ToString()
        {
            var icon = Tipo switch
            {
                TipoAcao.Inserir => "➕",
                TipoAcao.Validar => "✅",
                TipoAcao.Ajustar => "🔧",
                TipoAcao.Remover => "❌",
                TipoAcao.Verificar => "🔍",
                _ => "•"
            };
            return $"{icon} [{Tipo}] {AmbienteNome} → {Descricao}";
        }
    }

    /// <summary>
    /// Serviço que gera lista de ações automáticas por ambiente.
    /// Compara estado atual vs esperado e produz ações classificadas.
    /// </summary>
    public class AcaoAmbienteService
    {
        private readonly PontosHidraulicosService _pontosService;

        public AcaoAmbienteService()
        {
            _pontosService = new PontosHidraulicosService();
        }

        public AcaoAmbienteService(PontosHidraulicosService pontosService)
        {
            _pontosService = pontosService;
        }

        // ══════════════════════════════════════════════════════════
        //  GERAÇÃO DE AÇÕES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera lista de ações para todos os ambientes.
        /// </summary>
        public IEnumerable<AcaoAmbiente> GerarAcoes(IEnumerable<AmbienteInfo> ambientes)
        {
            if (ambientes == null)
                yield break;

            foreach (var ambiente in ambientes)
            {
                if (ambiente == null || !ambiente.EhRelevante)
                    continue;

                foreach (var acao in GerarAcoesAmbiente(ambiente))
                {
                    yield return acao;
                }
            }
        }

        /// <summary>
        /// Gera ações para um único ambiente.
        /// </summary>
        public List<AcaoAmbiente> GerarAcoesAmbiente(AmbienteInfo ambiente)
        {
            var acoes = new List<AcaoAmbiente>();

            if (ambiente == null || !ambiente.EhRelevante)
                return acoes;

            var esperados = EquipamentosPorAmbiente.Get(ambiente.Classificacao.Tipo);

            if (esperados.Count == 0)
                return acoes;

            var existentesNorm = ambiente.EquipamentosExistentes
                .Select(e => e.FamilyName.Trim().ToLowerInvariant())
                .ToHashSet();

            var nomesEsperadosNorm = esperados
                .Select(e => e.Nome.Trim().ToLowerInvariant())
                .ToHashSet();

            // ── Equipamentos esperados ────────────────────────
            foreach (var equip in esperados)
            {
                var nomeNorm = equip.Nome.Trim().ToLowerInvariant();

                if (existentesNorm.Contains(nomeNorm))
                {
                    // Presente → Validar
                    acoes.Add(CriarAcao(ambiente, TipoAcao.Validar,
                        PrioridadeAcao.Baixa,
                        $"Validar equipamento existente: {equip.Nome}",
                        equip.Nome, "Equipamento"));
                }
                else if (equip.Obrigatorio)
                {
                    // Obrigatório ausente → Inserir
                    acoes.Add(CriarAcao(ambiente, TipoAcao.Inserir,
                        PrioridadeAcao.Alta,
                        $"Inserir equipamento obrigatório: {equip.Nome}",
                        equip.Nome, "Equipamento"));
                }
                else
                {
                    // Opcional ausente → Verificar
                    acoes.Add(CriarAcao(ambiente, TipoAcao.Verificar,
                        PrioridadeAcao.Baixa,
                        $"Avaliar inserção de equipamento opcional: {equip.Nome}",
                        equip.Nome, "Equipamento"));
                }
            }

            // ── Equipamentos extras ──────────────────────────
            foreach (var existente in ambiente.EquipamentosExistentes)
            {
                var existenteNorm = existente.FamilyName.Trim().ToLowerInvariant();

                if (!nomesEsperadosNorm.Contains(existenteNorm))
                {
                    acoes.Add(CriarAcao(ambiente, TipoAcao.Ajustar,
                        PrioridadeAcao.Media,
                        $"Verificar equipamento não esperado: {existente.FamilyName}",
                        existente.FamilyName, "Equipamento"));
                }
            }

            // ── Pontos hidráulicos ───────────────────────────
            var pontos = _pontosService.GetPontosObrigatorios(ambiente).ToList();

            foreach (var ponto in pontos)
            {
                acoes.Add(CriarAcao(ambiente, TipoAcao.Inserir,
                    PrioridadeAcao.Alta,
                    $"Criar ponto hidráulico: {ponto.Nome} " +
                    $"(Ø{ponto.DiametroMm}mm, {ponto.Rede})",
                    ponto.Nome, "PontoHidraulico"));
            }

            return acoes;
        }

        // ══════════════════════════════════════════════════════════
        //  CONSULTAS
        // ══════════════════════════════════════════════════════════

        /// <summary>Filtra ações por tipo.</summary>
        public IEnumerable<AcaoAmbiente> FiltrarPorTipo(
            IEnumerable<AcaoAmbiente> acoes, TipoAcao tipo)
        {
            return acoes.Where(a => a.Tipo == tipo);
        }

        /// <summary>Filtra ações por prioridade.</summary>
        public IEnumerable<AcaoAmbiente> FiltrarPorPrioridade(
            IEnumerable<AcaoAmbiente> acoes, PrioridadeAcao prioridade)
        {
            return acoes.Where(a => a.Prioridade == prioridade);
        }

        /// <summary>Retorna ações agrupadas por ambiente.</summary>
        public Dictionary<string, List<AcaoAmbiente>> AgruparPorAmbiente(
            IEnumerable<AcaoAmbiente> acoes)
        {
            return acoes
                .GroupBy(a => $"{a.AmbienteNome} (#{a.AmbienteNumero})")
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>Gera resumo textual das ações.</summary>
        public string GerarResumo(IEnumerable<AcaoAmbiente> acoes)
        {
            var lista = acoes.ToList();

            if (lista.Count == 0)
                return "Nenhuma ação pendente.";

            var inserir = lista.Count(a => a.Tipo == TipoAcao.Inserir);
            var validar = lista.Count(a => a.Tipo == TipoAcao.Validar);
            var ajustar = lista.Count(a => a.Tipo == TipoAcao.Ajustar);
            var verificar = lista.Count(a => a.Tipo == TipoAcao.Verificar);

            var alta = lista.Count(a => a.Prioridade == PrioridadeAcao.Alta);

            var lines = new List<string>
            {
                "══════════════════════════════",
                "  Resumo de Ações",
                "══════════════════════════════",
                $"  ➕ Inserir:    {inserir,4}",
                $"  ✅ Validar:    {validar,4}",
                $"  🔧 Ajustar:    {ajustar,4}",
                $"  🔍 Verificar:  {verificar,4}",
                "──────────────────────────────",
                $"  Total:         {lista.Count,4}",
                $"  Alta prioridade: {alta,3}",
                "══════════════════════════════",
            };

            return string.Join("\n", lines);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPER
        // ══════════════════════════════════════════════════════════

        private static AcaoAmbiente CriarAcao(AmbienteInfo ambiente,
            TipoAcao tipo, PrioridadeAcao prioridade,
            string descricao, string equipamento, string categoria)
        {
            return new AcaoAmbiente
            {
                AmbienteNome = ambiente.NomeOriginal,
                AmbienteNumero = ambiente.Numero,
                ElementId = ambiente.ElementId,
                Tipo = tipo,
                Prioridade = prioridade,
                Descricao = descricao,
                Equipamento = equipamento,
                Categoria = categoria,
            };
        }

        // Exemplos:
        // var service = new AcaoAmbienteService();
        // var acoes = service.GerarAcoes(ambientes).ToList();
        //
        // → ➕ [Inserir] Banheiro → Inserir equipamento obrigatório: Vaso Sanitário
        // → ➕ [Inserir] Banheiro → Criar ponto hidráulico: Vaso Sanitário - AF (Ø25mm, AguaFria)
        // → ✅ [Validar] Cozinha  → Validar equipamento existente: Pia de Cozinha
        // → 🔧 [Ajustar] Lavabo  → Verificar equipamento não esperado: Banheira
        //
        // var resumo = service.GerarResumo(acoes);
        // var inserir = service.FiltrarPorTipo(acoes, TipoAcao.Inserir);
        // var porAmbiente = service.AgruparPorAmbiente(acoes);
    }
}
