using UniversalWallet.Api.Payments.Application.Execution;

namespace UniversalWallet.Api.Payments.Api.Execution;

public static class PaymentExecutionEndpoints
{
    public static WebApplication MapPaymentExecutionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payments/intents/{intentId:guid}/execute", async (Guid intentId, HttpContext context, ExecutePaymentIntentHandler handler) =>
        {
            try
            {
                var response = await handler.HandleAsync(intentId, Guid.Empty, "device-001", "session-001", context.RequestAborted);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message is "PAYMENT_INTENT_NOT_FOUND" or "PAYMENT_INTENT_NOT_EXECUTABLE" or "PAYMENT_AUTHORIZATION_REQUIRED" or "PAYMENT_RESERVATION_NOT_FOUND" or "PAYMENT_RESERVATION_NOT_ACTIVE" or "PAYMENT_WALLET_NOT_FOUND" or "PAYMENT_WALLET_NOT_ACTIVE" or "PAYMENT_SOURCE_WALLET_FORBIDDEN")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        return app;
    }
}
