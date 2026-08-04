using System.Text.RegularExpressions;

namespace MobileMoney.Production.Correlation;

public static partial class CorrelationIdValidator
{
    public const string DefaultCorrelationId = "correlation-id-default";

    public static string Normalize(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return DefaultCorrelationId;
        }

        var trimmed = correlationId.Trim();
        if (trimmed.Length > 128 || !ValidCorrelationIdRegex().IsMatch(trimmed))
        {
            return DefaultCorrelationId;
        }

        return trimmed;
    }

    public static string CreateOrGenerate(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return Generate();
        }

        var normalized = Normalize(correlationId);
        return normalized == DefaultCorrelationId ? DefaultCorrelationId : normalized;
    }

    public static string Generate()
    {
        return $"corr-{Guid.NewGuid():N}";
    }

    [GeneratedRegex("^(corr-[A-Za-z0-9._:-]+|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$")]
    private static partial Regex ValidCorrelationIdRegex();
}
