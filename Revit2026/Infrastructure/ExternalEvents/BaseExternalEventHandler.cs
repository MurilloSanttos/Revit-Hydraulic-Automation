using System.Collections.Concurrent;
using Autodesk.Revit.UI;
using PluginCore.Interfaces;

namespace Revit2026.Infrastructure.ExternalEvents
{
    /// <summary>
    /// Handler base genérico para ExternalEvents do Revit.
    /// Garante que todas as operações do plugin executem no thread
    /// principal do Revit via fila thread-safe (ConcurrentQueue).
    ///
    /// Uso:
    /// 1. Criar classe derivada ou usar GenericExternalEventHandler
    /// 2. Registrar via ExternalEvent.Create(handler)
    /// 3. Chamar handler.RegistrarExternalEvent(event)
    /// 4. Enfileirar ações via handler.EnfileirarAcao(app => { ... })
    ///
    /// Padrão: Producer (qualquer thread) → Consumer (thread Revit)
    /// </summary>
    public abstract class BaseExternalEventHandler : IExternalEventHandler, IDisposable
    {
        private readonly ConcurrentQueue<Action<UIApplication>> _queue = new();
        private readonly ILogService _log;
        private ExternalEvent? _externalEvent;
        private volatile bool _disposed;
        private volatile int _processando;

        private const string ETAPA = "99_Infrastructure";
        private const string COMPONENTE = "ExternalEvent";

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTOR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Construtor base.
        /// </summary>
        /// <param name="logService">Serviço de log para registrar erros.</param>
        protected BaseExternalEventHandler(ILogService logService)
        {
            _log = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        // ══════════════════════════════════════════════════════════
        //  REGISTRO DO EXTERNAL EVENT
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Registra o ExternalEvent associado a este handler.
        /// Deve ser chamado após ExternalEvent.Create(this).
        /// </summary>
        public void RegistrarExternalEvent(ExternalEvent externalEvent)
        {
            _externalEvent = externalEvent
                ?? throw new ArgumentNullException(nameof(externalEvent));
        }

        /// <summary>
        /// Cria e registra o ExternalEvent automaticamente.
        /// Retorna o ExternalEvent criado.
        /// </summary>
        public ExternalEvent CriarERegistrar()
        {
            var evt = ExternalEvent.Create(this);
            RegistrarExternalEvent(evt);
            return evt;
        }

        // ══════════════════════════════════════════════════════════
        //  ENFILEIRAR AÇÕES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Enfileira uma ação para execução no thread principal do Revit.
        /// Thread-safe: pode ser chamado de qualquer thread.
        /// </summary>
        /// <param name="acao">Ação que recebe UIApplication.</param>
        public void EnfileirarAcao(Action<UIApplication> acao)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetName());

            if (acao == null)
                throw new ArgumentNullException(nameof(acao));

            _queue.Enqueue(acao);
            Raise();
        }

