namespace Operations.Domain;

public sealed class OperationsWalletRecord
{
    public string WalletId { get; set; } = string.Empty;
    public string Awid { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? SuspendedAtUtc { get; set; }
}
