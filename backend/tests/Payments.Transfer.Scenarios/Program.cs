using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Application.Validation;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.Payments.Domain.Transfers;
using UniversalWallet.Api.Payments.Infrastructure.Authorizations;
using UniversalWallet.Api.Payments.Infrastructure.Intents;
using UniversalWallet.Api.Payments.Infrastructure.Reservations;
using UniversalWallet.Api.Payments.Infrastructure.Risk;
using UniversalWallet.Api.Payments.Infrastructure.Transfers;
using UniversalWallet.Api.WalletEngine;

var failures = new List<string>();
var walletRepository = new InMemoryWalletRepository();
var paymentRepository = new InMemoryPaymentIntentRepository();
var authorizationRepository = new InMemoryAuthorizationRepository();
var reservationRepository = new InMemoryReservationRepository();
var transferRepository = new InMemoryPaymentTransferRepository();
var recipientResolver = new PaymentRecipientResolver(walletRepository);
var walletReader = new PaymentWalletReader(walletRepository);
var riskEngine = new DefaultRiskEngine();
var limitEngine = new DefaultLimitEngine();
var projectionHarness = PaymentValidationSupport.CreateBalanceProjectionService(walletRepository);
var balanceService = projectionHarness.Service;
var createIntentHandler = new CreatePaymentIntentHandler(paymentRepository, recipientResolver, walletReader);
var validateHandler = new ValidatePaymentIntentHandler(paymentRepository, walletReader, balanceService, authorizationRepository, reservationRepository, riskEngine, limitEngine);
var authorizeHandler = new AuthorizePaymentIntentHandler(paymentRepository, authorizationRepository, reservationRepository, walletReader, balanceService, riskEngine, limitEngine);
var ledgerRepository = projectionHarness.LedgerRepository;
var journalRepository = new InMemoryLedgerJournalRepository();
var validator = new LedgerValidator();
var postingService = new LedgerPostingService(walletRepository, ledgerRepository, journalRepository, validator);
var postHandler = new PostTransactionHandler(postingService);
var createTransferHandler = new CreateTransferHandler(paymentRepository, authorizationRepository, reservationRepository, walletRepository, transferRepository, postHandler, balanceService);

await Run("valid transfer creates ledger and completes intent", async () =>
{
    var (sourceWallet, destinationWallet) = CreateWalletPair("AWID_TRANSFER_VALID", "AWID_TRANSFER_DEST_VALID", 1000m);
    var created = await createIntentHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, destinationWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "transfer-001"), ParseAwid("AWID_TRANSFER_VALID"));
    await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_TRANSFER_VALID"), "device-001", "session-001");
    await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_TRANSFER_VALID"), "device-001", "session-001");
    var transfer = await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_TRANSFER_VALID"), "device-001", "session-001");
    Assert(transfer.Status == PaymentTransferStatus.Completed, "transfer should complete");
    Assert(transfer.LedgerTransactionId is not null, "ledger transaction should be created");
});

await Run("authorization missing fails", async () =>
{
    var (sourceWallet, destinationWallet) = CreateWalletPair("AWID_TRANSFER_AUTH", "AWID_TRANSFER_DEST_AUTH", 1000m);
    var created = await createIntentHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, destinationWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "transfer-002"), ParseAwid("AWID_TRANSFER_AUTH"));
    await AssertThrowsAsync(async () => await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_TRANSFER_AUTH"), "device-001", "session-001"), "PAYMENT_AUTHORIZATION_REQUIRED");
});

await Run("reservation missing fails", async () =>
{
    var (sourceWallet, destinationWallet) = CreateWalletPair("AWID_TRANSFER_RES", "AWID_TRANSFER_DEST_RES", 1000m);
    var created = await createIntentHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, destinationWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "transfer-003"), ParseAwid("AWID_TRANSFER_RES"));
    await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_TRANSFER_RES"), "device-001", "session-001");

    await authorizationRepository.AddAsync(new PaymentAuthorization
    {
        PaymentIntentId = created.IntentId,
        Decision = PaymentAuthorizationDecision.Approved,
        DecisionCode = "APPROVED",
        AuthorizedAmountMinor = created.AmountMinor,
        CurrencyCode = created.CurrencyCode,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
    });

    var authorization = await authorizationRepository.GetByIntentAsync(created.IntentId);
    Assert(authorization is not null, "authorization should be present");
    var reservation = await reservationRepository.GetByIntentAsync(created.IntentId);
    Assert(reservation is null, "reservation should be absent");
    await AssertThrowsAsync(async () => await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_TRANSFER_RES"), "device-001", "session-001"), "PAYMENT_RESERVATION_NOT_FOUND");
});

await Run("double execution is idempotent", async () =>
{
    var (sourceWallet, destinationWallet) = CreateWalletPair("AWID_TRANSFER_IDEM", "AWID_TRANSFER_DEST_IDEM", 1000m);
    var created = await createIntentHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, destinationWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "transfer-004"), ParseAwid("AWID_TRANSFER_IDEM"));
    await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_TRANSFER_IDEM"), "device-001", "session-001");
    await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_TRANSFER_IDEM"), "device-001", "session-001");
    var first = await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_TRANSFER_IDEM"), "device-001", "session-001");
    var second = await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_TRANSFER_IDEM"), "device-001", "session-001");
    Assert(first.TransferId == second.TransferId, "transfers should be idempotent");
    Assert(first.Status == PaymentTransferStatus.Completed, "first transfer should complete");
    Assert(second.Status == PaymentTransferStatus.Completed, "second transfer should complete");
});

if (failures.Count == 0)
{
    Console.WriteLine("All transfer scenarios passed.");
    return;
}

Console.WriteLine("Transfer scenarios failed:");
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

(Wallet Source, Wallet Destination) CreateWalletPair(string sourceAwid, string destinationAwid, decimal balance)
{
    var sourceWallet = walletRepository.Create(sourceAwid, WalletType.Personal, "EUR");
    sourceWallet.Status = WalletStatus.Active;
    sourceWallet.AvailableBalance = balance;
    PaymentValidationSupport.SeedProjection(projectionHarness, sourceWallet);

    var destinationWallet = walletRepository.Create(destinationAwid, WalletType.Business, "EUR");
    destinationWallet.Status = WalletStatus.Active;

    return (sourceWallet, destinationWallet);
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
