namespace UniversalWallet.Api.Payments.Application.Timeline;

public sealed class LookupPaymentTimelineHandler
{
    private readonly IPaymentTimelineRepository _repository;

    public LookupPaymentTimelineHandler(IPaymentTimelineRepository repository)
    {
        _repository = repository;
    }

    public async Task<LookupPaymentTimelineResponse> HandleAsync(LookupPaymentTimelineRequest request, CancellationToken cancellationToken = default)
    {
        var items = await _repository.FindByReferenceAsync(request.Reference, cancellationToken);
        return new LookupPaymentTimelineResponse(items);
    }
}
