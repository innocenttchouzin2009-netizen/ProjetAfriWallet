namespace AfriWallet.Banking.Api.Production.Configuration;

public sealed class BankingProductionOptions
{
    public const string SectionName = "Banking:Production";

    public bool Enabled { get; set; }
    public string Environment { get; set; } = "Sandbox";
    public string[] RequiredSettings { get; set; } = Array.Empty<string>();
    public string[] RequiredSecrets { get; set; } = Array.Empty<string>();
    public string[] PartnerEndpoints { get; set; } = Array.Empty<string>();
}
