using UniversalWallet.Api.Domain.Balance;

namespace UniversalWallet.Api.Infrastructure.Balance;

public sealed class InMemoryProjectionVersionRepository : IProjectionVersionRepository
{
	private readonly object _sync = new();
	private readonly Dictionary<Guid, ProjectionVersion> _versions = new();

	public ProjectionVersion? Get(Guid walletId)
	{
		lock (_sync)
		{
			return _versions.GetValueOrDefault(walletId);
		}
	}

	public ProjectionVersion Increment(Guid walletId, long ledgerPosition)
	{
		lock (_sync)
		{
			var current = _versions.GetValueOrDefault(walletId);
			var next = new ProjectionVersion(
				walletId,
				ledgerPosition,
				(current?.Version ?? 0) + 1,
				DateTimeOffset.UtcNow);

			_versions[walletId] = next;
			return next;
		}
	}
}
