using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Infrastructure.Ledger;
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
var createHandler = new CreatePaymentIntentHandler(paymentRepository, recipientResolver, walletReader);
var projectionHarness = PaymentValidationSupport.CreateBalanceProjectionService(walletRepository);
var balanceProjectionService = projectionHarness.Service;
var validateHandler = new ValidatePaymentIntentHandler(paymentRepository, walletReader, balanceProjectionService, authRepository, reservationRepository, riskEngine, limitEngine);
var authorizeHandler = new AuthorizePaymentIntentHandler(paymentRepository, authRepository, reservationRepository, walletReader, balanceProjectionService, riskEngine, limitEngine);

var sourceWallet = walletRepository.Create("AWID_PAYMENT_001", WalletType.Personal, "EUR");
sourceWallet.Status = WalletStatus.Active;
sourceWallet.AvailableBalance = 1000m;
PaymentValidationSupport.SeedProjection(projectionHarness, sourceWallet);
var recipientWallet = walletRepository.Create("AWID_PAYMENT_001", WalletType.Business, "EUR");
recipientWallet.Status = WalletStatus.Active;

Run("validation succeeds", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-001"), ParseAwid("AWID_PAYMENT_001"));
    var validated = await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001");
    Assert(validated.Status == PaymentIntentStatus.Validated, "intent should be validated");
});

Run("expired intent rejected", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-002"), ParseAwid("AWID_PAYMENT_001"));
    var intent = await paymentRepository.GetAsync(created.IntentId, CancellationToken.None);
    intent!.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
    await paymentRepository.AddAsync(intent, CancellationToken.None);
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001"), "PAYMENT_INTENT_EXPIRED");
});

Run("wallet suspended rejected", async () =>
{
    var suspendedWallet = walletRepository.Create("AWID_PAYMENT_002", WalletType.Personal, "EUR");
    suspendedWallet.Status = WalletStatus.Suspended;
    suspendedWallet.AvailableBalance = 1000m;
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(suspendedWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-003"), ParseAwid("AWID_PAYMENT_002")), "PAYMENT_SOURCE_WALLET_NOT_ACTIVE");
});

Run("stale projection rejected", async () =>
{
    var staleWallet = walletRepository.Create("AWID_PAYMENT_003", WalletType.Personal, "EUR");
    staleWallet.Status = WalletStatus.Active;
    staleWallet.AvailableBalance = 1000m;
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(staleWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-004"), ParseAwid("AWID_PAYMENT_003"));
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_003"), "device-001", "session-001"), "BALANCE_PROJECTION_STALE");
});

Run("insufficient balance rejected", async () =>
{
    var poorWallet = walletRepository.Create("AWID_PAYMENT_004", WalletType.Personal, "EUR");
    poorWallet.Status = WalletStatus.Active;
    poorWallet.AvailableBalance = 50m;
    PaymentValidationSupport.SeedProjection(projectionHarness, poorWallet);
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(poorWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-005"), ParseAwid("AWID_PAYMENT_004"));
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_004"), "device-001", "session-001"), "INSUFFICIENT_AVAILABLE_BALANCE");
});

Run("transaction limit exceeded", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 60000, "EUR", "OTHER", "", "validation-006"), ParseAwid("AWID_PAYMENT_001"));
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001"), "PAYMENT_LIMIT_EXCEEDED");
});

Run("daily limit exceeded", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 60000, "EUR", "OTHER", "", "validation-007"), ParseAwid("AWID_PAYMENT_001"));
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001"), "PAYMENT_LIMIT_EXCEEDED");
});

Run("score low approved", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-008"), ParseAwid("AWID_PAYMENT_001"));
    var validated = await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001");
    Assert(validated.Status == PaymentIntentStatus.Validated, "low score intent should validate");
});

Run("missing device or session triggers step-up", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-011"), ParseAwid("AWID_PAYMENT_001"));
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "", "session-001"), "PAYMENT_STEP_UP_REQUIRED");
    await AssertThrowsAsync(async () => await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", ""), "PAYMENT_STEP_UP_REQUIRED");
});

Run("authorization idempotent", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-009"), ParseAwid("AWID_PAYMENT_001"));
    var first = await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001");
    var second = await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001");
    Assert(first.AuthorizationId == second.AuthorizationId, "authorization should be idempotent");
});

Run("single reservation per intent", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "validation-010"), ParseAwid("AWID_PAYMENT_001"));
    var first = await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001");
    var second = await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_PAYMENT_001"), "device-001", "session-001");
    Assert(first.ReservationId == second.ReservationId, "reservation should not duplicate");
});

if (failures.Count == 0)
{
    Console.WriteLine("All authorization scenarios passed.");
    return;
}

Console.WriteLine("Authorization scenarios failed:");
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
