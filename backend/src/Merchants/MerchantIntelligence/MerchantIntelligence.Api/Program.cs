using AfriWallet.Merchants.Intelligence.Api.Contracts;
using AfriWallet.Merchants.Intelligence.Application.Abstractions;
using AfriWallet.Merchants.Intelligence.Application.Policies;
using AfriWallet.Merchants.Intelligence.Application.Services;
using AfriWallet.Merchants.Intelligence.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = Environment.GetEnvironmentVariable("AFW_MERCHANT_INTELLIGENCE_DB_CONNECTION_STRING")
    ?? "Data Source=merchant-intelligence.db";
builder.Services.AddDbContext<MerchantIntelligenceDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton<IMerchantIntelligenceSource, SandboxMerchantIntelligenceSource>();
builder.Services.AddScoped<IMerchantIntelligenceRepository, EfMerchantIntelligenceRepository>();
builder.Services.AddScoped<IMerchantIntelligenceAuditStore, EfMerchantIntelligenceAuditStore>();
builder.Services.AddSingleton<IMerchantIntelligenceClock, SystemMerchantIntelligenceClock>();
builder.Services.AddScoped<MerchantRiskPolicy>();
builder.Services.AddScoped<MerchantIntelligenceService>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<MerchantIntelligenceDbContext>().Database.EnsureCreatedAsync();

const string Actor = "afriwallet-merchant-intelligence-system";
app.MapGet("/health", () => Results.Ok(new { status="Healthy", delivery="AFW-DLV-0019.6", merchantRiskScoring=true, commerceIntelligence=true, deterministic=true, explainable=true, automaticMerchantBlocking=false, automaticMerchantSuspension=false, automaticSettlementFreeze=false, automaticPayoutFreeze=false, paymentCapturePerformed=false, moneyMovementPerformed=false, ledgerMutationPerformed=false }));
app.MapPost("/api/v1/merchant-intelligence/evaluate", async (EvaluateMerchantRiskRequest request, MerchantIntelligenceService service, CancellationToken ct) => Results.Ok(await service.EvaluateAsync(new EvaluateMerchantRiskCommand(request.MerchantId, Actor), ct)));
app.MapGet("/api/v1/merchant-intelligence/{merchantId}", async (string merchantId, IMerchantIntelligenceRepository repository, CancellationToken ct) => { var result=await repository.GetLatestAsync(merchantId,ct); return result is null ? Results.NotFound() : Results.Ok(result); });
app.Run();
