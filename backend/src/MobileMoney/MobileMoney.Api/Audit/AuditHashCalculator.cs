using System.Security.Cryptography;
using System.Text;

namespace MobileMoney.Production.Audit;

public static class AuditHashCalculator
{
    public static string Calculate(string? previousHash, string auditId, AuditAction action, AuditResult result, DateTime timestampUtc)
    {
        var payload = string.Join("|", new[]
        {
            previousHash ?? string.Empty,
            timestampUtc.ToString("O"),
            auditId,
            action.ToString(),
            result.ToString()
        });

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
