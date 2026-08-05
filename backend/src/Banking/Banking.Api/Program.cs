using AfriWallet.Banking.Application.Accounts;
using AfriWallet.Banking.Application.Contracts;
using AfriWallet.Banking.Application.Registry;
using AfriWallet.Banking.Application.Routing;
using AfriWallet.Banking.Domain.Entities;
using AfriWallet.Banking.Domain.ValueObjects;
using AfriWallet.Banking.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBankProviderRepository, RegistryRepository>();
builder.Services.AddSingleton<BankRegistryService>();
builder.Services.AddSingleton<BankRoutingService>();
builder.Services.AddSingleton<IBankAccountRepository, AccountRepository>();
builder.Services.AddSingleton<BankAccountService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/v1/banks", async (BankRegistryService service, CancellationToken cancellationToken) =>
{
    var banks = await service.GetAllAsync(cancellationToken);
    return Results.Ok(banks);
});

app.MapGet("/api/v1/banks/{providerId}", async (string providerId, BankRegistryService service, CancellationToken cancellationToken) =>
{
    var bank = await service.GetByIdAsync(providerId, cancellationToken);
    return bank is null ? Results.NotFound() : Results.Ok(bank);
});

app.MapGet("/api/v1/banks/search", async (string? country, string? currency, string? scheme, string? environment, BankRegistryService service, CancellationToken cancellationToken) =>
{
    var banks = await service.SearchAsync(country, currency, scheme, environment, cancellationToken);
    return Results.Ok(banks);
});

app.MapPost("/api/v1/banks/routing", async (RoutingRequest request, BankRoutingService service, CancellationToken cancellationToken) =>
{
    var routingKey = new RoutingKey(request.Country, request.Currency, request.Scheme, request.Environment);
    var result = await service.RouteAsync(routingKey, request.AmountMinor, cancellationToken);

    return Results.Ok(new
    {
        providerCode = result.Provider?.ProviderCode,
        routingDecision = result.Decision.ToString(),
        environment = result.Provider?.Environment ?? request.Environment,
        providerId = result.Provider?.ProviderId,
        estimatedDelivery = result.Provider?.EstimatedDelivery,
        fixedFeeMinor = result.Provider?.FixedFeeMinor
    });
});

app.MapPost("/internal/banks", async (BankProvider request, BankRegistryService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(request, cancellationToken);
    return Results.Created($"/api/v1/banks/{created.ProviderId}", created);
});

app.MapPut("/internal/banks/{providerId}", async (string providerId, BankProvider request, BankRegistryService service, CancellationToken cancellationToken) =>
{
    var updated = new BankProvider
    {
        ProviderId = providerId,
        ProviderCode = request.ProviderCode,
        DisplayName = request.DisplayName,
        LegalName = request.LegalName,
        CountryCode = request.CountryCode,
        CurrencyCode = request.CurrencyCode,
        SupportedCurrencies = request.SupportedCurrencies,
        SwiftCode = request.SwiftCode,
        Bic = request.Bic,
        NationalClearingCode = request.NationalClearingCode,
        TransferSchemes = request.TransferSchemes,
        SupportsSepa = request.SupportsSepa,
        SupportsSwift = request.SupportsSwift,
        SupportsInstantPayments = request.SupportsInstantPayments,
        SupportsDomesticTransfers = request.SupportsDomesticTransfers,
        SettlementWindow = request.SettlementWindow,
        CutoffTime = request.CutoffTime,
        EstimatedDelivery = request.EstimatedDelivery,
        EstimatedDeliveryDays = request.EstimatedDeliveryDays,
        MinimumAmountMinor = request.MinimumAmountMinor,
        MaximumAmountMinor = request.MaximumAmountMinor,
        FixedFeeMinor = request.FixedFeeMinor,
        PercentageFee = request.PercentageFee,
        Environment = request.Environment,
        Status = request.Status,
        Priority = request.Priority,
        MaintenanceMode = request.MaintenanceMode,
        Capabilities = request.Capabilities,
        CreatedUtc = request.CreatedUtc,
        UpdatedUtc = request.UpdatedUtc,
        CreatedAt = request.CreatedAt,
        UpdatedAt = request.UpdatedAt,
        Version = request.Version
    };
    var result = await service.UpdateAsync(updated, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/v1/bank-accounts", async (BankAccount request, BankAccountService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(request, cancellationToken);
    return Results.Ok(created);
});

app.MapGet("/api/v1/bank-accounts", async (BankAccountService service, CancellationToken cancellationToken) =>
{
    return Results.Ok(new[] { await service.CreateAsync(new BankAccount(), cancellationToken) });
});

app.MapGet("/api/v1/bank-accounts/{bankAccountId}", async (string bankAccountId, BankAccountService service, CancellationToken cancellationToken) =>
{
    var result = await service.VerifyAsync(bankAccountId, cancellationToken);
    return Results.Ok(result);
});

app.MapPut("/api/v1/bank-accounts/{bankAccountId}", async (string bankAccountId, BankAccount request, BankAccountService service, CancellationToken cancellationToken) =>
{
    var updated = new BankAccount
    {
        BankAccountId = bankAccountId,
        OwnerAwidId = request.OwnerAwidId,
        BeneficiaryId = request.BeneficiaryId,
        AccountHolderName = request.AccountHolderName,
        AccountType = request.AccountType,
        CountryCode = request.CountryCode,
        CurrencyCode = request.CurrencyCode,
        Iban = request.Iban,
        Bic = request.Bic,
        BankCode = request.BankCode,
        BranchCode = request.BranchCode,
        AccountNumber = request.AccountNumber,
        RoutingScheme = request.RoutingScheme,
        VerificationStatus = request.VerificationStatus,
        Status = request.Status,
        Fingerprint = request.Fingerprint,
        CreatedAt = request.CreatedAt,
        UpdatedAt = request.UpdatedAt,
        Version = request.Version,
        ValidationErrors = request.ValidationErrors
    };
    return Results.Ok(updated);
});

app.MapDelete("/api/v1/bank-accounts/{bankAccountId}", async (string bankAccountId, BankAccountService service, CancellationToken cancellationToken) =>
{
    return Results.Ok(new { bankAccountId, deleted = true });
});

app.MapPost("/api/v1/bank-accounts/{bankAccountId}/verify", async (string bankAccountId, BankAccountService service, CancellationToken cancellationToken) =>
{
    var result = await service.VerifyAsync(bankAccountId, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/v1/bank-accounts/{bankAccountId}/verification", async (string bankAccountId, BankAccountService service, CancellationToken cancellationToken) =>
{
    var result = await service.VerifyAsync(bankAccountId, cancellationToken);
    return Results.Ok(new { bankAccountId, verificationStatus = result.VerificationStatus });
});

app.Run();

public sealed record RoutingRequest(string Country, string Currency, string Scheme, string Environment = "Sandbox", decimal? AmountMinor = null);
