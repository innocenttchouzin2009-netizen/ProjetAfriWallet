using Accounting.Application.Interfaces;
using Accounting.Application.Services;
using Accounting.Contracts.Requests;
using Accounting.Contracts.Responses;
using Accounting.Domain.Accounts;
using Accounting.Domain.Entries;
using Accounting.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAccountingRepository, InMemoryAccountingRepository>();
builder.Services.AddScoped<GeneralLedgerService>();
builder.Services.AddScoped<JournalReversalService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "Healthy",
    service = "afriwallet-accounting-general-ledger"
}));

app.MapPost(
    "/api/v1/accounting/accounts",
    async (CreateGeneralLedgerAccountRequest request, GeneralLedgerService service, CancellationToken cancellationToken) =>
    {
        var account = await service.CreateAccountAsync(
            request.AccountCode,
            request.DisplayName,
            request.CurrencyCode,
            request.Type,
            cancellationToken);

        return Results.Created($"/api/v1/accounting/accounts/{account.AccountId}", account);
    });

app.MapPost(
    "/api/v1/accounting/periods",
    async (CreateAccountingPeriodRequest request, GeneralLedgerService service, CancellationToken cancellationToken) =>
    {
        var period = await service.OpenPeriodAsync(
            request.PeriodCode,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return Results.Created($"/api/v1/accounting/periods/{period.PeriodId}", period);
    });

app.MapPost(
    "/api/v1/accounting/journal-entries",
    async (PostJournalEntryRequest request, GeneralLedgerService service, CancellationToken cancellationToken) =>
    {
        var journalEntry = await service.PostJournalEntryAsync(
            request.PeriodId,
            request.Reference,
            request.Description,
            request.Lines.Select(line => new JournalPostingLine(
                line.AccountId,
                line.CurrencyCode,
                line.AmountMinor,
                line.Side,
                line.Narration)).ToArray(),
            null,
            cancellationToken);

        return Results.Created($"/api/v1/accounting/journal-entries/{journalEntry.JournalEntryId}", journalEntry);
    });

app.MapPost(
    "/api/v1/accounting/journal-entries/{journalEntryId:guid}/reverse",
    async (Guid journalEntryId, ReverseJournalEntryRequest request, JournalReversalService service, CancellationToken cancellationToken) =>
    {
        var journalEntry = await service.ReverseAsync(
            journalEntryId,
            request.Reference,
            request.Reason,
            cancellationToken);

        return Results.Created($"/api/v1/accounting/journal-entries/{journalEntry.JournalEntryId}", journalEntry);
    });

app.MapGet(
    "/api/v1/accounting/periods/{periodId:guid}/trial-balance",
    async (Guid periodId, GeneralLedgerService service, CancellationToken cancellationToken) =>
    {
        var trialBalance = await service.GetTrialBalanceAsync(periodId, cancellationToken);
        var response = trialBalance.Select(line => new TrialBalanceLineResponse(
            line.AccountId,
            line.AccountCode,
            line.DisplayName,
            line.CurrencyCode,
            line.DebitMinor,
            line.CreditMinor,
            line.NetMinor)).ToArray();

        return Results.Ok(response);
    });

app.MapOpenApi();

app.Run();

public partial class Program;
