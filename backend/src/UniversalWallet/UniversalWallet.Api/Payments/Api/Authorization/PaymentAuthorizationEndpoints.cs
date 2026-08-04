using System.Security.Cryptography;
using UniversalWallet.Api.Payments.Application.Authorization;

namespace UniversalWallet.Api.Payments.Api.Authorization;

public static class PaymentAuthorizationEndpoints
{
    public static WebApplication MapPaymentAuthorizationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payments/intents/{intentId:guid}/authorize", async (Guid intentId, HttpContext context, AuthorizePaymentIntentHandler handler) =>
        {
            var payerAwidValue = context.Request.Headers["x-awid"].FirstOrDefault() ?? "default-awid";
            var payerAwid = HashAwid(payerAwidValue);
            try
            {
                var response = await handler.HandleAsync(intentId, payerAwid, "device-001", "session-001", context.RequestAborted);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message is "PAYMENT_INTENT_NOT_FOUND" or "PAYMENT_INTENT_EXPIRED" or "PAYMENT_INTENT_NOT_VALIDATABLE" or "PAYMENT_SOURCE_WALLET_NOT_FOUND" or "PAYMENT_SOURCE_WALLET_FORBIDDEN" or "PAYMENT_WALLET_NOT_ACTIVE" or "PAYMENT_CURRENCY_MISMATCH" or "BALANCE_PROJECTION_STALE" or "INSUFFICIENT_AVAILABLE_BALANCE" or "PAYMENT_LIMIT_EXCEEDED" or "PAYMENT_DAILY_LIMIT_EXCEEDED" or "PAYMENT_MONTHLY_LIMIT_EXCEEDED" or "PAYMENT_STEP_UP_REQUIRED" or "PAYMENT_REVIEW_REQUIRED" or "PAYMENT_AUTHORIZATION_DECLINED" or "PAYMENT_ALREADY_AUTHORIZED")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        app.MapGet("/api/v1/payments/authorizations/{authorizationId:guid}", async (Guid authorizationId, HttpContext context, IServiceProvider services) =>
        {
            var repo = services.GetRequiredService<IPaymentAuthorizationRepository>();
            var authorization = await repo.GetAsync(authorizationId, context.RequestAborted);
            return authorization is null
                ? Results.NotFound(new { code = "PAYMENT_AUTHORIZATION_NOT_FOUND", message = "Authorization not found." })
                : Results.Ok(authorization);
        });

        app.MapPost("/internal/payments/reservations/{reservationId:guid}/release", async (Guid reservationId, HttpContext context, IServiceProvider services) =>
        {
            var repo = services.GetRequiredService<IPaymentReservationRepository>();
            var reservation = await repo.GetAsync(reservationId, context.RequestAborted);
            if (reservation is null)
            {
                return Results.NotFound(new { code = "RESERVATION_NOT_FOUND", message = "Reservation not found." });
            }

            reservation.Release();
            await repo.UpdateAsync(reservation, context.RequestAborted);
            return Results.Ok(new { reservationId = reservation.Id, status = reservation.Status.ToString() });
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
