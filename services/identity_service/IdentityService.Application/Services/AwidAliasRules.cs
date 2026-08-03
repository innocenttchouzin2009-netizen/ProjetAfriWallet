using System.Text.RegularExpressions;

namespace IdentityService.Application.Services;

public static partial class AwidAliasRules
{
    private static readonly Regex AliasRegex = BuildAliasRegex();

    public static string Normalize(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return string.Empty;
        }

        var normalized = alias.Trim().ToLowerInvariant();
        if (normalized.StartsWith("@", StringComparison.Ordinal))
        {
            normalized = normalized[1..];
        }

        return normalized;
    }

    public static bool IsValidCanonical(string aliasCanonical)
    {
        return AliasRegex.IsMatch(aliasCanonical);
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{2,29}$", RegexOptions.CultureInvariant)]
    private static partial Regex BuildAliasRegex();
}
