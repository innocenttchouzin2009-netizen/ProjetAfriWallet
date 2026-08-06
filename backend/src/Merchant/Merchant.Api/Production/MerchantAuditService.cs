namespace AfriWallet.Merchant.Api.Production;

public sealed class MerchantAuditService
{
    private readonly List<string> _entries = [];

    public void Record(string action, string subjectId, string? correlationId = null, string? merchantId = null, string? settlementId = null, string? posTerminalId = null, string? qrReference = null)
    {
        _entries.Add($"{action}|{subjectId}|{correlationId}|{merchantId}|{settlementId}|{posTerminalId}|{qrReference}");
    }

    public IReadOnlyList<string> GetEntries() => _entries;
}
