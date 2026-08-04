using UniversalWallet.Api.Payments.Application.MerchantPayments;

namespace UniversalWallet.Api.Payments.Api.MerchantPayments;

public static class MerchantPaymentEndpoints
{
    public static WebApplication MapMerchantPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/merchant-qr/resolve", async (ResolveMerchantQrRequest request, ResolveMerchantQrHandler handler) =>
        {
            try
            {
                var response = await handler.HandleAsync(request);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message is "MERCHANT_QR_INVALID" or "MERCHANT_NOT_ACTIVE" or "MERCHANT_PAYMENT_REQUEST_NOT_FOUND")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        app.MapPost("/api/v1/merchants/me/payment-requests", async (Guid merchantAwid, CreateMerchantPaymentRequestRequest request, CreateMerchantPaymentRequestHandler handler) =>
        {
            try
            {
                var response = await handler.HandleAsync(merchantAwid, request);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message is "MERCHANT_NOT_ACTIVE")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        return app;
    }
}
