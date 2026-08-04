using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Infrastructure.Intents;
using UniversalWallet.Api.WalletEngine;

var failures = new List<string>();
var walletRepository = new InMemoryWalletRepository();
var paymentRepository = new InMemoryPaymentIntentRepository();
var recipientResolver = new PaymentRecipientResolver(walletRepository);
var walletReader = new PaymentWalletReader(walletRepository);
var createHandler = new CreatePaymentIntentHandler(paymentRepository, recipientResolver, walletReader);
var cancelHandler = new CancelPaymentIntentHandler(paymentRepository);
var expireHandler = new ExpirePaymentIntentsHandler(paymentRepository);

var sourceWallet = walletRepository.Create("AWID_PAYMENT_001", WalletType.Personal, "EUR");
sourceWallet.Status = WalletStatus.Active;
var recipientWallet = walletRepository.Create("AWID_PAYMENT_001", WalletType.Business, "EUR");
recipientWallet.Status = WalletStatus.Active;

Run("valid intent created", async () =>
{
    var response = await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        10000,
        "EUR",
        "FAMILY_SUPPORT",
        "Family support",
        "idempotency-001"),
        ParseAwid("AWID_PAYMENT_001"));

    Assert(response.IntentId != Guid.Empty, "intent should be created");
    Assert(response.Status == PaymentIntentStatus.Created, "status should be created");
});

Run("zero amount rejected", async () =>
{
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        0,
        "EUR",
        "OTHER",
        "",
        "idempotency-002"), ParseAwid("AWID_PAYMENT_001")), "PAYMENT_AMOUNT_INVALID");
});

Run("negative amount rejected", async () =>
{
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        -1,
        "EUR",
        "OTHER",
        "",
        "idempotency-003"), ParseAwid("AWID_PAYMENT_001")), "PAYMENT_AMOUNT_INVALID");
});

Run("source wallet not found", async () =>
{
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        Guid.NewGuid(),
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        100,
        "EUR",
        "OTHER",
        "",
        "idempotency-004"), ParseAwid("AWID_PAYMENT_001")), "PAYMENT_SOURCE_WALLET_NOT_FOUND");
});

Run("source wallet suspended rejected", async () =>
{
    var suspendedWallet = walletRepository.Create("AWID_PAYMENT_002", WalletType.Personal, "EUR");
    suspendedWallet.Status = WalletStatus.Suspended;
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        suspendedWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        100,
        "EUR",
        "OTHER",
        "",
        "idempotency-005"), ParseAwid("AWID_PAYMENT_002")), "PAYMENT_SOURCE_WALLET_NOT_ACTIVE");
});

Run("wallet belonging to another awid rejected", async () =>
{
    var otherWallet = walletRepository.Create("AWID_PAYMENT_003", WalletType.Personal, "EUR");
    otherWallet.Status = WalletStatus.Active;
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        otherWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        100,
        "EUR",
        "OTHER",
        "",
        "idempotency-006"), ParseAwid("AWID_PAYMENT_001")), "PAYMENT_SOURCE_WALLET_FORBIDDEN");
});

Run("different currency rejected", async () =>
{
    var usdWallet = walletRepository.Create("AWID_PAYMENT_004", WalletType.Personal, "USD");
    usdWallet.Status = WalletStatus.Active;
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        usdWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        100,
        "EUR",
        "OTHER",
        "",
        "idempotency-007"), ParseAwid("AWID_PAYMENT_004")), "PAYMENT_CURRENCY_INVALID");
});

Run("recipient not found", async () =>
{
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, Guid.NewGuid().ToString()),
        100,
        "EUR",
        "OTHER",
        "",
        "idempotency-008"), ParseAwid("AWID_PAYMENT_001")), "PAYMENT_RECIPIENT_NOT_FOUND");
});

Run("self transfer rejected", async () =>
{
    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, sourceWallet.Id.ToString()),
        100,
        "EUR",
        "OTHER",
        "",
        "idempotency-009"), ParseAwid("AWID_PAYMENT_001")), "PAYMENT_SELF_TRANSFER_NOT_ALLOWED");
});

Run("same idempotency key returns same intent", async () =>
{
    var first = await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1200,
        "EUR",
        "OTHER",
        "",
        "idempotency-010"), ParseAwid("AWID_PAYMENT_001"));
    var second = await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1200,
        "EUR",
        "OTHER",
        "",
        "idempotency-010"), ParseAwid("AWID_PAYMENT_001"));
    Assert(first.IntentId == second.IntentId, "same idempotency should return same intent");
});

Run("different payload with same idempotency conflicts", async () =>
{
    await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1300,
        "EUR",
        "OTHER",
        "",
        "idempotency-011"), ParseAwid("AWID_PAYMENT_001"));

    await AssertThrowsAsync(async () => await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1400,
        "EUR",
        "OTHER",
        "",
        "idempotency-011"), ParseAwid("AWID_PAYMENT_001")), "IDEMPOTENCY_CONFLICT");
});

Run("cancel valid intent", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1400,
        "EUR",
        "OTHER",
        "",
        "idempotency-012"), ParseAwid("AWID_PAYMENT_001"));
    var cancelled = await cancelHandler.HandleAsync(created.IntentId);
    Assert(cancelled is not null, "intent should be cancelled");
    Assert(cancelled.Status == PaymentIntentStatus.Cancelled, "intent should be cancelled");
});

Run("expired intent cannot be cancelled", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1500,
        "EUR",
        "OTHER",
        "",
        "idempotency-013"), ParseAwid("AWID_PAYMENT_001"));
    var intent = await paymentRepository.GetAsync(created.IntentId, CancellationToken.None);
    intent!.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
    await paymentRepository.AddAsync(intent, CancellationToken.None);
    await AssertThrowsAsync(async () => await cancelHandler.HandleAsync(created.IntentId), "PAYMENT_INTENT_EXPIRED");
});

Run("list filtered by status", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1600,
        "EUR",
        "OTHER",
        "",
        "idempotency-014"), ParseAwid("AWID_PAYMENT_001"));
    var intents = await paymentRepository.ListAsync(ParseAwid("AWID_PAYMENT_001"), PaymentIntentStatus.Created, CancellationToken.None);
    Assert(intents.Any(intent => intent.Id == created.IntentId), "created intent should appear in created list");
});

Run("expiration handler marks intent expired", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(
        sourceWallet.Id,
        new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()),
        1700,
        "EUR",
        "OTHER",
        "",
        "idempotency-015"), ParseAwid("AWID_PAYMENT_001"));
    var intent = await paymentRepository.GetAsync(created.IntentId, CancellationToken.None);
    intent!.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
    await paymentRepository.AddAsync(intent, CancellationToken.None);
    var expired = await expireHandler.HandleAsync();
    Assert(expired.Any(item => item.Id == created.IntentId), "expired handler should mark expired intent");
});

if (failures.Count == 0)
{
    Console.WriteLine("All payment intent scenarios passed.");
    return;
}

Console.WriteLine("Payment intent scenarios failed:");
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
