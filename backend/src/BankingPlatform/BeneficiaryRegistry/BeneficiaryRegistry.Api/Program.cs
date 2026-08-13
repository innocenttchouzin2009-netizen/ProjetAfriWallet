using AfriWallet.BankingPlatform.BeneficiaryRegistry.Application;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Application.Interfaces;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Application.Services;
using AfriWallet.BankingPlatform.BeneficiaryRegistry.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBeneficiaryRepository, InMemoryBeneficiaryRepository>();
builder.Services.AddScoped<BeneficiaryRegistryService>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new
{
    service = "afriwallet-beneficiary-registry",
    status = "healthy"
}));

app.MapPost("/api/v1/banking/beneficiaries", async (
    CreateBeneficiaryRequest request,
    BeneficiaryRegistryService service,
    CancellationToken ct) =>
{
    var beneficiary = await service.CreateBeneficiaryAsync(request, ct);

    return Results.Created(
        $"/api/v1/banking/beneficiaries/{beneficiary.BeneficiaryId}",
        beneficiary);
});

app.MapPost("/api/v1/banking/beneficiaries/{beneficiaryId:guid}/accounts", async (
    Guid beneficiaryId,
    AddBankAccountRequest request,
    BeneficiaryRegistryService service,
    CancellationToken ct) =>
{
    if (beneficiaryId != request.BeneficiaryId)
    {
        return Results.BadRequest(new { error = "beneficiary_mismatch" });
    }

    var account = await service.AddBankAccountAsync(request, ct);

    return Results.Created(
        $"/api/v1/banking/beneficiaries/{beneficiaryId}/accounts/{account.BankAccountId}",
        new
        {
            account.BankAccountId,
            account.BankName,
            account.CountryCode,
            account.CurrencyCode,
            account.AccountHolderName,
            identifier = account.Identifier.MaskedValue,
            status = account.Status.ToString()
        });
});

app.MapPost("/api/v1/banking/beneficiaries/{beneficiaryId:guid}/accounts/{bankAccountId:guid}/verify", async (
    Guid beneficiaryId,
    Guid bankAccountId,
    BeneficiaryRegistryService service,
    CancellationToken ct) =>
{
    await service.VerifyBankAccountAsync(beneficiaryId, bankAccountId, ct);
    return Results.NoContent();
});

app.MapGet("/api/v1/banking/beneficiaries/{beneficiaryId:guid}", async (
    Guid beneficiaryId,
    BeneficiaryRegistryService service,
    CancellationToken ct) =>
{
    var beneficiary = await service.GetAsync(beneficiaryId, ct);
    return beneficiary is null ? Results.NotFound() : Results.Ok(beneficiary);
});

app.MapGet("/api/v1/banking/owners/{ownerAwid}/beneficiaries", async (
    string ownerAwid,
    BeneficiaryRegistryService service,
    CancellationToken ct) =>
{
    return Results.Ok(await service.ListByOwnerAsync(ownerAwid, ct));
});

app.Run();

public partial class Program;
