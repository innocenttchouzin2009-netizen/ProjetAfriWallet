using System.Text.RegularExpressions;

namespace MobileMoney.Production.Logging;

public static partial class SensitiveDataRedactor
{
    public static string Redact(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var redacted = input;
        redacted = BearerTokenPattern().Replace(redacted, "[REDACTED]");
        redacted = HeaderValuePattern().Replace(redacted, "$1=[REDACTED]");
        redacted = KeyValuePattern().Replace(redacted, "$1=[REDACTED]");
        redacted = PhoneNumberPattern().Replace(redacted, "[PHONE_REDACTED]");

        return redacted;
    }

    [GeneratedRegex(@"(?i)\b(bearer)\s+([A-Za-z0-9._\-]+)")]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(@"(?i)(accessToken|apiKey|subscriptionKey|callbackSecret|authorization|password|pin)\s*[:=]\s*([^\s,;]+)")]
    private static partial Regex HeaderValuePattern();

    [GeneratedRegex(@"(?i)([A-Za-z0-9._-]+)\s*=\s*([^\s,;]+)")]
    private static partial Regex KeyValuePattern();

    [GeneratedRegex(@"\b(?:\+?237|0)?[67]\d{8}\b")]
    private static partial Regex PhoneNumberPattern();
}
