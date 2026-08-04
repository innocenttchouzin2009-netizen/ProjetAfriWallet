using UniversalWallet.Api.Payments.Domain.Timeline;

namespace UniversalWallet.Api.Payments.Application.Timeline;

public interface IPaymentTimelineRepository
{
    Task<PaymentTimelineItem?> GetAsync(Guid timelineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentTimelineItem>> ListAsync(Guid? ownerAwidId, string? direction, string? status, string? type, DateTimeOffset? from, DateTimeOffset? to, Guid? walletId, string? cursor, int? limit, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(PaymentTimelineItem item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentTimelineItem>> FindByReferenceAsync(string reference, CancellationToken cancellationToken = default);
}

public sealed record GetPaymentTimelineRequest(
    string? Cursor = null,
    int? Limit = 20,
    string? Direction = null,
    string? Status = null,
    string? Type = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? WalletId = null);

public sealed record GetPaymentTimelineResponse(IReadOnlyList<PaymentTimelineItem> Items, string? NextCursor);

public sealed record LookupPaymentTimelineRequest(string Reference);

public sealed record LookupPaymentTimelineResponse(IReadOnlyList<PaymentTimelineItem> Items);
