namespace MobileMoney.Production.Payout.Application;

public sealed class MobileMoneyPayoutSubmissionResult
{
    private MobileMoneyPayoutSubmissionResult(
        bool isAccepted,
        string? providerReference,
        string? failureCode)
    {
        IsAccepted = isAccepted;
        ProviderReference = providerReference;
        FailureCode = failureCode;
    }

    public bool IsAccepted { get; }
    public string? ProviderReference { get; }
    public string? FailureCode { get; }

    public static MobileMoneyPayoutSubmissionResult Success(string providerReference)
    {
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new ArgumentException(
                "Provider reference is required.",
                nameof(providerReference));

        return new MobileMoneyPayoutSubmissionResult(
            true,
            providerReference.Trim(),
            null);
    }

    public static MobileMoneyPayoutSubmissionResult Rejected(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException(
                "Failure code is required.",
                nameof(failureCode));

        return new MobileMoneyPayoutSubmissionResult(
            false,
            null,
            failureCode.Trim());
    }
}
