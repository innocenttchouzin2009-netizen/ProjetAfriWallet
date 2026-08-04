using UniversalWallet.Api.Payments.Application.Timeline;

namespace UniversalWallet.Api.Payments.Api.Timeline;

public static class PaymentTimelineEndpoints
{
    public static WebApplication MapPaymentTimelineEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/payments/timeline", async ([AsParameters] GetPaymentTimelineRequest request, GetPaymentTimelineHandler handler) =>
        {
            var response = await handler.HandleAsync(request);
            return Results.Ok(new { items = response.Items, nextCursor = response.NextCursor });
        });

        app.MapGet("/api/v1/payments/lookup", async (string reference, LookupPaymentTimelineHandler handler) =>
        {
            var response = await handler.HandleAsync(new LookupPaymentTimelineRequest(reference));
            return Results.Ok(new { items = response.Items });
        });

        return app;
    }
}
