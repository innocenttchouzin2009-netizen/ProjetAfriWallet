using AfriWallet.Merchant.Api.Production;
using AfriWallet.Merchant.Application.Services;
using Microsoft.Extensions.Options;
using MerchantDomain = AfriWallet.Merchant.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Services.Configure<MerchantProductionConfiguration>(builder.Configuration.GetSection(MerchantProductionConfiguration.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MerchantProductionConfiguration>>().Value);
builder.Services.AddSingleton<MerchantProductionConfigurationService>();
builder.Services.AddSingleton<MerchantRegistryService>();
builder.Services.AddSingleton<QrPaymentService>();
builder.Services.AddSingleton<SettlementService>();
builder.Services.AddSingleton<MerchantOnboardingService>();
builder.Services.AddSingleton<MerchantOnboardingValidator>();
builder.Services.AddSingleton<PosService>();
builder.Services.AddSingleton<MerchantHealthProbe>();
builder.Services.AddSingleton<MerchantTelemetry>();
builder.Services.AddSingleton<MerchantAuditService>();
builder.Services.AddSingleton<MerchantFeatureFlags>();
builder.Services.AddMerchantResilience();
builder.Services.AddMerchantRateLimiting();
builder.Services.AddMerchantOpenTelemetry();

var app = builder.Build();

app.UseMiddleware<MerchantCorrelationMiddleware>();
app.UseRateLimiter();

app.MapGet("/health/live", (MerchantHealthProbe probe) => Results.Ok(new { status = "live", checks = probe.Check() }));
app.MapGet("/health/ready", (MerchantHealthProbe probe) => Results.Ok(new { status = "ready", checks = probe.Check() }));
app.MapGet("/health/startup", () => Results.Ok(new { status = "startup" }));
app.MapGet("/api/v1/production/configuration", (MerchantProductionConfigurationService service) => Results.Ok(service.GetSummary()));
app.MapGet("/api/v1/production/feature-flags", (MerchantFeatureFlags flags) => Results.Ok(flags));
app.MapGet("/api/v1/production/metrics", (MerchantTelemetry telemetry) => Results.Ok(new { status = "ok" }));
app.MapPost("/api/v1/production/audit", (MerchantAuditService audit, string action, string subjectId, string? correlationId = null, string? merchantId = null, string? settlementId = null, string? posTerminalId = null, string? qrReference = null) =>
{
    audit.Record(action, subjectId, correlationId, merchantId, settlementId, posTerminalId, qrReference);
    return Results.Ok(new { status = "recorded" });
});
app.MapGet("/api/v1/merchants", async (MerchantRegistryService service, MerchantTelemetry telemetry, CancellationToken cancellationToken) =>
{
    telemetry.TrackMerchantCreated();
    var merchants = await service.GetAllAsync(cancellationToken);
    return Results.Ok(merchants);
}).RequireRateLimiting("merchant-registry");

app.MapGet("/api/v1/merchants/{merchantId}", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.GetByIdAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapPost("/api/v1/merchants", async (MerchantDomain.Merchant merchant, MerchantRegistryService service, MerchantTelemetry telemetry, MerchantAuditService audit, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateAsync(merchant, cancellationToken);
        telemetry.TrackMerchantCreated();
        var correlationId = httpContext.Items["MerchantCorrelationContext"] as MerchantCorrelationContext;
        audit.Record("merchant-created", created.MerchantId, correlationId?.CorrelationId, created.MerchantId);
        return Results.Created($"/api/v1/merchants/{created.MerchantId}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}).RequireRateLimiting("merchant-registry");

app.MapPut("/api/v1/merchants/{merchantId}", async (string merchantId, MerchantDomain.Merchant merchant, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateAsync(merchantId, merchant, cancellationToken);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapPost("/api/v1/merchants/{merchantId}/activate", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.ActivateAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapPost("/api/v1/merchants/{merchantId}/suspend", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.SuspendAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapPost("/api/v1/merchants/{merchantId}/close", async (string merchantId, MerchantRegistryService service, CancellationToken cancellationToken) =>
{
    var merchant = await service.CloseAsync(merchantId, cancellationToken);
    return merchant is null ? Results.NotFound() : Results.Ok(merchant);
});

app.MapGet("/api/v1/qr-payments", async (QrPaymentService service, CancellationToken cancellationToken) =>
{
    var payments = await service.GetAllAsync(cancellationToken);
    return Results.Ok(payments);
});

app.MapPost("/api/v1/qr-payments", async (MerchantDomain.QrPayment payment, QrPaymentService service, MerchantTelemetry telemetry, MerchantAuditService audit, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(payment, cancellationToken);
    telemetry.TrackQrCreated();
    var correlationId = httpContext.Items["MerchantCorrelationContext"] as MerchantCorrelationContext;
    audit.Record("qr-generated", created.PaymentId, correlationId?.CorrelationId, created.MerchantId, qrReference: created.QrId);
    return Results.Created($"/api/v1/qr-payments/{created.PaymentId}", created);
}).RequireRateLimiting("qr");

app.MapPost("/api/v1/qr-payments/generate", (GenerateQrCommand command, QrPaymentService service) =>
{
    var payment = service.GenerateQr(command);
    return Results.Created($"/api/v1/qr-payments/{payment.PaymentId}", payment);
});

app.MapPost("/api/v1/qr-payments/decode", (string code, QrPaymentService service) =>
{
    var payload = service.DecodeQr(code);
    return Results.Ok(payload);
});

app.MapPost("/api/v1/qr-payments/initiate", (InitiateQrPaymentCommand command, QrPaymentService service) =>
{
    var payment = service.InitiatePayment(command);
    return Results.Ok(payment);
});

app.MapPost("/api/v1/qr-payments/receipts", (string transferIntentId, QrPaymentService service) =>
{
    var receipt = service.GenerateReceipt(transferIntentId);
    return Results.Ok(receipt);
});

app.MapGet("/api/v1/qr-payments/{transferIntentId}/timeline", (string transferIntentId, QrPaymentService service) =>
{
    var timeline = service.GetTimeline(transferIntentId);
    return Results.Ok(new { transferIntentId, items = timeline });
});

app.MapGet("/api/v1/settlements", async (SettlementService service, CancellationToken cancellationToken) =>
{
    var settlements = await service.GetAllAsync(cancellationToken);
    return Results.Ok(settlements);
});

app.MapPost("/api/v1/settlements", async (MerchantDomain.MerchantSettlement settlement, SettlementService service, CancellationToken cancellationToken) =>
{
    var created = await service.CreateAsync(settlement, cancellationToken);
    return Results.Created($"/api/v1/settlements/{created.SettlementId}", created);
});

app.MapPost("/api/v1/merchant/settlements", (MerchantDomain.SettlementInstruction instruction, SettlementService service, MerchantTelemetry telemetry, MerchantAuditService audit, HttpContext httpContext) =>
{
    var created = service.CreateInstruction(instruction);
    telemetry.TrackSettlement();
    var correlationId = httpContext.Items["MerchantCorrelationContext"] as MerchantCorrelationContext;
    audit.Record("settlement-created", created.SettlementId, correlationId?.CorrelationId, created.MerchantId, created.SettlementId);
    return Results.Created($"/api/v1/merchant/settlements/{created.SettlementId}", created);
}).RequireRateLimiting("settlement");

app.MapGet("/api/v1/merchant/settlements", (SettlementService service) =>
{
    var settlements = service.ListInstructions();
    return Results.Ok(settlements);
});

app.MapGet("/api/v1/merchant/settlements/{settlementId}", (string settlementId, SettlementService service) =>
{
    var settlement = service.GetInstruction(settlementId);
    return settlement is null ? Results.NotFound() : Results.Ok(settlement);
});

app.MapPost("/api/v1/merchant/settlements/{settlementId}/execute", (string settlementId, MerchantDomain.SettlementMethod method, SettlementService service) =>
{
    var settlement = service.ExecuteInstruction(settlementId, method);
    return Results.Ok(settlement);
});

app.MapPost("/api/v1/merchant/settlements/{settlementId}/cancel", (string settlementId, SettlementService service) =>
{
    var settlement = service.FailInstruction(settlementId, "Cancelled by request");
    return Results.Ok(settlement);
});

app.MapPost("/api/v1/merchant/checkout", (MerchantDomain.PosCheckoutRequest request, PosService service) =>
{
    var checkout = service.CreateCheckout(request);
    return Results.Created($"/api/v1/merchant/transactions/{checkout.TransactionId}", checkout);
});

app.MapPost("/api/v1/merchant/pos/pay", (MerchantDomain.PosPaymentRequest request, PosService service) =>
{
    var payment = service.InitiatePayment(request);
    return Results.Created($"/api/v1/merchant/transactions/{payment.TransactionId}", payment);
});

app.MapPost("/api/v1/merchant/pos", (MerchantDomain.PosTerminal terminal, PosService service, MerchantTelemetry telemetry, MerchantAuditService audit, HttpContext httpContext) =>
{
    var registered = service.RegisterTerminal(terminal);
    telemetry.TrackPosTransaction();
    var correlationId = httpContext.Items["MerchantCorrelationContext"] as MerchantCorrelationContext;
    audit.Record("pos-terminal-registered", registered.TerminalId, correlationId?.CorrelationId, registered.MerchantId, posTerminalId: registered.TerminalId);
    return Results.Created($"/api/v1/merchant/pos/{registered.TerminalId}", registered);
}).RequireRateLimiting("pos");

app.MapGet("/api/v1/merchant/pos/{terminalId}", (string terminalId, PosService service) =>
{
    var terminal = service.GetTerminal(terminalId);
    return Results.Ok(terminal);
});

app.MapPost("/api/v1/merchant/pos/{terminalId}/heartbeat", (string terminalId, PosService service) =>
{
    var terminal = service.Heartbeat(terminalId);
    return Results.Ok(terminal);
});

app.MapGet("/api/v1/merchant/transactions", (PosService service, MerchantTelemetry telemetry) =>
{
    telemetry.TrackPosTransaction();
    var transactions = service.GetTransactions();
    return Results.Ok(transactions);
}).RequireRateLimiting("dashboard");

app.MapGet("/api/v1/merchant/receipts/{receiptId}", (string receiptId, PosService service, MerchantTelemetry telemetry) =>
{
    telemetry.TrackDashboardRequest();
    var receipt = service.GetReceipt(receiptId);
    return receipt is null ? Results.NotFound() : Results.Ok(receipt);
}).RequireRateLimiting("dashboard");

app.MapPost("/api/v1/merchant-onboarding", (string merchantId, string businessName, string legalName, string businessType, string registrationNumber, string taxIdentifier, MerchantOnboardingService service, MerchantTelemetry telemetry, MerchantAuditService audit, HttpContext httpContext) =>
{
    var onboarding = service.StartOnboarding(merchantId, businessName, legalName, businessType, registrationNumber, taxIdentifier);
    telemetry.TrackKyc();
    var correlationId = httpContext.Items["MerchantCorrelationContext"] as MerchantCorrelationContext;
    audit.Record("merchant-onboarding-started", onboarding.MerchantId, correlationId?.CorrelationId, onboarding.MerchantId);
    return Results.Created($"/api/v1/merchant-onboarding/{onboarding.MerchantId}", onboarding);
}).RequireRateLimiting("onboarding");

app.MapGet("/api/v1/merchant-onboarding/{merchantId}", (string merchantId, MerchantOnboardingService service, MerchantTelemetry telemetry) =>
{
    telemetry.TrackDashboardRequest();
    var onboarding = service.GetOnboarding(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
}).RequireRateLimiting("dashboard");

app.MapPut("/api/v1/merchant-onboarding/{merchantId}", (string merchantId, MerchantDomain.MerchantProfile profile, MerchantOnboardingService service) =>
{
    var onboarding = service.CompleteProfile(merchantId, profile);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.MapPost("/api/v1/merchant-onboarding/{merchantId}/submit", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.CreateKycCase(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.MapGet("/api/v1/merchant-kyc/{merchantId}", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.GetOnboarding(merchantId);
    return onboarding?.KycCase is null ? Results.NotFound() : Results.Ok(onboarding.KycCase);
});

app.MapPost("/api/v1/merchant-kyc/{merchantId}/approve", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.ApproveKyc(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.MapPost("/api/v1/merchant-kyc/{merchantId}/reject", (string merchantId, MerchantOnboardingService service) =>
{
    var onboarding = service.RejectKyc(merchantId);
    return onboarding is null ? Results.NotFound() : Results.Ok(onboarding);
});

app.Run();
