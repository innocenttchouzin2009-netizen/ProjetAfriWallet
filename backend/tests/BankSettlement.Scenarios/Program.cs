using AfriWallet.BankingPlatform.BankSettlement.Application;
using AfriWallet.BankingPlatform.BankSettlement.Application.Services;
using AfriWallet.BankingPlatform.BankSettlement.Infrastructure.Gateways;
using AfriWallet.BankingPlatform.BankSettlement.Infrastructure.Repositories;

var repository = new InMemoryBankSettlementRepository();
var reconciliationRepository = new InMemoryReconciliationRepository();
var gateway = new SandboxBankExecutionGateway();
var settlementService = new BankSettlementService(repository, gateway);
var reconciliationService = new BankReconciliationService(reconciliationRepository, repository);

var batch = await settlementService.CreateBatchAsync(
    new CreateSettlementBatchRequest(
        "BANK-ALPHA",
        "SEPA",
        "USD",
        new DateOnly(2025, 1, 10),
        "batch-001"),
    CancellationToken.None);

var executionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

var withItem = await settlementService.AddItemAsync(
    batch.SettlementBatchId,
    new AddSettlementItemRequest(
        executionId,
        "BANK-ALPHA",
        "SEPA",
        2500,
        50,
        "USD",
        "bank-alpha-001"),
    CancellationToken.None);

var closed = await settlementService.CloseBatchAsync(
    withItem.SettlementBatchId,
    CancellationToken.None);

var reconciliation = await reconciliationService.ReconcileAsync(
    new ReconciliationRequest(
        closed.SettlementBatchId,
        closed.NetAmountMinor,
        closed.NetAmountMinor,
        "USD",
        "sandbox-bank-transfer"),
    CancellationToken.None);

Console.WriteLine($"Settlement batch: {closed.SettlementBatchId}");
Console.WriteLine($"Status: {closed.Status}");
Console.WriteLine($"Net amount: {closed.NetAmountMinor}");
Console.WriteLine($"Reconciliation status: {reconciliation.Status}");

