using Settlement.Application.Interfaces;
using Settlement.Domain.Instructions;
using Settlement.Domain.Positions;

namespace Settlement.Application.Services;

public sealed class SettlementPositionService
{
    private readonly ISettlementRepository _repository;

    public SettlementPositionService(ISettlementRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<SettlementPosition>> GetPositionsAsync(CancellationToken cancellationToken)
    {
        var instructions = await _repository.GetInstructionsAsync(cancellationToken);

        return instructions
            .GroupBy(x => x.SourceCurrency)
            .Select(group =>
            {
                var settledDebits = group
                    .Where(x => x.Status == SettlementInstructionStatus.Settled)
                    .Sum(x => x.SourceAmountMinor);

                var settledCredits = instructions
                    .Where(x => x.Status == SettlementInstructionStatus.Settled && x.DestinationCurrency == group.Key)
                    .Sum(x => x.DestinationAmountMinor);

                var pendingDebits = group
                    .Where(x => x.Status == SettlementInstructionStatus.Pending)
                    .Sum(x => x.SourceAmountMinor);

                var pendingCredits = instructions
                    .Where(x => x.Status == SettlementInstructionStatus.Pending && x.DestinationCurrency == group.Key)
                    .Sum(x => x.DestinationAmountMinor);

                return new SettlementPosition(
                    group.Key,
                    settledDebits,
                    settledCredits,
                    pendingDebits,
                    pendingCredits,
                    settledCredits - settledDebits,
                    DateTime.UtcNow);
            })
            .OrderBy(x => x.CurrencyCode)
            .ToArray();
    }
}
