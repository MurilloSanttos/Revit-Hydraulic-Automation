using Newtonsoft.Json;
using Revit2026.Modules.DynamoIntegration.Contracts;

namespace Revit2026.Modules.DynamoIntegration.Services
{
    /// <summary>
    /// Interface para comunicação com o Dynamo.
    /// </summary>
    public interface IDynamoBridgeService
    {
        /// <summary>
        /// Envia um request para o Dynamo e aguarda resposta.
        /// </summary>
        Task<DynamoToPluginResponse> EnviarAsync(
            PluginToDynamoContract request,
            CancellationToken ct = default);

        /// <summary>
        /// Envia um request sem aguardar resposta.
        /// </summary>
        Task EnviarSemRespostaAsync(
            PluginToDynamoContract request,
            CancellationToken ct = default);

        /// <summary>
        /// Executa um workspace Dynamo com inputs e retorna outputs.
        /// </summary>
        Task<DynamoToPluginResponse> ExecutarWorkspaceAsync(
            string workspacePath,
            List<InputData>? inputs = null,
            List<OutputRequest>? outputs = null,
            int timeoutMs = 30000,
            CancellationToken ct = default);

        /// <summary>
        /// Verifica se o Dynamo está acessível.
        /// </summary>
        Task<bool> PingAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Status da conexão com o Dynamo.
    /// </summary>
    public enum DynamoConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Serviço de comunicação bidirecional entre Plugin (C#) e Dynamo.
    ///
    /// Mecanismo de troca:
    /// - Plugin grava JSON de request em arquivo temporário
    /// - Dynamo monitora diretório (FileSystemWatcher no script .dyn)
    /// - Dynamo processa e grava JSON de response
    /// - Plugin lê response via polling ou FileSystemWatcher
    ///
    /// Alternativas suportadas:
    /// - Named Pipes (IPC local)
    /// - Arquivo compartilhado (padrão)
    ///
    /// Uso:
    ///   var bridge = new DynamoBridgeService(exchangeDir, logService);
    ///   var response = await bridge.ExecutarWorkspaceAsync(
    ///       "scripts/inserir.dyn",
    ///       inputs: new List&lt;InputData&gt; { InputData.String(nodeId, "valor") },
    ///       outputs: new List&lt;OutputRequest&gt; { OutputRequest.Of(nodeId, "resultado") }
    ///   );
    /// </summary>
    public class DynamoBridgeService : IDynamoBridgeService, IDisposable
    {
        private readonly string _exchangeDir;
        private readonly string _requestDir;
        private readonly string _responseDir;
        private readonly int _defaultTimeoutMs;
        private FileSystemWatcher? _responseWatcher;

        // Respostas pendentes: requestId → TaskCompletionSource
        private readonly Dictionary<string, TaskCompletionSource<DynamoToPluginResponse>> _pendentes = new();
        private readonly object _lock = new();

        // Estado
        public DynamoConnectionStatus Status { get; private set; } = DynamoConnectionStatus.Disconnected;
        public int RequestsEnviados { get; private set; }
        public int RespostasRecebidas { get; private set; }

        // Eventos
        public event Action<PluginToDynamoContract>? RequestEnviado;
        public event Action<DynamoToPluginResponse>? RespostaRecebida;
        public event Action<string>? Erro;

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTOR
        // ══════════════════════════════════════════════════════════

        public DynamoBridgeService(
            string exchangeDir,
            int defaultTimeoutMs = 30000)
        {
            _exchangeDir = exchangeDir
                ?? throw new ArgumentNullException(nameof(exchangeDir));
            _defaultTimeoutMs = defaultTimeoutMs;

            _requestDir = Path.Combine(_exchangeDir, "requests");
            _responseDir = Path.Combine(_exchangeDir, "responses");

            // Garantir diretórios
            Directory.CreateDirectory(_requestDir);
            Directory.CreateDirectory(_responseDir);

            // Iniciar watcher de respostas
            IniciarWatcher();

            Status = DynamoConnectionStatus.Connected;
        }

        // ══════════════════════════════════════════════════════════
        //  ENVIAR COM RESPOSTA
        // ══════════════════════════════════════════════════════════

        public async Task<DynamoToPluginResponse> EnviarAsync(
            PluginToDynamoContract request,
            CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var tcs = new TaskCompletionSource<DynamoToPluginResponse>();

            // Registrar pendente
            lock (_lock)
            {
                _pendentes[request.RequestId] = tcs;
            }

            try
            {
                // Gravar request
                GravarRequest(request);
                RequestsEnviados++;
                RequestEnviado?.Invoke(request);

                // Aguardar resposta com timeout
                var timeoutMs = request.Payload?.Execution?.TimeoutMs
                    ?? _defaultTimeoutMs;

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutMs);

                var completedTask = await Task.WhenAny(
                    tcs.Task,
                    Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask == tcs.Task)
                    return await tcs.Task;

                // Timeout
                return DynamoToPluginResponse.Falha(
                    request.RequestId,
                    $"Timeout após {timeoutMs}ms aguardando resposta do Dynamo.",
                    "TIMEOUT");
            }
            catch (OperationCanceledException)
            {
                return DynamoToPluginResponse.Falha(
                    request.RequestId,
                    "Operação cancelada.",
                    "CANCELLED");
            }
            catch (Exception ex)
            {
                Erro?.Invoke(ex.Message);
                return DynamoToPluginResponse.Falha(
                    request.RequestId,
                    $"Erro ao enviar request: {ex.Message}",
                    "SEND_ERROR");
            }
            finally
            {
                lock (_lock)
                {
                    _pendentes.Remove(request.RequestId);
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ENVIAR SEM RESPOSTA
        // ══════════════════════════════════════════════════════════

        public Task EnviarSemRespostaAsync(
            PluginToDynamoContract request,
            CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.ExpectReturn = false;
            GravarRequest(request);
            RequestsEnviados++;
            RequestEnviado?.Invoke(request);

            return Task.CompletedTask;
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUTAR WORKSPACE
        // ══════════════════════════════════════════════════════════

        public async Task<DynamoToPluginResponse> ExecutarWorkspaceAsync(
            string workspacePath,
            List<InputData>? inputs = null,
            List<OutputRequest>? outputs = null,
            int timeoutMs = 30000,
            CancellationToken ct = default)
        {
            var request = PluginToDynamoContract.CriarRun(
                workspacePath, inputs, outputs, timeoutMs);

            return await EnviarAsync(request, ct);
        }

        // ══════════════════════════════════════════════════════════
        //  PING
        // ══════════════════════════════════════════════════════════

        public async Task<bool> PingAsync(CancellationToken ct = default)
        {
            try
            {
                var request = PluginToDynamoContract.CriarPing();
                request.Payload.Execution = new ExecutionData { TimeoutMs = 5000 };

                var response = await EnviarAsync(request, ct);
                return response.Result.Success;
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CARREGAR WORKSPACE
        // ══════════════════════════════════════════════════════════

        public Task CarregarWorkspaceAsync(
            string workspacePath,
            CancellationToken ct = default)
        {
            var request = PluginToDynamoContract.CriarLoad(workspacePath);
            return EnviarSemRespostaAsync(request, ct);
        }

        // ══════════════════════════════════════════════════════════
        //  DEFINIR INPUTS
        // ══════════════════════════════════════════════════════════

        public Task DefinirInputsAsync(
            string workspacePath,
            List<InputData> inputs,
            CancellationToken ct = default)
        {
            var request = PluginToDynamoContract.CriarSetInputs(
                workspacePath, inputs);
            return EnviarSemRespostaAsync(request, ct);
        }

        // ══════════════════════════════════════════════════════════
        //  LER OUTPUTS
        // ══════════════════════════════════════════════════════════

        public async Task<List<OutputResult>> LerOutputsAsync(
            string workspacePath,
            List<OutputRequest> outputs,
            CancellationToken ct = default)
        {
            var request = PluginToDynamoContract.CriarGetOutputs(
                workspacePath, outputs);

            var response = await EnviarAsync(request, ct);

            return response.Result.Success
                ? response.Result.Outputs ?? new()
                : new();
        }

        // ══════════════════════════════════════════════════════════
        //  TERMINAR
        // ══════════════════════════════════════════════════════════

        public Task TerminarAsync(CancellationToken ct = default)
        {
            var request = PluginToDynamoContract.CriarTerminate();
            return EnviarSemRespostaAsync(request, ct);
        }

        // ══════════════════════════════════════════════════════════
        //  FILE I/O
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Grava request como JSON no diretório de troca.
        /// Nome: {requestId}.json
        /// </summary>
        private void GravarRequest(PluginToDynamoContract request)
        {
            var fileName = $"{request.RequestId}.json";
            var filePath = Path.Combine(_requestDir, fileName);
            var json = request.ToJson(indented: true);

            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Lê response JSON do diretório de troca.
        /// </summary>
        private DynamoToPluginResponse? LerResponse(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                // Retry para file lock
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                        return DynamoToPluginResponse.FromJson(json);
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  FILE WATCHER
        // ══════════════════════════════════════════════════════════

        private void IniciarWatcher()
        {
            try
            {
                _responseWatcher = new FileSystemWatcher(_responseDir, "*.json")
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                _responseWatcher.Created += OnResponseFileCreated;
            }
            catch
            {
                // Watcher não disponível — fallback para polling
            }
        }

        private void OnResponseFileCreated(object sender, FileSystemEventArgs e)
        {
            // Aguardar escrita completa
            Thread.Sleep(50);

            var response = LerResponse(e.FullPath);
            if (response == null)
                return;

            RespostasRecebidas++;
            RespostaRecebida?.Invoke(response);

            // Resolver pendente
            lock (_lock)
            {
                if (_pendentes.TryGetValue(response.RequestId, out var tcs))
                {
                    tcs.TrySetResult(response);
                    _pendentes.Remove(response.RequestId);
                }
            }

            // Limpar arquivo processado
            try { File.Delete(e.FullPath); }
            catch { /* ignorar */ }
        }

        // ══════════════════════════════════════════════════════════
        //  LIMPEZA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Limpa arquivos antigos do diretório de troca.
        /// </summary>
        public void LimparArquivosAntigos(TimeSpan maxAge)
        {
            LimparDiretorio(_requestDir, maxAge);
            LimparDiretorio(_responseDir, maxAge);
        }

        private static void LimparDiretorio(string dir, TimeSpan maxAge)
        {
            try
            {
                var cutoff = DateTime.UtcNow - maxAge;
                var files = Directory.GetFiles(dir, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        if (File.GetCreationTimeUtc(file) < cutoff)
                            File.Delete(file);
                    }
                    catch { /* ignorar */ }
                }
            }
            catch { /* ignorar */ }
        }

        // ══════════════════════════════════════════════════════════
        //  DISPOSE
        // ══════════════════════════════════════════════════════════

        public void Dispose()
        {
            _responseWatcher?.Dispose();
            _responseWatcher = null;
            Status = DynamoConnectionStatus.Disconnected;

            // Cancelar pendentes
            lock (_lock)
            {
                foreach (var tcs in _pendentes.Values)
                {
                    tcs.TrySetCanceled();
                }
                _pendentes.Clear();
            }

            GC.SuppressFinalize(this);
        }

        public override string ToString() =>
            $"DynamoBridge: {Status} | " +
            $"{RequestsEnviados} enviados, {RespostasRecebidas} recebidos, " +
            $"{_pendentes.Count} pendentes";
    }
}
