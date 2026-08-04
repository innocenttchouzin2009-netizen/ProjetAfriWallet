using UniversalWallet.Api.Payments.Application.Receipts;

namespace UniversalWallet.Api.Payments.Api.Receipts;

public static class PaymentReceiptEndpoints
{
    public static WebApplication MapPaymentReceiptEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/payments/{paymentIntentId:guid}/receipt", async (Guid paymentIntentId, GenerateReceiptHandler handler) =>
        {
            try
            {
                var response = await handler.HandleAsync(new GenerateReceiptRequest(paymentIntentId));
                return Results.Ok(response.Receipt);
            }
            catch (InvalidOperationException ex) when (ex.Message is "PAYMENT_INTENT_NOT_FOUND" or "PAYMENT_RECEIPT_NOT_AVAILABLE")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        app.MapGet("/api/v1/receipts/verify/{token}", async (string token, VerifyReceiptHandler handler) =>
        {
            var response = await handler.HandleAsync(token);
            return Results.Ok(new
            {
                valid = response.Valid,
                status = response.Status,
                amount = new { minor = response.AmountMinor, currency = response.CurrencyCode },
                publicReference = response.PublicReference,
                paidAt = response.PaidAt,
                documentVersion = response.DocumentVersion
            });
        });

        return app;
    }
}
