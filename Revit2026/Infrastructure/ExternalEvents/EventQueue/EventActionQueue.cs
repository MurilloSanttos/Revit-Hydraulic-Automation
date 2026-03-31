using System.Collections.Concurrent;
using Autodesk.Revit.DB;

namespace Revit2026.Infrastructure.ExternalEvents.EventQueue
{
    /// <summary>
    /// Representa uma ação enfileirada com callback opcional.
    /// A ação recebe Document e retorna um resultado (object?).
    /// O callback recebe o resultado após execução.
    /// </summary>
    public class EventActionItem
    {
        /// <summary>
        /// Ação a executar no thread do Revit.
        /// Recebe Document, retorna resultado arbitrário.
        /// </summary>
        public Func<Document, object?> Acao { get; }

        /// <summary>
        /// Callback executado APÓS a ação, fora do contexto Revit.
        /// Recebe o resultado da ação.
        /// </summary>
        public Action<object?>? Callback { get; }

        /// <summary>
        /// Identificador único do item (para rastreamento).
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Timestamp de criação do item.
        /// </summary>
        public DateTime CriadoEm { get; }

        /// <summary>
        /// Descrição opcional para logging.
        /// </summary>
        public string? Descricao { get; }

        public EventActionItem(
            Func<Document, object?> acao,
            Action<object?>? callback = null,
            string? descricao = null)
        {
            Acao = acao ?? throw new ArgumentNullException(nameof(acao));
            Callback = callback;
            Id = Guid.NewGuid().ToString("N")[..8];
            CriadoEm = DateTime.Now;
            Descricao = descricao;
        }

        public override string ToString() =>
            $"[{Id}] {Descricao ?? "Ação"} ({CriadoEm:HH:mm:ss.fff})";
    }

    /// <summary>
    /// Resultado da execução de um EventActionItem.
    /// </summary>
    public class EventActionResult
    {
        public string ItemId { get; set; } = "";
        public bool Sucesso { get; set; }
        public object? Resultado { get; set; }
        public string? Erro { get; set; }
        public TimeSpan Duracao { get; set; }

        public override string ToString() =>
            $"[{ItemId}] {(Sucesso ? "OK" : "FALHA")} ({Duracao.TotalMilliseconds:F0}ms)";
    }

    /// <summary>
    /// Interface da fila de eventos com callbacks.
    /// </summary>
    public interface IEventActionQueue
    {
        /// <summary>
        /// Enfileira uma ação com callback opcional.
        /// Thread-safe.
        /// </summary>
        void Enfileirar(
            Func<Document, object?> acao,
            Action<object?>? callback = null,
            string? descricao = null);

        /// <summary>
        /// Tenta desenfileirar o próximo item.
        /// </summary>
        bool TryDesenfileirar(out EventActionItem? item);

        /// <summary>
        /// Limpa toda a fila.
        /// </summary>
        void Limpar();

        /// <summary>
        /// Executa um item: roda a ação e dispara o callback.
        /// </summary>
        EventActionResult Executar(Document doc, EventActionItem item);

        /// <summary>
        /// Processa todos os itens da fila.
        /// </summary>
        List<EventActionResult> ProcessarTodos(Document doc);

        /// <summary>
        /// Número de itens pendentes.
        /// </summary>
        int Pendentes { get; }

        /// <summary>
        /// Se a fila está vazia.
        /// </summary>
        bool Vazia { get; }

        /// <summary>
        /// Evento disparado quando um item é processado.
        /// </summary>
        event Action<EventActionResult>? ItemProcessado;
    }

    /// <summary>
    /// Fila de eventos thread-safe com suporte a callbacks.
    ///
    /// Características:
    /// - ConcurrentQueue para thread-safety sem locks
    /// - Callbacks executados APÓS a ação Revit (fora do contexto)
    /// - Exceções em callbacks são capturadas (nunca propagadas)
    /// - Execução ordenada (FIFO)
    /// - Métricas de execução (duração, sucesso/falha)
    ///
    /// Integração com BaseExternalEventHandler:
    ///   handler.EnfileirarAcao(app => {
    ///       var doc = app.ActiveUIDocument.Document;
    ///       queue.ProcessarTodos(doc);
    ///   });
    /// </summary>
    public class EventActionQueue : IEventActionQueue
    {
        private readonly ConcurrentQueue<EventActionItem> _queue = new();

        // Contadores de métricas
        private volatile int _totalProcessados;
        private volatile int _totalSucessos;
        private volatile int _totalFalhas;

        /// <summary>
        /// Evento disparado quando um item é processado.
        /// </summary>
        public event Action<EventActionResult>? ItemProcessado;

