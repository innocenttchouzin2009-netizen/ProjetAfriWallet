using UniversalWallet.Api.Payments.Application.Settlements;

namespace UniversalWallet.Api.Payments.Api.Settlements;

public static class PaymentSettlementEndpoints
{
    public static WebApplication MapPaymentSettlementEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payments/settlements", async (CreateSettlementRequest request, CreateSettlementHandler handler) =>
        {
            try
            {
                var response = await handler.HandleAsync(request);
                return Results.Ok(new { settlementId = response.SettlementId, transferId = response.TransferId, channel = response.Channel.ToString(), status = response.Status.ToString(), settlementReference = response.SettlementReference, settledAt = response.SettledAt });
            }
            catch (InvalidOperationException ex) when (ex.Message is "TRANSFER_NOT_FOUND" or "TRANSFER_NOT_COMPLETED" or "PAYMENT_INTENT_NOT_FOUND")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        app.MapGet("/api/v1/payments/settlements/{settlementId:guid}", async (Guid settlementId, IServiceProvider services) =>
        {
            var repo = services.GetRequiredService<ISettlementRepository>();
            var settlement = await repo.GetAsync(settlementId);
            return settlement is null
                ? Results.NotFound(new { code = "SETTLEMENT_NOT_FOUND", message = "Settlement not found." })
                : Results.Ok(settlement);
        });

        app.MapGet("/api/v1/payments/settlements", async (IServiceProvider services) =>
        {
            var repo = services.GetRequiredService<ISettlementRepository>();
            var settlements = await repo.ListAsync();
            return Results.Ok(new { items = settlements });
        });

        return app;
    }
}
