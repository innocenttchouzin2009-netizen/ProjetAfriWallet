using Treasury.Application.Services;
using Treasury.Contracts;
using Treasury.Infrastructure.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TreasuryLedgerStore>();
builder.Services.AddSingleton<TreasuryLedgerService>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

app.MapPost("/api/v1/treasury/accounts", (
    CreateTreasuryAccountRequest request,
    TreasuryLedgerService service) =>
{
    var account = service.CreateAccount(request);
    return Results.Ok(account);
});

app.MapPost("/api/v1/treasury/ledger/post", (
    PostLedgerTransactionRequest request,
    TreasuryLedgerService service) =>
{
    service.PostLedgerTransaction(request);
    return Results.Ok(new { status = "posted" });
});

app.MapPost("/api/v1/treasury/reservations", (
    CreateReservationRequest request,
    TreasuryLedgerService service) =>
{
    service.CreateReservation(request);
    return Results.Ok(new { status = "reserved" });
});

app.MapPost("/api/v1/treasury/reservations/{reservationId}/release", (
    string reservationId,
    TreasuryLedgerService service) =>
{
    service.ReleaseReservation(reservationId);
    return Results.Ok(new { status = "released" });
});

app.MapGet("/api/v1/treasury/balances/{accountId}", (
    string accountId,
    TreasuryLedgerService service) =>
{
    return Results.Ok(service.GetBalance(accountId));
});

app.MapGet("/api/v1/treasury/settlement/{partner}/{currency}", (
    string partner,
    string currency,
    TreasuryLedgerService service) =>
{
    return Results.Ok(service.GetSettlementPosition(partner, currency));
});

app.Run();
