using PluginCore.Interfaces;

namespace PluginCore.Pipeline
{
    /// <summary>
    /// Dados do evento de progresso de uma etapa.
    /// </summary>
    public class EtapaProgressEventArgs : EventArgs
    {
        /// <summary>Nome da etapa.</summary>
        public string NomeEtapa { get; set; } = string.Empty;

        /// <summary>Descrição da etapa.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Status atual.</summary>
        public StatusEtapa Status { get; set; }

        /// <summary>Índice da etapa (0-based).</summary>
        public int IndiceEtapa { get; set; }

        /// <summary>Total de etapas no pipeline.</summary>
        public int TotalEtapas { get; set; }

        /// <summary>Progresso geral (0.0 a 1.0).</summary>
        public double Progresso => TotalEtapas > 0
            ? (double)IndiceEtapa / TotalEtapas
            : 0.0;

        /// <summary>Progresso percentual (0 a 100).</summary>
        public int ProgressoPct => (int)(Progresso * 100);

        /// <summary>Duração da etapa (se concluída).</summary>
        public TimeSpan? Duracao { get; set; }

        /// <summary>Mensagem adicional.</summary>
        public string? Mensagem { get; set; }

        /// <summary>Duração total do pipeline até agora.</summary>
        public TimeSpan DuracaoTotal { get; set; }
    }

    /// <summary>
    /// Dados do evento de conclusão do pipeline.
    /// </summary>
    public class PipelineConcluidoEventArgs : EventArgs
    {
        /// <summary>Resultado final.</summary>
        public ResultadoPipeline? Resultado { get; set; }

        /// <summary>Sucesso geral.</summary>
        public bool Sucesso { get; set; }

        /// <summary>Mensagem resumo.</summary>
        public string Resumo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sistema de eventos de progresso do pipeline.
    /// Permite que UI, dashboards e notificações acompanhem a execução em tempo real.
    /// </summary>
    public class PipelineProgressEvents
    {
        private readonly ILogService? _log;
        private DateTime _inicioPipeline;

        private const string ETAPA = "00_Pipeline";
        private const string COMPONENTE = "Progresso";

        // ══════════════════════════════════════════════════════════
        //  EVENTOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Pipeline iniciou.</summary>
        public event EventHandler<EtapaProgressEventArgs>? PipelineIniciado;

        /// <summary>Etapa iniciou.</summary>
        public event EventHandler<EtapaProgressEventArgs>? EtapaIniciada;

        /// <summary>Status da etapa mudou.</summary>
        public event EventHandler<EtapaProgressEventArgs>? EtapaAtualizada;

        /// <summary>Etapa concluiu (sucesso ou falha).</summary>
        public event EventHandler<EtapaProgressEventArgs>? EtapaConcluida;

        /// <summary>Pipeline concluiu.</summary>
        public event EventHandler<PipelineConcluidoEventArgs>? PipelineConcluido;

        /// <summary>Pipeline cancelado.</summary>
        public event EventHandler<PipelineConcluidoEventArgs>? PipelineCancelado;

        /// <summary>Mensagem de log genérica.</summary>
        public event Action<string>? MensagemRecebida;

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public PipelineProgressEvents() { }

