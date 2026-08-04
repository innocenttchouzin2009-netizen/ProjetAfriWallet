using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Settlements;
using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Application.Validation;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.Payments.Domain.Settlements;
using UniversalWallet.Api.Payments.Domain.Transfers;
using UniversalWallet.Api.Payments.Infrastructure.Authorizations;
using UniversalWallet.Api.Payments.Infrastructure.Intents;
using UniversalWallet.Api.Payments.Infrastructure.Reservations;
using UniversalWallet.Api.Payments.Infrastructure.Risk;
using UniversalWallet.Api.Payments.Infrastructure.Settlements;
using UniversalWallet.Api.Payments.Infrastructure.Transfers;
using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.WalletEngine;

var failures = new List<string>();
var walletRepository = new InMemoryWalletRepository();
var paymentRepository = new InMemoryPaymentIntentRepository();
var authorizationRepository = new InMemoryAuthorizationRepository();
var reservationRepository = new InMemoryReservationRepository();
var transferRepository = new InMemoryPaymentTransferRepository();
var settlementRepository = new InMemorySettlementRepository();
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
var settlementProvider = new InternalSettlementProvider();
var settleHandler = new CreateSettlementHandler(transferRepository, settlementRepository, paymentRepository, settlementProvider);

await Run("settlement succeeds", async () =>
{
    var (sourceWallet, destinationWallet) = CreateWalletPair("AWID_SETTLE_001", "AWID_SETTLE_DEST_001", 1000m);
    var created = await createIntentHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, destinationWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "settle-001"), ParseAwid("AWID_SETTLE_001"));
    await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_SETTLE_001"), "device-001", "session-001");
    await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_SETTLE_001"), "device-001", "session-001");
    var transfer = await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_SETTLE_001"), "device-001", "session-001");
    var settlement = await settleHandler.HandleAsync(new CreateSettlementRequest(transfer.TransferId, SettlementChannel.INTERNAL));
    Assert(settlement.Status == SettlementStatus.SETTLED, "settlement should be settled");
    Assert(settlement.SettlementId != Guid.Empty, "settlement should have an id");
});

await Run("double settlement is idempotent", async () =>
{
    var (sourceWallet, destinationWallet) = CreateWalletPair("AWID_SETTLE_002", "AWID_SETTLE_DEST_002", 1000m);
    var created = await createIntentHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, destinationWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "settle-002"), ParseAwid("AWID_SETTLE_002"));
    await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_SETTLE_002"), "device-001", "session-001");
    await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_SETTLE_002"), "device-001", "session-001");
    var transfer = await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_SETTLE_002"), "device-001", "session-001");
    var first = await settleHandler.HandleAsync(new CreateSettlementRequest(transfer.TransferId, SettlementChannel.INTERNAL));
    var second = await settleHandler.HandleAsync(new CreateSettlementRequest(transfer.TransferId, SettlementChannel.INTERNAL));
    Assert(first.SettlementId == second.SettlementId, "double settlement should reuse existing settlement");
});

await Run("settlement missing transfer fails", async () =>
{
    await AssertThrowsAsync(async () => await settleHandler.HandleAsync(new CreateSettlementRequest(Guid.NewGuid(), SettlementChannel.INTERNAL)), "TRANSFER_NOT_FOUND");
});

if (failures.Count == 0)
{
    Console.WriteLine("All settlement scenarios passed.");
    return;
}

Console.WriteLine("Settlement scenarios failed:");
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
