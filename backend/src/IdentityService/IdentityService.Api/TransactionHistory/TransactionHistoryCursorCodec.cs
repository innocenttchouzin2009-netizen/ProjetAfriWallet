using System.Globalization;
using System.Text;
using AfriWallet.TransactionHistory.Application.Cursor;

namespace IdentityService.Api.TransactionHistory;

public static class TransactionHistoryCursorCodec
{
    private const string Version = "1";

    public static string Encode(TransactionHistoryCursor cursor)
    {
        var payload = string.Join(
            '|',
            Version,
            cursor.OccurredAtUtc.UtcTicks.ToString("D19", CultureInfo.InvariantCulture),
            cursor.TransactionId.ToString("N"));

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static TransactionHistoryCursor? Decode(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return null;
        }

        try
        {
            var base64 = encoded.Trim().Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = payload.Split('|');

            if (parts.Length != 3 ||
                parts[0] != Version ||
                !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var utcTicks) ||
                !Guid.TryParseExact(parts[2], "N", out var transactionId))
            {
                throw new FormatException();
            }

            return new TransactionHistoryCursor(
                new DateTimeOffset(utcTicks, TimeSpan.Zero),
                transactionId);
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or ArgumentOutOfRangeException)
        {
            throw new ArgumentException("Transaction history cursor is invalid.", nameof(encoded));
        }
    }
}
