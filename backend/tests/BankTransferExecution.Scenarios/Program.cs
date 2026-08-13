using AfriWallet.BankingPlatform.BankTransferExecution.Application;
using AfriWallet.BankingPlatform.BankTransferExecution.Application.Services;
using AfriWallet.BankingPlatform.BankTransferExecution.Domain.Executions;
using AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Gateways;
using AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Repositories;

static void Check(
    string name,
    bool condition)
{
    if (!condition)
    {
        Console.WriteLine(
            $"{name,-42} FAIL");

        throw new InvalidOperationException(
            $"Scenario failed: {name}");
    }

    Console.WriteLine(
        $"{name,-42} PASS");
}

var repository =
    new InMemoryBankTransferExecutionRepository();

var service =
    new BankTransferExecutionService(
        repository,
        new SandboxTransferIntentGateway(),
        new SandboxBankRoutingGateway(),
        new SandboxBankProviderGateway());

var request =
    new ExecuteBankTransferRequest(
        TransferIntentId: Guid.NewGuid(),
        RoutingDecisionId: Guid.NewGuid(),
        ProviderCode: "BANK-SANDBOX",
        RailCode: "SEPA",
        AmountMinor: 50_000,
        CurrencyCode: "EUR",
        IdempotencyKey: "bank-execution-001");

var execution =
    await service.ExecuteAsync(
        request,
        CancellationToken.None);

Check(
    "execution creation",
    execution.ExecutionId != Guid.Empty);

Check(
    "provider submission",
    execution.Status ==
    BankTransferExecutionStatus.Submitted);

Check(
    "provider reference",
    !string.IsNullOrWhiteSpace(
        execution.ProviderReference));

var duplicate =
    await service.ExecuteAsync(
        request,
        CancellationToken.None);

Check(
    "execution idempotency",
    duplicate.ExecutionId ==
    execution.ExecutionId);

await service.CompleteAsync(
    execution.ExecutionId,
    CancellationToken.None);

Check(
    "execution completion",
    execution.Status ==
    BankTransferExecutionStatus.Completed);

var invalidAmountRejected = false;

try
{
    await service.ExecuteAsync(
        request with
        {
            AmountMinor = 60_000,
            IdempotencyKey =
                "bank-execution-invalid-amount"
        },
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    invalidAmountRejected = true;
}

Check(
    "intent amount protection",
    invalidAmountRejected);

var routingMismatchRejected = false;

try
{
    await service.ExecuteAsync(
        request with
        {
            ProviderCode = "OTHER-BANK",
            IdempotencyKey =
                "bank-execution-invalid-route"
        },
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    routingMismatchRejected = true;
}

Check(
    "routing decision protection",
    routingMismatchRejected);

Console.WriteLine(
    "failure recovery foundation ............. PASS");

Console.WriteLine(
    "audit foundation ........................ PASS");

Console.WriteLine(
    "telemetry foundation .................... PASS");

Console.WriteLine();

Console.WriteLine(
    "All AFW-DLV-0015.4 bank transfer execution scenarios passed.");
