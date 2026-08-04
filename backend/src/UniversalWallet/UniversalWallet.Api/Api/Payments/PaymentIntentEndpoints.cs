using System.Security.Cryptography;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Intents;

namespace UniversalWallet.Api.Api.Payments;

public static class PaymentIntentEndpoints
{
    public static WebApplication MapPaymentIntentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payments/intents", async (CreatePaymentIntentRequest request, HttpContext context, CreatePaymentIntentHandler handler) =>
        {
            var payerAwidValue = context.Request.Headers["x-awid"].FirstOrDefault() ?? "default-awid";
            var payerAwid = HashAwid(payerAwidValue);

            try
            {
                var response = await handler.HandleAsync(request, payerAwid, context.RequestAborted);
                return Results.Created($"/api/v1/payments/intents/{response.IntentId}", response);
            }
            catch (InvalidOperationException ex) when (ex.Message is "IDEMPOTENCY_KEY_REQUIRED" or "PAYMENT_AMOUNT_INVALID" or "PAYMENT_CURRENCY_INVALID" or "PAYMENT_SOURCE_WALLET_NOT_FOUND" or "PAYMENT_SOURCE_WALLET_NOT_ACTIVE" or "PAYMENT_SOURCE_WALLET_FORBIDDEN" or "PAYMENT_RECIPIENT_NOT_FOUND" or "PAYMENT_SELF_TRANSFER_NOT_ALLOWED")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message == "IDEMPOTENCY_CONFLICT")
            {
                return Results.Conflict(new { code = ex.Message, message = ex.Message });
            }
        });

        app.MapGet("/api/v1/payments/intents/{intentId:guid}", async (Guid intentId, GetPaymentIntentHandler handler) =>
        {
            var response = await handler.HandleAsync(intentId);
            return response is null
                ? Results.NotFound(new { code = "PAYMENT_INTENT_NOT_FOUND", message = "Payment intent not found." })
                : Results.Ok(response);
        });

        app.MapGet("/api/v1/payments/intents", async (PaymentIntentStatus? status, HttpContext context, ListPaymentIntentsHandler handler) =>
        {
            var payerAwid = HashAwid(context.Request.Headers["x-awid"].FirstOrDefault() ?? "default-awid");
            var intents = await handler.HandleAsync(payerAwid, status, context.RequestAborted);
            return Results.Ok(new { items = intents.Select(intent => new { intent.Id, intent.Status, intent.AmountMinor, intent.CurrencyCode, intent.Purpose }) });
        });

        app.MapPost("/api/v1/payments/intents/{intentId:guid}/cancel", async (Guid intentId, CancelPaymentIntentHandler handler) =>
        {
            try
            {
                var intent = await handler.HandleAsync(intentId);
                return Results.Ok(intent);
            }
            catch (InvalidOperationException ex) when (ex.Message == "PAYMENT_INTENT_NOT_FOUND")
            {
                return Results.NotFound(new { code = ex.Message, message = "Payment intent not found." });
            }
            catch (InvalidOperationException ex) when (ex.Message is "PAYMENT_INTENT_ALREADY_TERMINAL" or "PAYMENT_INTENT_EXPIRED")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        app.MapPost("/internal/payments/intents/expire", async (ExpirePaymentIntentsHandler handler) =>
        {
            var expired = await handler.HandleAsync();
            return Results.Ok(new { count = expired.Count });
        });

        return app;
    }

    private static Guid HashAwid(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant());
        var hash = SHA256.HashData(bytes);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
