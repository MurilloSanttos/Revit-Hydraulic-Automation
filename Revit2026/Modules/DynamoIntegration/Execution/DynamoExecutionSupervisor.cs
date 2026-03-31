using System.Diagnostics;

namespace Revit2026.Modules.DynamoIntegration.Execution
{
    public class DynamoSupervisionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool TimeoutOccurred { get; set; }
        public bool Cancelled { get; set; }
        public long ElapsedMs { get; set; }
        public Exception? InnerException { get; set; }

        public static DynamoSupervisionResult Ok(long elapsedMs) =>
            new() { Success = true, ElapsedMs = elapsedMs };

        public static DynamoSupervisionResult Timeout(int timeoutMs, long elapsedMs) =>
            new()
            {
                Success = false,
                TimeoutOccurred = true,
                ElapsedMs = elapsedMs,
                ErrorMessage = $"Execution timed out after {timeoutMs} ms."
            };

        public static DynamoSupervisionResult Error(Exception ex, long elapsedMs) =>
            new()
            {
                Success = false,
                ElapsedMs = elapsedMs,
                ErrorMessage = UnwrapMessage(ex),
                InnerException = Unwrap(ex)
            };

        public static DynamoSupervisionResult Cancel(long elapsedMs) =>
            new()
            {
                Success = false,
                Cancelled = true,
                ElapsedMs = elapsedMs,
                ErrorMessage = "Execution was cancelled."
            };

        private static Exception Unwrap(Exception ex)
        {
            if (ex is AggregateException agg && agg.InnerExceptions.Count == 1)
                return Unwrap(agg.InnerExceptions[0]);
            return ex.InnerException != null ? Unwrap(ex.InnerException) : ex;
        }

        private static string UnwrapMessage(Exception ex)
        {
            var inner = Unwrap(ex);
            return $"{inner.GetType().Name}: {inner.Message}";
        }