        public PipelineProgressEvents(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  DISPARAR EVENTOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Notifica início do pipeline.</summary>
        public void DispararInicioPipeline(int totalEtapas)
        {
            _inicioPipeline = DateTime.Now;

            var args = new EtapaProgressEventArgs
            {
                NomeEtapa = "Pipeline",
                Status = StatusEtapa.EmExecucao,
                TotalEtapas = totalEtapas,
                Mensagem = $"Iniciando pipeline com {totalEtapas} etapas.",
            };

            PipelineIniciado?.Invoke(this, args);
            MensagemRecebida?.Invoke(args.Mensagem);

            _log?.Info(ETAPA, COMPONENTE,
                $"▶ Pipeline iniciado ({totalEtapas} etapas).");
        }

        /// <summary>Notifica início de uma etapa.</summary>
        public void DispararInicio(EtapaPipeline etapa, int indice, int total)
        {
            var args = CriarArgs(etapa, indice, total);
            args.Status = StatusEtapa.EmExecucao;
            args.Mensagem = $"Executando: {etapa.Descricao}";

            EtapaIniciada?.Invoke(this, args);
            MensagemRecebida?.Invoke(
                $"[{args.ProgressoPct}%] ▶ {etapa.Nome}: {etapa.Descricao}");

            _log?.Info(ETAPA, COMPONENTE,
                $"[{args.ProgressoPct}%] ▶ '{etapa.Nome}' iniciada.");
        }

        /// <summary>Notifica atualização de status.</summary>
        public void DispararAtualizacao(EtapaPipeline etapa, int indice, int total,
            string? mensagem = null)
        {
            var args = CriarArgs(etapa, indice, total);
            args.Mensagem = mensagem ?? $"{etapa.Nome}: {etapa.Status}";

            EtapaAtualizada?.Invoke(this, args);
            MensagemRecebida?.Invoke(args.Mensagem);
        }

        /// <summary>Notifica conclusão de uma etapa.</summary>
        public void DispararConclusao(EtapaPipeline etapa, int indice, int total)
        {
            var args = CriarArgs(etapa, indice + 1, total);
            args.Duracao = etapa.Duracao;

            var icon = etapa.Status switch
            {
                StatusEtapa.Concluida => "✅",
                StatusEtapa.ConcluidaComAvisos => "⚠️",
                StatusEtapa.Falha => "❌",
                StatusEtapa.Ignorada => "⏭️",
                _ => "❓",
            };

            var tempo = etapa.Duracao.HasValue
                ? $" ({etapa.Duracao.Value.TotalMilliseconds:F0}ms)"
                : "";

            args.Mensagem = $"{icon} {etapa.Nome}{tempo}";

            EtapaConcluida?.Invoke(this, args);
            MensagemRecebida?.Invoke(
                $"[{args.ProgressoPct}%] {args.Mensagem}");

            _log?.Info(ETAPA, COMPONENTE,
                $"[{args.ProgressoPct}%] {args.Mensagem}");
        }

        /// <summary>Notifica conclusão do pipeline.</summary>
        public void DispararConclusaoPipeline(ResultadoPipeline resultado)
        {
            var args = new PipelineConcluidoEventArgs
            {
                Resultado = resultado,
                Sucesso = resultado.Sucesso,
                Resumo = resultado.ToString(),
            };

            PipelineConcluido?.Invoke(this, args);
            MensagemRecebida?.Invoke($"[100%] {args.Resumo}");

            _log?.Info(ETAPA, COMPONENTE,
                $"■ Pipeline concluído: {args.Resumo}");
        }

        /// <summary>Notifica cancelamento.</summary>
        public void DispararCancelamento(ResultadoPipeline? resultado = null)
        {
            var args = new PipelineConcluidoEventArgs
            {
                Resultado = resultado,
                Sucesso = false,
                Resumo = "Pipeline cancelado pelo usuário.",
            };

            PipelineCancelado?.Invoke(this, args);
            MensagemRecebida?.Invoke("🚫 Pipeline cancelado.");

            _log?.Medio(ETAPA, COMPONENTE, "Pipeline cancelado.");
        }

        // ══════════════════════════════════════════════════════════
        //  LIMPAR ASSINATURAS
        // ══════════════════════════════════════════════════════════

        /// <summary>Remove todos os handlers registrados.</summary>
        public void LimparAssinaturas()
        {
            PipelineIniciado = null;
            EtapaIniciada = null;
            EtapaAtualizada = null;
            EtapaConcluida = null;
            PipelineConcluido = null;
            PipelineCancelado = null;
            MensagemRecebida = null;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private EtapaProgressEventArgs CriarArgs(EtapaPipeline etapa, int indice, int total)
        {
            return new EtapaProgressEventArgs
            {
                NomeEtapa = etapa.Nome,
                Descricao = etapa.Descricao,
                Status = etapa.Status,
                IndiceEtapa = indice,
                TotalEtapas = total,
                DuracaoTotal = DateTime.Now - _inicioPipeline,
            };
        }

        // Exemplos:
        //
        // var eventos = new PipelineProgressEvents(logService);
        //
        // // Assinar na UI
        // eventos.EtapaIniciada += (s, e) =>
        //     progressBar.Value = e.ProgressoPct;
        //
        // eventos.EtapaConcluida += (s, e) =>
        //     statusLabel.Text = e.Mensagem;
        //
        // eventos.PipelineConcluido += (s, e) =>
        //     MessageBox.Show(e.Resumo);
        //
        // eventos.MensagemRecebida += msg =>
        //     logTextBox.AppendLine(msg);
        //
        // // No OrquestradorService
        // eventos.DispararInicioPipeline(11);
        // eventos.DispararInicio(etapa, 0, 11);
        // eventos.DispararConclusao(etapa, 0, 11);
        // eventos.DispararConclusaoPipeline(resultado);
    }
}
