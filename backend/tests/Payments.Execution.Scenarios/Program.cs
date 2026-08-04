using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Execution;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.Validation;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.Payments.Infrastructure.Authorizations;
using UniversalWallet.Api.Payments.Infrastructure.Intents;
using UniversalWallet.Api.Payments.Infrastructure.Reservations;
using UniversalWallet.Api.Payments.Infrastructure.Risk;
using UniversalWallet.Api.WalletEngine;

var failures = new List<string>();
var walletRepository = new InMemoryWalletRepository();
var paymentRepository = new InMemoryPaymentIntentRepository();
var authorizationRepository = new InMemoryAuthorizationRepository();
var reservationRepository = new InMemoryReservationRepository();
var recipientResolver = new PaymentRecipientResolver(walletRepository);
var walletReader = new PaymentWalletReader(walletRepository);
var riskEngine = new DefaultRiskEngine();
var limitEngine = new DefaultLimitEngine();
var projectionHarness = PaymentValidationSupport.CreateBalanceProjectionService(walletRepository);
var balanceService = projectionHarness.Service;
var createHandler = new CreatePaymentIntentHandler(paymentRepository, recipientResolver, walletReader);
var validateHandler = new ValidatePaymentIntentHandler(paymentRepository, walletReader, balanceService, authorizationRepository, reservationRepository, riskEngine, limitEngine);
var authorizeHandler = new AuthorizePaymentIntentHandler(paymentRepository, authorizationRepository, reservationRepository, walletReader, balanceService, riskEngine, limitEngine);
var ledgerRepository = projectionHarness.LedgerRepository;
var journalRepository = new InMemoryLedgerJournalRepository();
var validator = new LedgerValidator();
var postingService = new LedgerPostingService(walletRepository, ledgerRepository, journalRepository, validator);
var postHandler = new PostTransactionHandler(postingService);
var executionHandler = new ExecutePaymentIntentHandler(paymentRepository, authorizationRepository, reservationRepository, walletRepository, postHandler, balanceService);

var sourceWallet = walletRepository.Create("AWID_EXEC_001", WalletType.Personal, "EUR");
sourceWallet.Status = WalletStatus.Active;
sourceWallet.AvailableBalance = 1000m;
PaymentValidationSupport.SeedProjection(projectionHarness, sourceWallet);
var recipientWallet = walletRepository.Create("AWID_EXEC_001", WalletType.Business, "EUR");
recipientWallet.Status = WalletStatus.Active;

Run("executes authorized payment intent", async () =>
{
    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(sourceWallet.Id, new RecipientRequest(RecipientType.Wallet, recipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "exec-001"), ParseAwid("AWID_EXEC_001"));
    var validated = await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_EXEC_001"), "device-001", "session-001");
    Assert(validated.Status == PaymentIntentStatus.Validated, "intent should be validated");
    var authorized = await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_EXEC_001"), "device-001", "session-001");
    Assert(authorized.Decision == PaymentAuthorizationDecision.Approved, "authorization should be approved");
    var executed = await executionHandler.HandleAsync(created.IntentId, ParseAwid("AWID_EXEC_001"), "device-001", "session-001");
    Assert(executed.Status == PaymentIntentStatus.Completed, "intent should complete after execution");
    Assert(executed.PostingResult is not null, "execution should include a posting result");
    Assert(executed.PostingResult!.Accepted, "ledger posting should be accepted");
    Assert(executed.ReservationStatus == FundsReservationStatus.Consumed, "reservation should be consumed");
});

Run("executes idempotently", async () =>
{
    var freshSourceWallet = walletRepository.Create("AWID_EXEC_002", WalletType.Personal, "EUR");
    freshSourceWallet.Status = WalletStatus.Active;
    freshSourceWallet.AvailableBalance = 1000m;
    PaymentValidationSupport.SeedProjection(projectionHarness, freshSourceWallet);
    var freshRecipientWallet = walletRepository.Create("AWID_EXEC_002", WalletType.Business, "EUR");
    freshRecipientWallet.Status = WalletStatus.Active;

    var created = await createHandler.HandleAsync(new CreatePaymentIntentRequest(freshSourceWallet.Id, new RecipientRequest(RecipientType.Wallet, freshRecipientWallet.Id.ToString()), 10000, "EUR", "OTHER", "", "exec-002"), ParseAwid("AWID_EXEC_002"));
    var validated = await validateHandler.HandleAsync(created.IntentId, ParseAwid("AWID_EXEC_002"), "device-001", "session-001");
    Assert(validated.Status == PaymentIntentStatus.Validated, "intent should be validated");
    await authorizeHandler.HandleAsync(created.IntentId, ParseAwid("AWID_EXEC_002"), "device-001", "session-001");
    var first = await executionHandler.HandleAsync(created.IntentId, ParseAwid("AWID_EXEC_002"), "device-001", "session-001");
    var second = await executionHandler.HandleAsync(created.IntentId, ParseAwid("AWID_EXEC_002"), "device-001", "session-001");
    Assert(first.ExecutionId == second.ExecutionId, "execution should be idempotent");
});

if (failures.Count == 0)
{
    Console.WriteLine("All payment execution scenarios passed.");
    return;
}

Console.WriteLine("Payment execution scenarios failed:");
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
