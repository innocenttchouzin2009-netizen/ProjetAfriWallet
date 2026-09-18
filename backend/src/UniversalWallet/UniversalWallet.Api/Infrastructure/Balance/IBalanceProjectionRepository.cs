using UniversalWallet.Api.Domain.Balance;

namespace UniversalWallet.Api.Infrastructure.Balance;

public interface IBalanceProjectionRepository
{
	WalletBalanceProjection? Get(Guid walletId);
	void Upsert(WalletBalanceProjection projection);
}
