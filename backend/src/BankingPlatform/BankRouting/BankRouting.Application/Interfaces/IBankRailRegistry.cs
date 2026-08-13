using BankRouting.Domain.Rails;

namespace BankRouting.Application.Interfaces;

public interface IBankRailRegistry
{
    Task<IReadOnlyCollection<BankRail>> ListAsync(CancellationToken cancellationToken);
}
