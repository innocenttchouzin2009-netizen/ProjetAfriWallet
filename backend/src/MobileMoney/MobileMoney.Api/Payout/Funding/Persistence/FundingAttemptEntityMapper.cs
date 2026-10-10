using System.Globalization;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Persistence;

internal static class FundingAttemptEntityMapper
{
    public static FundingAttemptEntity ToEntity(FundingAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        var entity = new FundingAttemptEntity();
        Apply(entity, attempt);
        return entity;
    }

    public static void Apply(
        FundingAttemptEntity entity,
        FundingAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(attempt);

        entity.Id = attempt.Id;
        entity.CorrelationId = attempt.CorrelationId;
        entity.SourceId = attempt.Allocation.SourceId;
        entity.SourceType = (int)attempt.Allocation.SourceType;
        entity.AmountMinor = attempt.Allocation.AmountMinor;
        entity.CurrencyCode = attempt.Allocation.CurrencyCode;
        entity.Status = (int)attempt.Status;
        entity.CreatedAtUtc = FormatTimestamp(attempt.CreatedAtUtc);
        entity.UpdatedAtUtc = FormatTimestamp(attempt.UpdatedAtUtc);
        entity.StatusReason = attempt.StatusReason;
    }

    public static FundingAttempt ToDomain(FundingAttemptEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var allocation = FundingAllocation.Create(
            entity.SourceId,
            (FundingSourceType)entity.SourceType,
            entity.AmountMinor,
            entity.CurrencyCode);

        return FundingAttempt.Restore(
            entity.Id,
            entity.CorrelationId,
            allocation,
            (FundingAttemptStatus)entity.Status,
            ParseTimestamp(entity.CreatedAtUtc, nameof(entity.CreatedAtUtc)),
            ParseTimestamp(entity.UpdatedAtUtc, nameof(entity.UpdatedAtUtc)),
            entity.StatusReason);
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseTimestamp(
        string value,
        string fieldName)
    {
        if (!DateTimeOffset.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            throw new InvalidOperationException(
                "Persisted funding attempt timestamp is invalid: " + fieldName + ".");
        }

        return parsed;
    }
}
