using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Fraud.Application;
using UniversalWallet.Api.Fraud.Domain;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Validation;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Infrastructure.Authorizations;
using UniversalWallet.Api.Payments.Infrastructure.Intents;
using UniversalWallet.Api.Payments.Infrastructure.Reservations;
using UniversalWallet.Api.Payments.Infrastructure.Risk;
using UniversalWallet.Api.WalletEngine;

var failures = new List<string>();
var walletRepository = new InMemoryWalletRepository();
var paymentRepository = new InMemoryPaymentIntentRepository();
var recipientResolver = new PaymentRecipientResolver(walletRepository);
var walletReader = new PaymentWalletReader(walletRepository);
var authRepository = new InMemoryAuthorizationRepository();
var reservationRepository = new InMemoryReservationRepository();
var riskEngine = new DefaultRiskEngine();
var limitEngine = new DefaultLimitEngine();
var fraudService = new FraudService();
var createHandler = new CreatePaymentIntentHandler(paymentRepository, recipientResolver, walletReader);
var projectionHarness = PaymentValidationSupport.CreateBalanceProjectionService(walletRepository);
var balanceProjectionService = projectionHarness.Service;
var validateHandler = new ValidatePaymentIntentHandler(paymentRepository, walletReader, balanceProjectionService, authRepository, reservationRepository, riskEngine, limitEngine);

var sourceWallet = walletRepository.Create("AWID_FRAUD_001", WalletType.Personal, "EUR");
sourceWallet.Status = WalletStatus.Active;
sourceWallet.AvailableBalance = 1000000m;
PaymentValidationSupport.SeedProjection(projectionHarness, sourceWallet);
var recipientWallet = walletRepository.Create("AWID_FRAUD_001", WalletType.Business, "EUR");
recipientWallet.Status = WalletStatus.Active;

Run("low risk payment validates", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "fraud-001"), ParseAwid("AWID_FRAUD_001"));
    var validated = await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_FRAUD_001"), "device-001", "session-001");
    Assert(validated.Status == PaymentIntentStatus.Validated, "low risk payment should validate");
});

Run("high value payment without device triggers step-up", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 50000, "EUR", "OTHER", "", "fraud-002"), ParseAwid("AWID_FRAUD_001"));
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_FRAUD_001"), "", "session-001"), "PAYMENT_STEP_UP_REQUIRED");
});

Run("risk engine preserves rules version", async () =>
{
    var assessment = riskEngine.Assess(new PaymentIntent { AmountMinor = 120000, RecipientType = RecipientType.Merchant }, new PaymentWalletSnapshot(sourceWallet.Id, ParseAwid("AWID_FRAUD_001"), "EUR", WalletStatus.Active), "", "session-001");
    Assert(assessment.RulesVersion == "risk-v2", "rules version should be preserved");
});

Run("fraud service creates review cases for suspicious assessments", async () =>
{
    var assessment = await fraudService.CreateAssessmentAsync(new CreateFraudAssessmentRequest(
        PaymentIntentId: Guid.NewGuid(),
        PayerAwidId: ParseAwid("AWID_FRAUD_001"),
        SourceWalletId: sourceWallet.Id,
        DeviceId: string.Empty,
        SessionId: "session-001",
        RiskScore: 75,
        RuleSetVersion: "risk-v2",
        CorrelationId: "corr-001"));

    Assert(assessment.Decision == FraudDecision.Review, "suspicious assessment should require review");
    var review = await fraudService.CreateReviewCaseAsync(assessment.Id, "HIGH_RISK", "ops-01");
    Assert(review.Status == FraudReviewStatus.Open, "review case should open for review decisions");
});

if (failures.Count == 0)
{
    Console.WriteLine("All fraud scenarios passed.");
    return;
}

Console.WriteLine("Fraud scenarios failed:");
foreach (var failure in failures)
{
    Console.WriteLine($" - {failure}");
}
Environment.ExitCode = 1;

async Task Run(string name, Func<Task> scenario)
{
    try
    {
        await scenario();
        Console.WriteLine($"[OK] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[KO] {name} -> {ex.Message}");
    }
}

async Task AssertThrowsAsync(Func<Task> action, string expectedCode)
{
    try
    {
        await action();
        throw new Exception($"expected {expectedCode}");
    }
    catch (InvalidOperationException ex) when (ex.Message == expectedCode)
    {
        return;
    }
}

Guid ParseAwid(string awid)
{
    var bytes = System.Text.Encoding.UTF8.GetBytes(awid.Trim().ToUpperInvariant());
    var hash = System.Security.Cryptography.SHA256.HashData(bytes);
    Span<byte> guidBytes = stackalloc byte[16];
    hash.AsSpan(0, 16).CopyTo(guidBytes);
    return new Guid(guidBytes);
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}
