using AfriWallet.Merchants.Onboarding.Api.Contracts;
using AfriWallet.Merchants.Onboarding.Application.Abstractions;
using AfriWallet.Merchants.Onboarding.Application.Commands;
using AfriWallet.Merchants.Onboarding.Application.Policies;
using AfriWallet.Merchants.Onboarding.Application.Services;
using AfriWallet.Merchants.Onboarding.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IMerchantProfileReader, SandboxMerchantProfileReader>();
builder.Services.AddSingleton<IMerchantVerificationRepository, InMemoryMerchantVerificationRepository>();
builder.Services.AddSingleton<IMerchantVerificationProvider, SandboxMerchantVerificationProvider>();
builder.Services.AddSingleton<IMerchantVerificationAuditStore, InMemoryMerchantVerificationAuditStore>();
builder.Services.AddSingleton<IMerchantVerificationClock, SystemMerchantVerificationClock>();
builder.Services.AddSingleton<MerchantVerificationPolicy>();
builder.Services.AddSingleton<MerchantVerificationService>();

var app = builder.Build();
const string Actor = "afriwallet-merchant-verification-system";

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    delivery = "AFW-DLV-0019.2",
    scope = "SANDBOX MERCHANT VERIFICATION ONLY",
    paymentAcceptanceEnabled = false,
    captureEnabled = false,
    settlementEnabled = false,
    payoutEnabled = false,
    moneyMovementPerformed = false,
    ledgerMutationPerformed = false
}));

app.MapPost("/api/v1/merchants/verifications", async (
    CreateVerificationRequest request,
    MerchantVerificationService service,
    CancellationToken ct) =>
{
    var result = await service.CreateAsync(new CreateVerificationCommand(request.MerchantId, Actor), ct);
    return Results.Created($"/api/v1/merchants/verifications/{result.VerificationId}", result);
});

app.MapPost("/api/v1/merchants/verifications/{id:guid}/documents", async (
    Guid id,
    AddVerificationDocumentRequest request,
    MerchantVerificationService service,
    CancellationToken ct) =>
        Results.Ok(
            await service.AddDocumentAsync(
                new AddVerificationDocumentCommand(
                    id,
                    request.Type,
                    request.Reference,
                    request.Sha256,
                    request.SizeBytes,
                    request.ContentType,
                    request.SubmittedBy,
                    Actor),
                ct)));

app.MapPost("/api/v1/merchants/verifications/{id:guid}/assign", async (
    Guid id,
    AssignReviewerRequest request,
    MerchantVerificationService service,
    CancellationToken ct) =>
        Results.Ok(await service.AssignReviewerAsync(new AssignVerificationReviewerCommand(id, request.Reviewer, Actor), ct)));

app.MapPost("/api/v1/merchants/verifications/{id:guid}/notes", async (
    Guid id,
    AddReviewNoteRequest request,
    MerchantVerificationService service,
    CancellationToken ct) =>
        Results.Ok(await service.AddNoteAsync(new AddVerificationNoteCommand(id, request.Note, Actor), ct)));

app.MapPost("/api/v1/merchants/verifications/{id:guid}/execute", async (
    Guid id,
    MerchantVerificationService service,
    CancellationToken ct) =>
        Results.Ok(await service.ExecuteAsync(new ExecuteVerificationCommand(id, Actor), ct)));

app.MapGet("/api/v1/merchants/verifications/{id:guid}", async (
    Guid id,
    MerchantVerificationService service,
    CancellationToken ct) =>
        Results.Ok(await service.GetAsync(id, ct)));

app.Run();
