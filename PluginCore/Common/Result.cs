namespace PluginCore.Common;

/// <summary>
/// Resultado genérico de operação. Elimina exceções como fluxo de controle.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string ErrorMessage { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string errorMessage, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static Result<T> Success(T value) =>
        new(true, value, string.Empty, null);

    public static Result<T> Failure(string error, Exception? ex = null) =>
        new(false, default, error, ex);
}

/// <summary>
/// Resultado sem valor de retorno (operações void).
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, string errorMessage, Exception? exception)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static Result Success() =>
        new(true, string.Empty, null);

    public static Result Failure(string error, Exception? ex = null) =>
        new(false, error, ex);
}
