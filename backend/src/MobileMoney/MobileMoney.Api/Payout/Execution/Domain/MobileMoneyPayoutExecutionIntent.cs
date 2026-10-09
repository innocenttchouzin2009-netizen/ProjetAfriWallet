using System.Security.Cryptography;
using System.Text;
using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionIntent
{
    private const string FingerprintVersion = "v1";

    private MobileMoneyPayoutExecutionIntent(
        Guid quoteId,
        string sourceWalletId,
        MobileMoneyBeneficiary beneficiary,
        string idempotencyKey,
        string fingerprint)
    {
        QuoteId = quoteId;
        SourceWalletId = sourceWalletId;
        Beneficiary = beneficiary;
        IdempotencyKey = idempotencyKey;
        Fingerprint = fingerprint;
    }

    public Guid QuoteId { get; }
    public string SourceWalletId { get; }
    public MobileMoneyBeneficiary Beneficiary { get; }
    public string IdempotencyKey { get; }
    public string Fingerprint { get; }

    public static MobileMoneyPayoutExecutionIntent Create(
        Guid quoteId,
        string sourceWalletId,
        MobileMoneyBeneficiary beneficiary,
        string idempotencyKey)
    {
        if (quoteId == Guid.Empty)
            throw new ArgumentException("Quote id is required.", nameof(quoteId));
        if (string.IsNullOrWhiteSpace(sourceWalletId))
            throw new ArgumentException(
                "Source wallet id is required.",
                nameof(sourceWalletId));
        ArgumentNullException.ThrowIfNull(beneficiary);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));

        var normalizedWalletId = sourceWalletId.Trim();
        var normalizedIdempotencyKey = idempotencyKey.Trim();
        var canonical = string.Join(
            "\n",
            FingerprintVersion,
            quoteId.ToString("N"),
            normalizedWalletId,
            beneficiary.Msisdn,
            beneficiary.CountryCode,
            beneficiary.OperatorCode);

        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

        return new(
            quoteId,
            normalizedWalletId,
            beneficiary,
            normalizedIdempotencyKey,
            fingerprint);
    }
}
