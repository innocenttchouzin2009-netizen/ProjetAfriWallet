using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;

public sealed class EfMobileMoneyPayoutFundingAttemptStore(
    MobileMoneyPayoutFundingDbContext dbContext)
    : IMobileMoneyPayoutFundingAttemptStore
{
    public async Task SaveAsync(
        FundingAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        await EnsureCreatedAsync(cancellationToken);

        var entity = await dbContext.Attempts.SingleOrDefaultAsync(
            x => x.Id == attempt.Id,
            cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException(
                "Funding attempts must be created through atomic plan persistence.");
        }

        ValidateImmutableAttempt(entity, attempt);
        entity.Status = (int)attempt.Status;
        entity.UpdatedAtUtc = attempt.UpdatedAtUtc;
        entity.StatusReason = attempt.StatusReason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<FundingAttempt>> SavePlanAsync(
        IReadOnlyList<FundingAttempt> attempts,
        CancellationToken cancellationToken = default) =>
        SavePlanCoreAsync(null, attempts, cancellationToken);

    public Task<IReadOnlyList<FundingAttempt>> SavePlanForOwnerAsync(
        Guid ownerUserId,
        IReadOnlyList<FundingAttempt> attempts,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));

        return SavePlanCoreAsync(ownerUserId, attempts, cancellationToken);
    }

    private async Task<IReadOnlyList<FundingAttempt>> SavePlanCoreAsync(
        Guid? ownerUserId,
        IReadOnlyList<FundingAttempt> attempts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempts);

        if (attempts.Count == 0)
        {
            throw new ArgumentException(
                "At least one funding attempt is required.",
                nameof(attempts));
        }

        await EnsureCreatedAsync(cancellationToken);

        var correlationId = attempts[0].CorrelationId;
        var plannedAtUtc = attempts[0].CreatedAtUtc;
        var currencyCode = attempts[0].Allocation.CurrencyCode;

        ValidateNewPlan(attempts, correlationId, plannedAtUtc, currencyCode);

        var existing = await LoadValidatedAsync(
            correlationId,
            cancellationToken);

        if (existing.Count > 0)
            return existing;

        dbContext.Plans.Add(new MobileMoneyPayoutFundingPlanEntity
        {
            CorrelationId = correlationId,
            OwnerUserId = ownerUserId,
            RequiredAmountMinor = attempts.Sum(x => x.Allocation.AmountMinor),
            CurrencyCode = currencyCode,
            AllocationCount = attempts.Count,
            PlannedAtUtc = plannedAtUtc
        });

        for (var index = 0; index < attempts.Count; index++)
        {
            dbContext.Attempts.Add(ToEntity(attempts[index], index));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return attempts.ToArray();
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            var authoritative = await LoadValidatedAsync(
                correlationId,
                cancellationToken);

            if (authoritative.Count > 0)
                return authoritative;

            throw;
        }
    }

    public async Task<Guid?> FindOwnerUserIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));

        await EnsureCreatedAsync(cancellationToken);

        var owner = await dbContext.Plans
            .AsNoTracking()
            .Where(x => x.CorrelationId == correlationId)
            .Select(x => x.OwnerUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (owner == Guid.Empty)
            throw new InvalidOperationException("Persisted funding owner is invalid.");

        return owner;
    }

    public async Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Correlation id cannot be empty.",
                nameof(correlationId));
        }

        await EnsureCreatedAsync(cancellationToken);
        return await LoadValidatedAsync(correlationId, cancellationToken);
    }

    private async Task<IReadOnlyList<FundingAttempt>> LoadValidatedAsync(
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.Plans
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CorrelationId == correlationId,
                cancellationToken);

        var entities = await dbContext.Attempts
            .AsNoTracking()
            .Where(x => x.CorrelationId == correlationId)
            .OrderBy(x => x.Sequence)
            .ToArrayAsync(cancellationToken);

        if (plan is null)
        {
            if (entities.Length != 0)
            {
                throw new InvalidOperationException(
                    "Persisted funding attempts are missing their authoritative plan record.");
            }

            return Array.Empty<FundingAttempt>();
        }

        if (plan.CorrelationId == Guid.Empty
            || plan.OwnerUserId == Guid.Empty
            || plan.RequiredAmountMinor <= 0
            || plan.AllocationCount <= 0
            || plan.PlannedAtUtc.Offset != TimeSpan.Zero
            || entities.Length != plan.AllocationCount)
        {
            throw new InvalidOperationException(
                "Persisted funding plan failed integrity checks.");
        }

        var normalizedCurrency =
            FundingAllocation.NormalizeCurrency(plan.CurrencyCode);

        var attempts = new FundingAttempt[entities.Length];
        long totalMinor = 0;

        for (var index = 0; index < entities.Length; index++)
        {
            var entity = entities[index];

            if (entity.Sequence != index
                || entity.CorrelationId != correlationId
                || !Enum.IsDefined(typeof(FundingSourceType), entity.SourceType)
                || !Enum.IsDefined(typeof(FundingAttemptStatus), entity.Status))
            {
                throw new InvalidOperationException(
                    "Persisted funding allocation sequence or enum values are invalid.");
            }

            var allocation = FundingAllocation.Create(
                entity.SourceId,
                (FundingSourceType)entity.SourceType,
                entity.AmountMinor,
                entity.CurrencyCode);

            if (!string.Equals(
                    allocation.CurrencyCode,
                    normalizedCurrency,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Persisted funding allocation currency does not match its plan.");
            }

            var attempt = FundingAttempt.Restore(
                entity.Id,
                entity.CorrelationId,
                allocation,
                (FundingAttemptStatus)entity.Status,
                entity.CreatedAtUtc,
                entity.UpdatedAtUtc,
                entity.StatusReason);

            if (attempt.CreatedAtUtc != plan.PlannedAtUtc)
            {
                throw new InvalidOperationException(
                    "Persisted funding attempt timestamp does not match its plan.");
            }

            totalMinor = checked(totalMinor + allocation.AmountMinor);
            attempts[index] = attempt;
        }

        if (totalMinor != plan.RequiredAmountMinor)
        {
            throw new InvalidOperationException(
                "Persisted funding plan monetary total is inconsistent.");
        }

        return attempts;
    }

    private static void ValidateNewPlan(
        IReadOnlyList<FundingAttempt> attempts,
        Guid correlationId,
        DateTimeOffset plannedAtUtc,
        string currencyCode)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty.", nameof(attempts));
        if (plannedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Funding plan timestamp must be UTC.", nameof(attempts));

        var normalizedCurrency = FundingAllocation.NormalizeCurrency(currencyCode);
        var sourceIds = new HashSet<string>(StringComparer.Ordinal);
        var attemptIds = new HashSet<Guid>();

        foreach (var attempt in attempts)
        {
            ArgumentNullException.ThrowIfNull(attempt);

            if (attempt.CorrelationId != correlationId
                || attempt.CreatedAtUtc != plannedAtUtc
                || attempt.UpdatedAtUtc != plannedAtUtc
                || attempt.Status != FundingAttemptStatus.Planned
                || attempt.StatusReason is not null
                || !string.Equals(
                    attempt.Allocation.CurrencyCode,
                    normalizedCurrency,
                    StringComparison.Ordinal)
                || !sourceIds.Add(attempt.Allocation.SourceId)
                || !attemptIds.Add(attempt.Id))
            {
                throw new InvalidOperationException(
                    "Funding plan attempts must form one unique complete planned set.");
            }
        }
    }

    private static MobileMoneyPayoutFundingAttemptEntity ToEntity(
        FundingAttempt attempt,
        int sequence) =>
        new()
        {
            Id = attempt.Id,
            CorrelationId = attempt.CorrelationId,
            Sequence = sequence,
            SourceId = attempt.Allocation.SourceId,
            SourceType = (int)attempt.Allocation.SourceType,
            AmountMinor = attempt.Allocation.AmountMinor,
            CurrencyCode = attempt.Allocation.CurrencyCode,
            Status = (int)attempt.Status,
            CreatedAtUtc = attempt.CreatedAtUtc,
            UpdatedAtUtc = attempt.UpdatedAtUtc,
            StatusReason = attempt.StatusReason
        };

    private static void ValidateImmutableAttempt(
        MobileMoneyPayoutFundingAttemptEntity entity,
        FundingAttempt attempt)
    {
        if (entity.CorrelationId != attempt.CorrelationId
            || entity.SourceId != attempt.Allocation.SourceId
            || entity.SourceType != (int)attempt.Allocation.SourceType
            || entity.AmountMinor != attempt.Allocation.AmountMinor
            || entity.CurrencyCode != attempt.Allocation.CurrencyCode
            || entity.CreatedAtUtc != attempt.CreatedAtUtc)
        {
            throw new InvalidOperationException(
                "Persisted funding attempt immutable material cannot be changed.");
        }
    }

    private Task EnsureCreatedAsync(CancellationToken cancellationToken) =>
        dbContext.Database.EnsureCreatedAsync(cancellationToken);
}
