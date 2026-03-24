using PluginCore.Interfaces;

namespace PluginCore.Pipeline
{
    /// <summary>
    /// Resultado da execução completa do pipeline.
    /// </summary>
    public class ResultadoPipeline
    {
        /// <summary>Início da execução.</summary>
        public DateTime Inicio { get; set; }

        /// <summary>Fim da execução.</summary>
        public DateTime Fim { get; set; }

        /// <summary>Duração total.</summary>
        public TimeSpan Duracao => Fim - Inicio;

        /// <summary>Total de etapas.</summary>
        public int TotalEtapas { get; set; }

        /// <summary>Etapas concluídas.</summary>
        public int Concluidas { get; set; }

        /// <summary>Etapas com falha.</summary>
        public int Falhas { get; set; }

        /// <summary>Etapas ignoradas.</summary>
        public int Ignoradas { get; set; }

        /// <summary>Pipeline concluído com sucesso.</summary>
        public bool Sucesso => Falhas == 0;

        /// <summary>Foi cancelado.</summary>
        public bool Cancelado { get; set; }

        /// <summary>Detalhes por etapa.</summary>
        public List<(string Nome, StatusEtapa Status, TimeSpan? Duracao)> Detalhes { get; set; } = new();

        public override string ToString()
        {
            var status = Cancelado ? "🚫 Cancelado" : Sucesso ? "✅ Sucesso" : "❌ Falha";
            return $"{status} | {Concluidas}/{TotalEtapas} etapas | {Duracao.TotalSeconds:F1}s";
        }
    }

    /// <summary>
    /// Orquestrador de execução do pipeline de dimensionamento hidráulico.
    /// Executa etapas sequencialmente com verificação de pré-condições,
    /// controle de falhas, pausa configurável e suporte a cancelamento.
    /// </summary>
    public class OrquestradorService
    {
        private readonly ILogService? _log;
        private readonly int _pausaMs;
        private bool _cancelado;

        private const string ETAPA = "00_Pipeline";
        private const string COMPONENTE = "Orquestrador";

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public OrquestradorService(int pausaEntreEtapasMs = 500)
        {
            _pausaMs = pausaEntreEtapasMs;
        }

