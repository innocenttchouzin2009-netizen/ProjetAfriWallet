using UniversalWallet.Api.Payments.Application.Transfers;

namespace UniversalWallet.Api.Payments.Api.Transfers;

public static class PaymentTransferEndpoints
{
    public static WebApplication MapPaymentTransferEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payments/transfers", async (CreateTransferRequest request, HttpContext context, CreateTransferHandler handler) =>
        {
            try
            {
                var payerAwidValue = context.Request.Headers["x-awid"].FirstOrDefault() ?? "default-awid";
                var payerAwid = Guid.Empty;
                if (!string.IsNullOrWhiteSpace(payerAwidValue))
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(payerAwidValue.Trim().ToUpperInvariant());
                    var hash = System.Security.Cryptography.SHA256.HashData(bytes);
                    Span<byte> guidBytes = stackalloc byte[16];
                    hash.AsSpan(0, 16).CopyTo(guidBytes);
                    payerAwid = new Guid(guidBytes);
                }

                var response = await handler.HandleAsync(request, payerAwid, "device-001", "session-001", context.RequestAborted);
                return Results.Ok(new { transferId = response.TransferId, status = response.Status.ToString(), ledgerTransactionId = response.LedgerTransactionId });
            }
            catch (InvalidOperationException ex) when (ex.Message is "PAYMENT_INTENT_NOT_FOUND" or "PAYMENT_ALREADY_EXECUTED" or "PAYMENT_AUTHORIZATION_REQUIRED" or "PAYMENT_RESERVATION_NOT_FOUND" or "PAYMENT_RESERVATION_NOT_ACTIVE" or "PAYMENT_WALLET_NOT_FOUND" or "PAYMENT_SOURCE_WALLET_FORBIDDEN" or "LEDGER_TRANSACTION_FAILED")
            {
                return Results.BadRequest(new { code = ex.Message, message = ex.Message });
            }
        });

        app.MapGet("/api/v1/payments/transfers/{transferId:guid}", async (Guid transferId, IServiceProvider services) =>
        {
            var repo = services.GetRequiredService<IPaymentTransferRepository>();
            var transfer = await repo.GetAsync(transferId);
            return transfer is null
                ? Results.NotFound(new { code = "TRANSFER_NOT_FOUND", message = "Transfer not found." })
                : Results.Ok(transfer);
        });

        app.MapGet("/api/v1/payments/transfers", async (IServiceProvider services) =>
        {
            var repo = services.GetRequiredService<IPaymentTransferRepository>();
            var transfers = await repo.ListAsync();
            return Results.Ok(new { items = transfers });
        });

        return app;
    }
}
