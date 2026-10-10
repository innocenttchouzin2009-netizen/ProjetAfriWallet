using Microsoft.EntityFrameworkCore;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Persistence;

public sealed class EfMobileMoneyPayoutFundingAttemptStore(
    FundingAttemptDbContext dbContext)
    : IMobileMoneyPayoutFundingAttemptStore
{
    public async Task SaveAsync(
        FundingAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.FundingAttempts
            .SingleOrDefaultAsync(
                x => x.Id == attempt.Id,
                cancellationToken);

        if (entity is null)
        {
            dbContext.FundingAttempts.Add(
                FundingAttemptEntityMapper.ToEntity(attempt));
        }
        else
        {
            FundingAttemptEntityMapper.Apply(entity, attempt);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "Funding attempt persistence violated a durable storage constraint.",
                exception);
        }
    }

    public async Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Correlation id cannot be empty.",
                nameof(correlationId));
        }

        var entities = await dbContext.FundingAttempts
            .AsNoTracking()
            .Where(x => x.CorrelationId == correlationId)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities
            .Select(FundingAttemptEntityMapper.ToDomain)
            .ToArray();
    }
}
