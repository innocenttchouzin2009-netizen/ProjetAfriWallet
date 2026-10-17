using UniversalWallet.Api.Domain.Balance;

namespace UniversalWallet.Api.Infrastructure.Balance;

public interface IBalanceSnapshotRepository
{
	BalanceSnapshot? GetLatest(Guid walletId);
	void Save(BalanceSnapshot snapshot);
}