        public override string ToString() =>
            TimeoutOccurred ? $"TIMEOUT ({ElapsedMs}ms)"
            : Cancelled ? $"CANCELLED ({ElapsedMs}ms)"
            : Success ? $"OK ({ElapsedMs}ms)"
            : $"ERROR: {ErrorMessage} ({ElapsedMs}ms)";
    }

    public class SupervisorConfig
    {
        public int DefaultTimeoutMs { get; set; } = 60000;
        public int MinTimeoutMs { get; set; } = 1000;
        public int MaxTimeoutMs { get; set; } = 600000;
        public int GracePeriodMs { get; set; } = 2000;
        public int MaxRetries { get; set; } = 0;
        public int RetryDelayMs { get; set; } = 1000;
    }

    public class DynamoExecutionSupervisor
    {
        private readonly SupervisorConfig _config;

        public int TotalExecutions { get; private set; }
        public int TotalTimeouts { get; private set; }
        public int TotalErrors { get; private set; }
        public int TotalSuccesses { get; private set; }

        public event Action<DynamoSupervisionResult>? ExecutionCompleted;
        public event Action<int>? TimeoutOccurred;
        public event Action<Exception>? ErrorOccurred;

        public DynamoExecutionSupervisor()
        {
            _config = new SupervisorConfig();
        }

        public DynamoExecutionSupervisor(SupervisorConfig config)
        {
            _config = config ?? new SupervisorConfig();
        }

        // ══════════════════════════════════════════════════════════
        //  SUPERVISE ASYNC
        // ══════════════════════════════════════════════════════════

        public async Task<DynamoSupervisionResult> SuperviseAsync(
            Task executionTask,
            int timeoutMilliseconds,
            CancellationTokenSource cts)
        {
            if (executionTask == null)
                throw new ArgumentNullException(nameof(executionTask));
            if (cts == null)
                throw new ArgumentNullException(nameof(cts));

            timeoutMilliseconds = ClampTimeout(timeoutMilliseconds);

            var sw = Stopwatch.StartNew();
            TotalExecutions++;

            try
            {
                var timeoutTask = Task.Delay(timeoutMilliseconds, cts.Token);
                var completedTask = await Task.WhenAny(executionTask, timeoutTask);

                if (completedTask == timeoutTask && !executionTask.IsCompleted)
                {
                    TotalTimeouts++;

                    try { cts.Cancel(); }
                    catch (ObjectDisposedException) { }

                    await WaitGracePeriod(executionTask);

                    var result = DynamoSupervisionResult.Timeout(
                        timeoutMilliseconds, sw.ElapsedMilliseconds);

                    TimeoutOccurred?.Invoke(timeoutMilliseconds);
                    ExecutionCompleted?.Invoke(result);

                    return result;
                }

                if (executionTask.IsCanceled)
                {
                    var result = DynamoSupervisionResult.Cancel(
                        sw.ElapsedMilliseconds);
                    ExecutionCompleted?.Invoke(result);
                    return result;
                }

                if (executionTask.IsFaulted)
                {
                    TotalErrors++;

                    var ex = executionTask.Exception
                        ?? new Exception("Unknown execution fault.");

                    var result = DynamoSupervisionResult.Error(
                        ex, sw.ElapsedMilliseconds);

                    ErrorOccurred?.Invoke(ex);
                    ExecutionCompleted?.Invoke(result);

                    return result;
                }

                TotalSuccesses++;

                var successResult = DynamoSupervisionResult.Ok(
                    sw.ElapsedMilliseconds);
                ExecutionCompleted?.Invoke(successResult);

                return successResult;
            }
            catch (OperationCanceledException)
            {
                var result = DynamoSupervisionResult.Cancel(
                    sw.ElapsedMilliseconds);
                ExecutionCompleted?.Invoke(result);
                return result;
            }
            catch (Exception ex)
            {
                TotalErrors++;

                var result = DynamoSupervisionResult.Error(
                    ex, sw.ElapsedMilliseconds);

                ErrorOccurred?.Invoke(ex);
                ExecutionCompleted?.Invoke(result);

                return result;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SUPERVISE COM RETRY
        // ══════════════════════════════════════════════════════════

        public async Task<DynamoSupervisionResult> SuperviseWithRetryAsync(
            Func<CancellationToken, Task> executionFactory,
            int timeoutMilliseconds,
            int? maxRetries = null)
        {
            var retries = maxRetries ?? _config.MaxRetries;
            DynamoSupervisionResult? lastResult = null;

            for (int attempt = 0; attempt <= retries; attempt++)
            {
                using var cts = new CancellationTokenSource();

                var executionTask = executionFactory(cts.Token);
                lastResult = await SuperviseAsync(
                    executionTask, timeoutMilliseconds, cts);

                if (lastResult.Success)
                    return lastResult;

                if (lastResult.Cancelled)
                    return lastResult;

                if (attempt < retries)
                    await Task.Delay(_config.RetryDelayMs);
            }

            return lastResult ?? DynamoSupervisionResult.Error(
                new Exception("All retry attempts failed."), 0);
        }

        // ══════════════════════════════════════════════════════════
        //  SUPERVISE TYPED
        // ══════════════════════════════════════════════════════════

        public async Task<(DynamoSupervisionResult Supervision, T? Result)>
            SuperviseAsync<T>(
                Task<T> executionTask,
                int timeoutMilliseconds,
                CancellationTokenSource cts)
        {
            var supervision = await SuperviseAsync(
                (Task)executionTask, timeoutMilliseconds, cts);

            if (supervision.Success && executionTask.IsCompletedSuccessfully)
                return (supervision, executionTask.Result);

            return (supervision, default);
        }

        // ══════════════════════════════════════════════════════════
        //  SUPERVISE SÍNCRONO
        // ══════════════════════════════════════════════════════════

        public DynamoSupervisionResult Supervise(
            Task executionTask,
            int timeoutMilliseconds,
            CancellationTokenSource cts)
        {
            return SuperviseAsync(executionTask, timeoutMilliseconds, cts)
                .GetAwaiter()
                .GetResult();
        }

        // ══════════════════════════════════════════════════════════
        //  SUPERVISE COM ACTION
        // ══════════════════════════════════════════════════════════

        public async Task<DynamoSupervisionResult> SuperviseAsync(
            Action action,
            int timeoutMilliseconds)
        {
            using var cts = new CancellationTokenSource();

            var executionTask = Task.Run(() =>
            {
                cts.Token.ThrowIfCancellationRequested();
                action();
            }, cts.Token);

            return await SuperviseAsync(executionTask, timeoutMilliseconds, cts);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private int ClampTimeout(int timeoutMs)
        {
            if (timeoutMs <= 0)
                return _config.DefaultTimeoutMs;

            return Math.Clamp(timeoutMs, _config.MinTimeoutMs, _config.MaxTimeoutMs);
        }

        private async Task WaitGracePeriod(Task executionTask)
        {
            if (_config.GracePeriodMs <= 0)
                return;

            try
            {
                await Task.WhenAny(
                    executionTask,
                    Task.Delay(_config.GracePeriodMs));
            }
            catch { /* grace period best-effort */ }
        }

        public void ResetCounters()
        {
            TotalExecutions = 0;
            TotalTimeouts = 0;
            TotalErrors = 0;
            TotalSuccesses = 0;
        }

        public override string ToString() =>
            $"Supervisor: {TotalExecutions} total, " +
            $"{TotalSuccesses} ok, {TotalTimeouts} timeouts, " +
            $"{TotalErrors} errors";
    }
}
