using UniversalWallet.Api.Domain.Balance;

namespace UniversalWallet.Api.Infrastructure.Balance;

public interface IProjectionVersionRepository
{
	ProjectionVersion? Get(Guid walletId);
	ProjectionVersion Increment(Guid walletId, long ledgerPosition);
}
