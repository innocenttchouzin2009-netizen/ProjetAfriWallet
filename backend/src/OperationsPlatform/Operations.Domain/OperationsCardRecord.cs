namespace Operations.Domain;

public sealed class OperationsCardRecord
{
    public string CardId { get; set; } = string.Empty;
    public string Awid { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? FrozenAtUtc { get; set; }
}
