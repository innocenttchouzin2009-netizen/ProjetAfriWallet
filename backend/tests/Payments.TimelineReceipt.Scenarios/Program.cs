using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Settlements;
using UniversalWallet.Api.Payments.Application.Timeline;
using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Application.Validation;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.Payments.Domain.Settlements;
using UniversalWallet.Api.Payments.Domain.Timeline;
using UniversalWallet.Api.Payments.Domain.Transfers;
using UniversalWallet.Api.Payments.Infrastructure.Authorizations;
using UniversalWallet.Api.Payments.Infrastructure.Intents;
using UniversalWallet.Api.Payments.Infrastructure.Reservations;
using UniversalWallet.Api.Payments.Infrastructure.Risk;
using UniversalWallet.Api.Payments.Infrastructure.Settlements;
using UniversalWallet.Api.Payments.Infrastructure.Timeline;
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
var timelineRepository = new InMemoryTimelineItemRepository();
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
var timelineProjector = new PaymentTimelineProjector(timelineRepository, paymentRepository, transferRepository, settlementRepository);
var timelineHandler = new GetPaymentTimelineHandler(timelineRepository);
var lookupHandler = new LookupPaymentTimelineHandler(timelineRepository);

await Run("timeline item is projected after transfer and settlement", async () =>
{
    var (sourceWallet, destinationWallet) = CreateWalletPair("AWID_TL_001", "AWID_TL_DEST_001", 1000m);
    var created = await createIntentHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, destinationWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "timeline-001"), ParseAwid("AWID_TL_001"));
    await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_TL_001"), "device-001", "session-001");
    await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_TL_001"), "device-001", "session-001");
    var transfer = await createTransferHandler.HandleAsync(new CreateTransferRequest(created.IntentId), ParseAwid("AWID_TL_001"), "device-001", "session-001");
    await settleHandler.HandleAsync(new CreateSettlementRequest(transfer.TransferId, SettlementChannel.INTERNAL));
    await timelineProjector.ProjectAsync(created.IntentId, CancellationToken.None);

    var items = await timelineHandler.HandleAsync(new GetPaymentTimelineRequest(Limit: 10));
    Assert(items.Items.Count > 0, "timeline should contain at least one projected item");
    var item = items.Items.First();
    Assert(item.Direction == PaymentTimelineDirection.Outgoing, "timeline item should be outgoing");
    Assert(item.Status == PaymentTimelineStatus.Completed, "timeline item should be completed");
});

await Run("lookup by reference returns the projected item", async () =>
{
    var result = await lookupHandler.HandleAsync(new LookupPaymentTimelineRequest("AFW-PAY-"));
    Assert(result.Items.Count > 0, "lookup should return at least one item");
});

if (failures.Count == 0)
{
    Console.WriteLine("All timeline scenarios passed.");
    return;
}

Console.WriteLine("Timeline scenarios failed:");
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
    return new Guid(hash[..16].ToArray());
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}