        public OrquestradorService(ILogService log, int pausaEntreEtapasMs = 500)
        {
            _log = log;
            _pausaMs = pausaEntreEtapasMs;
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUÇÃO DO PIPELINE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa todas as etapas do pipeline sequencialmente.
        /// </summary>
        public ResultadoPipeline ExecutarPipeline(PipelineFila fila)
        {
            var resultado = new ResultadoPipeline
            {
                Inicio = DateTime.Now,
                TotalEtapas = fila.Etapas.Count,
            };

            _cancelado = false;

            _log?.Info(ETAPA, COMPONENTE,
                $"═══ Iniciando pipeline ({fila.Etapas.Count} etapas) ═══");

            foreach (var etapa in fila.Etapas.OrderBy(e => e.Ordem))
            {
                // Verificar cancelamento
                if (_cancelado)
                {
                    etapa.Status = StatusEtapa.Cancelada;
                    resultado.Cancelado = true;
                    _log?.Medio(ETAPA, COMPONENTE,
                        $"Pipeline cancelado na etapa '{etapa.Nome}'.");
                    break;
                }

                // Executar etapa
                var sucesso = ExecutarEtapa(etapa, fila);

                // Registrar detalhe
                resultado.Detalhes.Add((etapa.Nome, etapa.Status, etapa.Duracao));

                // Atualizar contexto
                fila.Contexto.StatusEtapas[etapa.Nome] = etapa.Status;

                // Contabilizar
                switch (etapa.Status)
                {
                    case StatusEtapa.Concluida:
                    case StatusEtapa.ConcluidaComAvisos:
                        resultado.Concluidas++;
                        break;
                    case StatusEtapa.Falha:
                        resultado.Falhas++;
                        break;
                    case StatusEtapa.Ignorada:
                        resultado.Ignoradas++;
                        break;
                }

                // Se falha em etapa obrigatória, propagar
                if (etapa.Status == StatusEtapa.Falha && etapa.Obrigatoria)
                {
                    _log?.Critico(ETAPA, COMPONENTE,
                        $"Etapa obrigatória '{etapa.Nome}' falhou — " +
                        $"marcando dependentes como ignoradas.");

                    var ignoradas = PreCondicoesService.MarcarIgnoradas(fila.Etapas, _log);
                    resultado.Ignoradas += ignoradas;
                }

                // Pausa entre etapas
                if (_pausaMs > 0 && etapa.Status != StatusEtapa.Ignorada)
                    Thread.Sleep(_pausaMs);
            }

            resultado.Fim = DateTime.Now;

            _log?.Info(ETAPA, COMPONENTE,
                $"═══ Pipeline finalizado: {resultado} ═══");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUÇÃO DE ETAPA INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa uma etapa individual com verificação de pré-condições.
        /// </summary>
        public bool ExecutarEtapa(EtapaPipeline etapa, PipelineFila fila)
        {
            if (etapa == null)
                return false;

            // ── 1. Verificar se já foi processada ─────────────
            if (etapa.Status == StatusEtapa.Concluida ||
                etapa.Status == StatusEtapa.ConcluidaComAvisos ||
                etapa.Status == StatusEtapa.Ignorada)
                return true;

            // ── 2. Verificar pré-condições ────────────────────
            var preCondicao = PreCondicoesService.VerificarDetalhado(
                etapa, fila.Etapas, _log);

            if (!preCondicao.PodeExecutar)
            {
                // Dependência falhou → ignorar
                if (preCondicao.Falhas.Count > 0)
                {
                    etapa.Status = StatusEtapa.Ignorada;
                    etapa.MensagemErro = preCondicao.Motivo;

                    _log?.Medio(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}' ignorada: {preCondicao.Motivo}");
                    return false;
                }

                // Dependência pendente → falha
                etapa.Status = StatusEtapa.Falha;
                etapa.MensagemErro = preCondicao.Motivo;

                _log?.Critico(ETAPA, COMPONENTE,
                    $"Etapa '{etapa.Nome}' bloqueada: {preCondicao.Motivo}");
                return false;
            }

            // ── 3. Executar ───────────────────────────────────
            etapa.Status = StatusEtapa.EmExecucao;
            etapa.InicioExecucao = DateTime.Now;
            etapa.TentativaAtual++;

            _log?.Info(ETAPA, COMPONENTE,
                $"▶ Executando '{etapa.Nome}' " +
                $"(tentativa {etapa.TentativaAtual}/{etapa.MaxTentativas})...");

            try
            {
                // Ação com contexto tem prioridade
                if (etapa.AcaoComContexto != null)
                    etapa.AcaoComContexto(fila.Contexto);
                else if (etapa.AcaoAsync != null)
                    etapa.AcaoAsync().GetAwaiter().GetResult();
                else if (etapa.Acao != null)
                    etapa.Acao();
                else
                {
                    _log?.Leve(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}': nenhuma ação definida.");
                }

                // Sucesso
                etapa.FimExecucao = DateTime.Now;

                etapa.Status = etapa.Alertas.Count > 0
                    ? StatusEtapa.ConcluidaComAvisos
                    : StatusEtapa.Concluida;

                _log?.Info(ETAPA, COMPONENTE,
                    $"✅ '{etapa.Nome}' concluída em " +
                    $"{etapa.Duracao?.TotalMilliseconds:F0}ms" +
                    $"{(etapa.Alertas.Count > 0 ? $" ({etapa.Alertas.Count} alertas)" : "")}.");

                return true;
            }
            catch (Exception ex)
            {
                etapa.FimExecucao = DateTime.Now;
                etapa.Excecao = ex;
                etapa.MensagemErro = ex.Message;

                // Retry?
                if (etapa.TentativaAtual < etapa.MaxTentativas)
                {
                    _log?.Medio(ETAPA, COMPONENTE,
                        $"⚠ '{etapa.Nome}' falhou (tentativa {etapa.TentativaAtual}): " +
                        $"{ex.Message}. Tentando novamente...");

                    etapa.Status = StatusEtapa.Pendente;
                    Thread.Sleep(500); // Pausa antes do retry
                    return ExecutarEtapa(etapa, fila);
                }

                etapa.Status = StatusEtapa.Falha;

                _log?.Critico(ETAPA, COMPONENTE,
                    $"❌ '{etapa.Nome}' falhou após {etapa.TentativaAtual} tentativa(s): " +
                    $"{ex.Message}");

                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONTROLE
        // ══════════════════════════════════════════════════════════

        /// <summary>Cancela o pipeline.</summary>
        public void Cancelar()
        {
            _cancelado = true;
            _log?.Medio(ETAPA, COMPONENTE, "Cancelamento solicitado.");
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual da execução.</summary>
        public static string GerarResumo(ResultadoPipeline resultado)
        {
            if (resultado == null)
                return "Pipeline não executado.";

            var lines = new List<string>
            {
                "══════════════════════════════════════════════",
                "  RESULTADO DO PIPELINE",
                "══════════════════════════════════════════════",
                $"  Status:     {resultado}",
                $"  Início:     {resultado.Inicio:HH:mm:ss}",
                $"  Fim:        {resultado.Fim:HH:mm:ss}",
                $"  Duração:    {resultado.Duracao.TotalSeconds:F1}s",
                "──────────────────────────────────────────────",
            };

            foreach (var (nome, status, duracao) in resultado.Detalhes)
            {
                var icon = status switch
                {
                    StatusEtapa.Concluida => "✅",
                    StatusEtapa.ConcluidaComAvisos => "⚠️",
                    StatusEtapa.Falha => "❌",
                    StatusEtapa.Ignorada => "⏭️",
                    StatusEtapa.Cancelada => "🚫",
                    _ => "❓",
                };

                var tempo = duracao.HasValue ? $" ({duracao.Value.TotalMilliseconds:F0}ms)" : "";
                lines.Add($"  {icon} {nome}{tempo}");
            }

            lines.Add("══════════════════════════════════════════════");

            return string.Join("\n", lines);
        }

        // Exemplos:
        // var fila = new PipelineFila(logService);
        // var orquestrador = new OrquestradorService(logService, pausaEntreEtapasMs: 500);
        //
        // var resultado = orquestrador.ExecutarPipeline(fila);
        //
        // Console.WriteLine(OrquestradorService.GerarResumo(resultado));
        // ══════════════════════════════════════════════
        //   RESULTADO DO PIPELINE
        // ══════════════════════════════════════════════
        //   Status:     ✅ Sucesso | 11/11 etapas | 3.2s
        //   Início:     14:30:01
        //   Fim:        14:30:04
        // ──────────────────────────────────────────────
        //   ✅ CarregarModelo (120ms)
        //   ✅ DeteccaoAmbientes (340ms)
        //   ✅ ValidarAmbientes (89ms)
        //   ...
        // ══════════════════════════════════════════════
        //
        // // Cancelar
        // orquestrador.Cancelar();
    }
}
