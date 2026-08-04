using UniversalWallet.Api.Payments.Application.Timeline;
using UniversalWallet.Api.Payments.Domain.Timeline;

namespace UniversalWallet.Api.Payments.Infrastructure.Timeline;

public sealed class InMemoryTimelineItemRepository : IPaymentTimelineRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, PaymentTimelineItem> _items = new();
    private readonly Dictionary<string, Guid> _byReference = new(StringComparer.OrdinalIgnoreCase);

    public Task<PaymentTimelineItem?> GetAsync(Guid timelineId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_items.TryGetValue(timelineId, out var item) ? item : null);
        }
    }

    public Task<IReadOnlyList<PaymentTimelineItem>> ListAsync(Guid? ownerAwidId, string? direction, string? status, string? type, DateTimeOffset? from, DateTimeOffset? to, Guid? walletId, string? cursor, int? limit, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var filtered = _items.Values
                .Where(item => ownerAwidId is null || item.OwnerAwidId == ownerAwidId)
                .Where(item => string.IsNullOrWhiteSpace(direction) || string.Equals(item.Direction.ToString(), direction, StringComparison.OrdinalIgnoreCase))
                .Where(item => string.IsNullOrWhiteSpace(status) || string.Equals(item.Status.ToString(), status, StringComparison.OrdinalIgnoreCase))
                .Where(item => string.IsNullOrWhiteSpace(type) || string.Equals(item.Type.ToString(), type, StringComparison.OrdinalIgnoreCase))
                .Where(item => from is null || item.OccurredAt >= from)
                .Where(item => to is null || item.OccurredAt <= to)
                .OrderByDescending(item => item.OccurredAt)
                .ThenByDescending(item => item.ProjectionVersion)
                .Take(limit ?? 20)
                .ToList();

            return Task.FromResult<IReadOnlyList<PaymentTimelineItem>>(filtered);
        }
    }

    public Task AddOrUpdateAsync(PaymentTimelineItem item, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _items[item.TimelineId] = item;
            if (!string.IsNullOrWhiteSpace(item.PublicReference))
            {
                _byReference[item.PublicReference] = item.TimelineId;
            }

            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<PaymentTimelineItem>> FindByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return Task.FromResult<IReadOnlyList<PaymentTimelineItem>>(Array.Empty<PaymentTimelineItem>());
            }

            if (_byReference.TryGetValue(reference, out var timelineId) && _items.TryGetValue(timelineId, out var item))
            {
                return Task.FromResult<IReadOnlyList<PaymentTimelineItem>>(new[] { item });
            }

            var matches = _items.Values
                .Where(item => !string.IsNullOrWhiteSpace(item.PublicReference) && item.PublicReference.Contains(reference, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.OccurredAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<PaymentTimelineItem>>(matches);
        }
    }
}
