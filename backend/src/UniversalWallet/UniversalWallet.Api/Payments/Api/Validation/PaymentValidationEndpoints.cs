using System.Security.Cryptography;
using UniversalWallet.Api.Payments.Application.Validation;

namespace UniversalWallet.Api.Payments.Api.Validation;

public static class PaymentValidationEndpoints
{
    public static WebApplication MapPaymentValidationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payments/intents/{intentId:guid}/validate", async (Guid intentId, HttpContext context, ValidatePaymentIntentHandler handler) =>
        {
            var payerAwidValue = context.Request.Headers["x-awid"].FirstOrDefault() ?? "default-awid";
            var payerAwid = HashAwid(payerAwidValue);
            try
            {
                var response = await handler.HandleAsync(intentId, payerAwid, "device-001", "session-001", context.RequestAborted);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message is "PAYMENT_INTENT_NOT_FOUND" or "PAYMENT_INTENT_EXPIRED" or "PAYMENT_INTENT_NOT_VALIDATABLE" or "PAYMENT_SOURCE_WALLET_NOT_FOUND" or "PAYMENT_SOURCE_WALLET_FORBIDDEN" or "PAYMENT_WALLET_NOT_ACTIVE" or "PAYMENT_CURRENCY_MISMATCH" or "BALANCE_PROJECTION_STALE" or "INSUFFICIENT_AVAILABLE_BALANCE" or "PAYMENT_LIMIT_EXCEEDED" or "PAYMENT_DAILY_LIMIT_EXCEEDED" or "PAYMENT_MONTHLY_LIMIT_EXCEEDED" or "PAYMENT_STEP_UP_REQUIRED" or "PAYMENT_REVIEW_REQUIRED" or "PAYMENT_AUTHORIZATION_DECLINED")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
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
