using Treasury.Application.Interfaces;
using Treasury.Application.Services;
using Treasury.Contracts.Requests;
using Treasury.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITreasuryRepository, InMemoryTreasuryRepository>();
builder.Services.AddScoped<TreasuryLedgerService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "Healthy",
    service = "afriwallet-treasury-ledger"
}));

app.MapPost(
    "/api/v1/treasury/accounts",
    async (CreateTreasuryAccountRequest request, TreasuryLedgerService service, CancellationToken cancellationToken) =>
    {
        var account = await service.CreateAccountAsync(
            request.AccountCode,
            request.DisplayName,
            request.CurrencyCode,
            request.Type,
            cancellationToken);

        return Results.Created($"/api/v1/treasury/accounts/{account.AccountId}", account);
    });

app.MapPost(
    "/api/v1/treasury/transactions",
    async (PostTreasuryTransactionRequest request, TreasuryLedgerService service, CancellationToken cancellationToken) =>
    {
        var transaction = await service.PostAsync(
            request.Reference,
            request.CorrelationId,
            request.DebitAccountId,
            request.CreditAccountId,
            request.CurrencyCode,
            request.AmountMinor,
            cancellationToken);

        return Results.Created($"/api/v1/treasury/transactions/{transaction.TransactionId}", transaction);
    });

app.MapGet(
    "/api/v1/treasury/accounts/{accountId:guid}/balance",
    async (Guid accountId, TreasuryLedgerService service, CancellationToken cancellationToken) =>
    {
        var balance = await service.GetBalanceAsync(accountId, cancellationToken);
        return Results.Ok(balance);
    });

app.MapPost(
    "/api/v1/treasury/accounts/{accountId:guid}/reservations",
    async (Guid accountId, CreateTreasuryReservationRequest request, TreasuryLedgerService service, CancellationToken cancellationToken) =>
    {
        var reservation = await service.ReserveAsync(accountId, request.AmountMinor, request.Reference, cancellationToken);
        return Results.Created($"/api/v1/treasury/reservations/{reservation.ReservationId}", reservation);
    });

app.MapPost(
    "/api/v1/treasury/reservations/{reservationId:guid}/release",
    async (Guid reservationId, TreasuryLedgerService service, CancellationToken cancellationToken) =>
    {
        await service.ReleaseReservationAsync(reservationId, cancellationToken);
        return Results.NoContent();
    });

app.MapOpenApi();

app.Run();

public partial class Program;