        /// <summary>
        /// Enfileira uma ação com Document (atalho comum).
        /// </summary>
        public void EnfileirarAcaoDoc(Action<Autodesk.Revit.DB.Document> acao)
        {
            EnfileirarAcao(app =>
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        "Nenhum documento ativo no Revit.");
                    return;
                }
                acao(doc);
            });
        }

        /// <summary>
        /// Enfileira uma ação com Transaction automática.
        /// </summary>
        public void EnfileirarComTransaction(
            string nomeTransaction,
            Action<Autodesk.Revit.DB.Document> acao)
        {
            EnfileirarAcao(app =>
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        "Nenhum documento ativo para Transaction.");
                    return;
                }

                using var trans = new Autodesk.Revit.DB.Transaction(
                    doc, nomeTransaction);

                try
                {
                    trans.Start();
                    acao(doc);
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    if (trans.HasStarted())
                        trans.RollBack();

                    _log.Critico(ETAPA, COMPONENTE,
                        $"Transaction '{nomeTransaction}' falhou: {ex.Message}",
                        detalhes: ex.StackTrace);
                }
            });
        }

        /// <summary>
        /// Enfileira ação com callback de resultado.
        /// </summary>
        public void EnfileirarComResultado<T>(
            Func<UIApplication, T> funcao,
            Action<T?> callback) where T : class
        {
            EnfileirarAcao(app =>
            {
                try
                {
                    var resultado = funcao(app);
                    callback(resultado);
                }
                catch (Exception ex)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Ação com resultado falhou: {ex.Message}",
                        detalhes: ex.StackTrace);
                    callback(null);
                }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUTE (THREAD REVIT)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executado pelo Revit no thread principal.
        /// Processa todas as ações enfileiradas.
        /// NÃO chamar diretamente.
        /// </summary>
        public void Execute(UIApplication app)
        {
            if (_disposed) return;

            // Guard contra reentrada
            if (Interlocked.CompareExchange(ref _processando, 1, 0) != 0)
                return;

            try
            {
                int processadas = 0;
                int erros = 0;

                while (_queue.TryDequeue(out var acao))
                {
                    try
                    {
                        acao(app);
                        processadas++;
                    }
                    catch (Exception ex)
                    {
                        erros++;
                        _log.Critico(ETAPA, COMPONENTE,
                            $"Erro ao executar ação #{processadas + erros} " +
                            $"em '{GetName()}': {ex.Message}",
                            detalhes: ex.StackTrace);
                    }
                }

                if (processadas > 0 || erros > 0)
                {
                    _log.Info(ETAPA, COMPONENTE,
                        $"'{GetName()}': {processadas} ações processadas" +
                        (erros > 0 ? $", {erros} erros" : "") + ".");
                }
            }
            finally
            {
                Interlocked.Exchange(ref _processando, 0);

                // Se chegaram novas ações durante processamento, re-raise
                if (!_queue.IsEmpty && !_disposed)
                    Raise();
            }
        }

        /// <summary>
        /// Nome do handler para o Revit.
        /// </summary>
        public abstract string GetName();

        // ══════════════════════════════════════════════════════════
        //  RAISE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dispara o ExternalEvent de forma segura.
        /// </summary>
        private void Raise()
        {
            if (_disposed || _externalEvent == null)
                return;

            try
            {
                var result = _externalEvent.Raise();

                if (result != ExternalEventRequest.Accepted &&
                    result != ExternalEventRequest.Pending)
                {
                    _log.Leve(ETAPA, COMPONENTE,
                        $"ExternalEvent.Raise() para '{GetName()}' " +
                        $"retornou: {result}. Ação pode ser atrasada.");
                }
            }
            catch (Exception ex)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Erro ao fazer Raise de '{GetName()}': {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ESTADO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Número de ações pendentes na fila.
        /// </summary>
        public int AcoesPendentes => _queue.Count;

        /// <summary>
        /// Indica se o handler está processando ações.
        /// </summary>
        public bool Processando => _processando == 1;

        /// <summary>
        /// Indica se o handler foi descartado.
        /// </summary>
        public bool Disposed => _disposed;

        // ══════════════════════════════════════════════════════════
        //  DISPOSE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Descarte seguro: esvazia a fila.
        /// Não descarta o ExternalEvent (owner é responsável).
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Esvaziar fila
            while (_queue.TryDequeue(out _)) { }

            _log.Info(ETAPA, COMPONENTE,
                $"Handler '{GetName()}' descartado.");
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  IMPLEMENTAÇÃO GENÉRICA PRONTA PARA USO
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Implementação concreta genérica do BaseExternalEventHandler.
    /// Pode ser instanciada diretamente sem criar subclasses.
    ///
    /// Uso:
    /// var handler = new GenericExternalEventHandler(log, "MeuHandler");
    /// var evt = handler.CriarERegistrar();
    /// handler.EnfileirarAcao(app => { /* no thread Revit */ });
    /// </summary>
    public class GenericExternalEventHandler : BaseExternalEventHandler
    {
        private readonly string _name;

        public GenericExternalEventHandler(ILogService logService, string name = "PluginHidraulico")
            : base(logService)
        {
            _name = name ?? "GenericHandler";
        }

        public override string GetName() => _name;
    }
}
