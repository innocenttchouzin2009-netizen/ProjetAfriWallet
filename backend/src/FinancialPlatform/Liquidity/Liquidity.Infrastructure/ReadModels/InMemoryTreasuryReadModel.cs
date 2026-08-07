using Liquidity.Application.Interfaces;

namespace Liquidity.Infrastructure.ReadModels;

public sealed class InMemoryTreasuryReadModel : ITreasuryReadModel
{
    private readonly List<TreasuryLiquidityAccountState> _states;

    public InMemoryTreasuryReadModel()
    {
        _states =
        [
            new TreasuryLiquidityAccountState(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "XAF",
                7_000_000,
                500_000,
                500_000,
                0),
            new TreasuryLiquidityAccountState(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "XAF",
                2_000_000,
                100_000,
                900_000,
                100_000),
            new TreasuryLiquidityAccountState(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "XAF",
                1_200_000,
                100_000,
                200_000,
                300_000),
            new TreasuryLiquidityAccountState(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "USD",
                2_000_000,
                300_000,
                100_000,
                0)
        ];
    }

    public Task<IReadOnlyCollection<TreasuryLiquidityAccountState>> GetAccountStatesAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<TreasuryLiquidityAccountState>>(_states.ToArray());
    }
}