        // ══════════════════════════════════════════════════════════
        //  ENFILEIRAR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Enfileira uma ação com callback opcional.
        /// Thread-safe: pode ser chamado de qualquer thread.
        /// </summary>
        public void Enfileirar(
            Func<Document, object?> acao,
            Action<object?>? callback = null,
            string? descricao = null)
        {
            if (acao == null)
                throw new ArgumentNullException(nameof(acao));

            var item = new EventActionItem(acao, callback, descricao);
            _queue.Enqueue(item);
        }

        /// <summary>
        /// Enfileira uma ação sem retorno (void).
        /// </summary>
        public void Enfileirar(
            Action<Document> acao,
            Action? callbackVoid = null,
            string? descricao = null)
        {
            Enfileirar(
                doc =>
                {
                    acao(doc);
                    return null;
                },
                callbackVoid != null
                    ? _ => callbackVoid()
                    : null,
                descricao);
        }

        /// <summary>
        /// Enfileira uma ação tipada com callback tipado.
        /// </summary>
        public void Enfileirar<T>(
            Func<Document, T> acao,
            Action<T>? callback = null,
            string? descricao = null)
        {
            Enfileirar(
                doc => (object?)acao(doc),
                callback != null
                    ? resultado => callback((T)resultado!)
                    : null,
                descricao);
        }

        // ══════════════════════════════════════════════════════════
        //  DESENFILEIRAR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Tenta desenfileirar o próximo item.
        /// Thread-safe, não bloqueia.
        /// </summary>
        public bool TryDesenfileirar(out EventActionItem? item)
        {
            if (_queue.TryDequeue(out var dequeued))
            {
                item = dequeued;
                return true;
            }

            item = null;
            return false;
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUTAR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa um item individual: roda ação + callback.
        /// A ação roda no contexto do Revit (thread principal).
        /// O callback roda após, sem contexto Revit.
        /// Exceções no callback são capturadas silenciosamente.
        /// </summary>
        public EventActionResult Executar(Document doc, EventActionItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var result = new EventActionResult { ItemId = item.Id };
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // ── Executar ação no contexto Revit ───────────
                var resultado = item.Acao(doc);

                sw.Stop();
                result.Sucesso = true;
                result.Resultado = resultado;
                result.Duracao = sw.Elapsed;

                Interlocked.Increment(ref _totalSucessos);

                // ── Executar callback (fora do contexto Revit) ─
                ExecutarCallback(item.Callback, resultado);
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.Sucesso = false;
                result.Erro = ex.Message;
                result.Duracao = sw.Elapsed;

                Interlocked.Increment(ref _totalFalhas);

                // Callback com null em caso de erro
                ExecutarCallback(item.Callback, null);
            }
            finally
            {
                Interlocked.Increment(ref _totalProcessados);

                // Notificar observadores
                try { ItemProcessado?.Invoke(result); }
                catch { /* evento externo nunca propaga */ }
            }

            return result;
        }

        /// <summary>
        /// Processa todos os itens da fila em ordem.
        /// Retorna lista de resultados.
        /// </summary>
        public List<EventActionResult> ProcessarTodos(Document doc)
        {
            var resultados = new List<EventActionResult>();

            while (TryDesenfileirar(out var item))
            {
                if (item == null) continue;
                var result = Executar(doc, item);
                resultados.Add(result);
            }

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  CALLBACK
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa callback de forma segura.
        /// Nunca propaga exceções.
        /// </summary>
        private static void ExecutarCallback(
            Action<object?>? callback,
            object? resultado)
        {
            if (callback == null) return;

            try
            {
                callback(resultado);
            }
            catch
            {
                // Callbacks NUNCA propagam exceções.
                // Erros são silenciados para proteger o pipeline.
            }
        }

        // ══════════════════════════════════════════════════════════
        //  LIMPAR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Remove todos os itens pendentes da fila.
        /// </summary>
        public void Limpar()
        {
            while (_queue.TryDequeue(out _)) { }
        }

        // ══════════════════════════════════════════════════════════
        //  ESTADO / MÉTRICAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Número de itens pendentes na fila.
        /// </summary>
        public int Pendentes => _queue.Count;

        /// <summary>
        /// Se a fila está vazia.
        /// </summary>
        public bool Vazia => _queue.IsEmpty;

        /// <summary>
        /// Total de itens processados desde a criação.
        /// </summary>
        public int TotalProcessados => _totalProcessados;

        /// <summary>
        /// Total de execuções bem-sucedidas.
        /// </summary>
        public int TotalSucessos => _totalSucessos;

        /// <summary>
        /// Total de execuções com falha.
        /// </summary>
        public int TotalFalhas => _totalFalhas;

        /// <summary>
        /// Resumo de métricas.
        /// </summary>
        public override string ToString() =>
            $"EventActionQueue: {Pendentes} pendentes, " +
            $"{TotalProcessados} processados " +
            $"({TotalSucessos} OK, {TotalFalhas} falhas)";
    }
}
