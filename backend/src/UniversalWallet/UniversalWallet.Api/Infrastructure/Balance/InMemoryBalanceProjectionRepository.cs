using UniversalWallet.Api.Domain.Balance;

namespace UniversalWallet.Api.Infrastructure.Balance;

public sealed class InMemoryBalanceProjectionRepository : IBalanceProjectionRepository
{
	private readonly object _sync = new();
	private readonly Dictionary<Guid, WalletBalanceProjection> _projections = new();

	public WalletBalanceProjection? Get(Guid walletId)
	{
		lock (_sync)
		{
			return _projections.GetValueOrDefault(walletId);
		}
	}

	public void Upsert(WalletBalanceProjection projection)
	{
		lock (_sync)
		{
			_projections[projection.WalletId] = projection;
		}
	}
}
