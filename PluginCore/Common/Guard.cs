namespace PluginCore.Common;

/// <summary>
/// Guard clauses para validação de parâmetros.
/// </summary>
public static class Guard
{
    public static void NotNull<T>(T value, string paramName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    public static void NotNullOrEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty.", paramName);
    }

    public static void Positive(double value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive.");
    }

    public static void InRange(double value, double min, double max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName,
                $"Value {value} must be between {min} and {max}.");
    }
}
