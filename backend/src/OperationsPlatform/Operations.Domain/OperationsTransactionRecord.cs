namespace Operations.Domain;

public sealed class OperationsTransactionRecord
{
    public string TransactionId { get; set; } = string.Empty;
    public string Awid { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string? CardId { get; set; }
    public string Status { get; set; } = "COMPLETED";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "XOF";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<string> Timeline { get; set; } = new();
}
