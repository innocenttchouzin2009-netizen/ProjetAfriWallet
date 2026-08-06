namespace Operations.Domain;

public sealed class OperationsUserRecord
{
    public string Awid { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EmailMasked { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string RiskLabel { get; set; } = "LOW";
    public string SupportSummary { get; set; } = string.Empty;
}
