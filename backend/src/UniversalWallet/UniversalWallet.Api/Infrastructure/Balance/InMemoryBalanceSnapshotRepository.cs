using UniversalWallet.Api.Domain.Balance;

namespace UniversalWallet.Api.Infrastructure.Balance;

public sealed class InMemoryBalanceSnapshotRepository : IBalanceSnapshotRepository
{
	private readonly object _sync = new();
	private readonly Dictionary<Guid, BalanceSnapshot> _snapshots = new();

	public BalanceSnapshot? GetLatest(Guid walletId)
	{
		lock (_sync)
		{
			return _snapshots.GetValueOrDefault(walletId);
		}
	}

	public void Save(BalanceSnapshot snapshot)
	{
		lock (_sync)
		{
			_snapshots[snapshot.WalletId] = snapshot;
		}
	}
}
