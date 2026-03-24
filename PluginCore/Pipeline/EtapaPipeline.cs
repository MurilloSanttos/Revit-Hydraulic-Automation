namespace PluginCore.Pipeline
{
    /// <summary>
    /// Status de execução de uma etapa do pipeline.
    /// </summary>
    public enum StatusEtapa
    {
        /// <summary>Aguardando execução.</summary>
        Pendente,

        /// <summary>Aguardando dependências serem concluídas.</summary>
        AguardandoDependencias,

        /// <summary>Em execução.</summary>
        EmExecucao,

        /// <summary>Concluída com sucesso.</summary>
        Concluida,

        /// <summary>Concluída com avisos.</summary>
        ConcluidaComAvisos,

        /// <summary>Falha na execução.</summary>
        Falha,

        /// <summary>Ignorada (dependência falhou).</summary>
        Ignorada,

        /// <summary>Cancelada pelo usuário.</summary>
        Cancelada,
    }

    /// <summary>
    /// Representa uma etapa do pipeline de dimensionamento hidráulico.
    /// Cada etapa possui nome, ação, dependências e controle de status.
    /// </summary>
    public class EtapaPipeline
    {
        /// <summary>Nome único da etapa (ex: "01_DeteccaoAmbientes").</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Descrição curta para logs e UI.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Componente responsável (ex: "AmbienteService").</summary>
        public string Componente { get; set; } = string.Empty;

        /// <summary>Status atual da etapa.</summary>
        public StatusEtapa Status { get; set; } = StatusEtapa.Pendente;

        /// <summary>Nomes das etapas das quais esta depende.</summary>
        public List<string> Dependencias { get; set; } = new();

        /// <summary>Ação síncrona a executar.</summary>
        public Action? Acao { get; set; }

        /// <summary>Ação assíncrona a executar (prioridade sobre Acao).</summary>
        public Func<Task>? AcaoAsync { get; set; }

        /// <summary>Ação com contexto do pipeline.</summary>
        public Action<PipelineContext>? AcaoComContexto { get; set; }

        /// <summary>Se a etapa é obrigatória (falha bloqueia pipeline).</summary>
        public bool Obrigatoria { get; set; } = true;

        /// <summary>Ordem de execução (para ordenação quando sem dependências).</summary>
        public int Ordem { get; set; }

        /// <summary>Tempo máximo de execução (ms). 0 = sem limite.</summary>
        public int TimeoutMs { get; set; }

        /// <summary>Número de tentativas em caso de falha.</summary>
        public int MaxTentativas { get; set; } = 1;

        /// <summary>Tentativa atual.</summary>
        public int TentativaAtual { get; set; }

        /// <summary>Hora de início da execução.</summary>
        public DateTime? InicioExecucao { get; set; }

        /// <summary>Hora de fim da execução.</summary>
        public DateTime? FimExecucao { get; set; }

        /// <summary>Duração da execução.</summary>
        public TimeSpan? Duracao => FimExecucao.HasValue && InicioExecucao.HasValue
            ? FimExecucao.Value - InicioExecucao.Value
            : null;

        /// <summary>Mensagem de erro (se falhou).</summary>
        public string? MensagemErro { get; set; }

        /// <summary>Exceção capturada (se falhou).</summary>
        public Exception? Excecao { get; set; }

        /// <summary>Alertas gerados durante execução.</summary>
        public List<string> Alertas { get; set; } = new();

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public EtapaPipeline() { }

        public EtapaPipeline(string nome, Action acao, List<string>? dependencias = null)
        {
            Nome = nome;
            Acao = acao;
            if (dependencias != null)
                Dependencias = dependencias;
        }

        public EtapaPipeline(string nome, Func<Task> acaoAsync, List<string>? dependencias = null)
        {
            Nome = nome;
            AcaoAsync = acaoAsync;
            if (dependencias != null)
                Dependencias = dependencias;
        }

        public EtapaPipeline(string nome, Action<PipelineContext> acao, List<string>? dependencias = null)
        {
            Nome = nome;
            AcaoComContexto = acao;
            if (dependencias != null)
                Dependencias = dependencias;
        }

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Verifica se todas as dependências estão concluídas.</summary>
        public bool DependenciasSatisfeitas(Dictionary<string, StatusEtapa> statusMap)
        {
            if (Dependencias.Count == 0)
                return true;

            return Dependencias.All(dep =>
                statusMap.TryGetValue(dep, out var s) &&
                (s == StatusEtapa.Concluida || s == StatusEtapa.ConcluidaComAvisos));
        }

        /// <summary>Verifica se alguma dependência falhou.</summary>
        public bool DependenciaFalhou(Dictionary<string, StatusEtapa> statusMap)
        {
            return Dependencias.Any(dep =>
                statusMap.TryGetValue(dep, out var s) &&
                (s == StatusEtapa.Falha || s == StatusEtapa.Cancelada));
        }

        /// <summary>Se pode executar (dependências OK, status Pendente).</summary>
        public bool PodeExecutar(Dictionary<string, StatusEtapa> statusMap)
        {
            return Status == StatusEtapa.Pendente && DependenciasSatisfeitas(statusMap);
        }

        /// <summary>Reseta a etapa para re-execução.</summary>
        public void Resetar()
        {
            Status = StatusEtapa.Pendente;
            InicioExecucao = null;
            FimExecucao = null;
            MensagemErro = null;
            Excecao = null;
            TentativaAtual = 0;
            Alertas.Clear();
        }

        public override string ToString()
        {
            var icon = Status switch
            {
                StatusEtapa.Pendente => "⏳",
                StatusEtapa.AguardandoDependencias => "⏸️",
                StatusEtapa.EmExecucao => "🔄",
                StatusEtapa.Concluida => "✅",
                StatusEtapa.ConcluidaComAvisos => "⚠️",
                StatusEtapa.Falha => "❌",
                StatusEtapa.Ignorada => "⏭️",
                StatusEtapa.Cancelada => "🚫",
                _ => "❓",
            };

            var tempo = Duracao.HasValue ? $" ({Duracao.Value.TotalMilliseconds:F0}ms)" : "";
            return $"{icon} [{Nome}] {Descricao}{tempo}";
        }
    }

    /// <summary>
    /// Contexto compartilhado entre etapas do pipeline.
    /// Permite passar dados entre etapas sem acoplamento direto.
    /// </summary>
    public class PipelineContext
    {
        /// <summary>Dados compartilhados entre etapas (chave-valor).</summary>
        private readonly Dictionary<string, object> _dados = new();

        /// <summary>Status de todas as etapas.</summary>
        public Dictionary<string, StatusEtapa> StatusEtapas { get; } = new();

        /// <summary>Se o pipeline deve ser cancelado.</summary>
        public bool Cancelado { get; set; }

        /// <summary>Hora de início do pipeline.</summary>
        public DateTime Inicio { get; set; } = DateTime.Now;

        /// <summary>Armazena um dado no contexto.</summary>
        public void Set<T>(string chave, T valor)
        {
            _dados[chave] = valor!;
        }

        /// <summary>Recupera um dado do contexto.</summary>
        public T? Get<T>(string chave)
        {
            return _dados.TryGetValue(chave, out var val) && val is T typed
                ? typed
                : default;
        }

        /// <summary>Verifica se existe um dado no contexto.</summary>
        public bool Has(string chave)
        {
            return _dados.ContainsKey(chave);
        }

        /// <summary>Remove um dado do contexto.</summary>
        public bool Remove(string chave)
        {
            return _dados.Remove(chave);
        }

        /// <summary>Duração total do pipeline até agora.</summary>
        public TimeSpan Duracao => DateTime.Now - Inicio;
    }

    // Exemplos:
    // var etapa = new EtapaPipeline(
    //     "01_DeteccaoAmbientes",
    //     () => AmbienteService.ClassificarTodos());
    // etapa.Descricao = "Detectar e classificar ambientes";
    // etapa.Componente = "AmbienteService";
    // etapa.Dependencias.Add("00_CarregarModelo");
    //
    // var etapaAsync = new EtapaPipeline(
    //     "02_CalculoVazao",
    //     async () => await VazaoService.CalcularAsync());
    //
    // var etapaContexto = new EtapaPipeline(
    //     "03_Dimensionamento",
    //     (ctx) =>
    //     {
    //         var pontos = ctx.Get<List<PontoHidraulico>>("pontos");
    //         DimensionamentoService.Dimensionar(pontos);
    //         ctx.Set("resultado", resultado);
    //     });
}
