namespace AfriWallet.Compliance.Screening.Domain.Entries;

public sealed record ScreeningSource(
    string Code,
    string DisplayName,
    bool Sandbox);