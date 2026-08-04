using UniversalWallet.Api.Payments.Domain.Timeline;

namespace UniversalWallet.Api.Payments.Application.Timeline;

public sealed class GetPaymentTimelineHandler
{
    private readonly IPaymentTimelineRepository _repository;

    public GetPaymentTimelineHandler(IPaymentTimelineRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPaymentTimelineResponse> HandleAsync(GetPaymentTimelineRequest request, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(
            ownerAwidId: null,
            direction: request.Direction,
            status: request.Status,
            type: request.Type,
            from: request.From,
            to: request.To,
            walletId: request.WalletId,
            cursor: request.Cursor,
            limit: request.Limit,
            cancellationToken: cancellationToken);

        return new GetPaymentTimelineResponse(items, null);
    }
}
