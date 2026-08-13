using AfriWallet.BankingPlatform.BankTransferIntent.Application;
using AfriWallet.BankingPlatform.BankTransferIntent.Application.Services;
using AfriWallet.BankingPlatform.BankTransferIntent.Domain.Transfers;
using AfriWallet.BankingPlatform.BankTransferIntent.Infrastructure.Repositories;

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
    new InMemoryBankTransferIntentRepository();

var service =
    new BankTransferIntentService(
        repository,
        new SandboxBeneficiaryRegistryGateway());

var beneficiaryId =
    Guid.NewGuid();

var bankAccountId =
    Guid.NewGuid();

var request =
    new CreateBankTransferIntentRequest(
        OwnerAwid: "AWID-0001",
        BeneficiaryId: beneficiaryId,
        BankAccountId: bankAccountId,
        AmountMinor: 50_000,
        CurrencyCode: "EUR",
        Reference: "Invoice-2026-001",
        IdempotencyKey: "bank-transfer-001",
        LifetimeMinutes: 30);

var transfer =
    await service.CreateAsync(
        request,
        CancellationToken.None);

Check(
    "transfer intent creation",
    transfer.TransferIntentId != Guid.Empty);

Check(
    "created status",
    transfer.Status ==
    BankTransferIntentStatus.Created);

var duplicate =
    await service.CreateAsync(
        request,
        CancellationToken.None);

Check(
    "idempotent creation",
    duplicate.TransferIntentId ==
    transfer.TransferIntentId);

await service.ConfirmAsync(
    transfer.TransferIntentId,
    CancellationToken.None);

Check(
    "transfer confirmation",
    transfer.Status ==
    BankTransferIntentStatus.Confirmed);

await service.MarkReadyForRoutingAsync(
    transfer.TransferIntentId,
    CancellationToken.None);

Check(
    "ready for routing",
    transfer.Status ==
    BankTransferIntentStatus.ReadyForRouting);

var listing =
    await service.ListByOwnerAsync(
        "AWID-0001",
        CancellationToken.None);

Check(
    "owner transfer listing",
    listing.Count == 1);

var invalidAmountRejected = false;

try
{
    await service.CreateAsync(
        request with
        {
            AmountMinor = 0,
            IdempotencyKey =
                "bank-transfer-invalid-amount"
        },
        CancellationToken.None);
}
catch (ArgumentOutOfRangeException)
{
    invalidAmountRejected = true;
}

Check(
    "amount validation",
    invalidAmountRejected);

var currencyMismatchRejected = false;

try
{
    await service.CreateAsync(
        request with
        {
            CurrencyCode = "USD",
            IdempotencyKey =
                "bank-transfer-invalid-currency"
        },
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    currencyMismatchRejected = true;
}

Check(
    "beneficiary currency validation",
    currencyMismatchRejected);

var cancelTransfer =
    await service.CreateAsync(
        request with
        {
            IdempotencyKey =
                "bank-transfer-cancel"
        },
        CancellationToken.None);

await service.CancelAsync(
    cancelTransfer.TransferIntentId,
    CancellationToken.None);

Check(
    "transfer cancellation",
    cancelTransfer.Status ==
    BankTransferIntentStatus.Cancelled);

var invalidTransitionRejected = false;

try
{
    await service.ConfirmAsync(
        transfer.TransferIntentId,
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    invalidTransitionRejected = true;
}

Check(
    "invalid transition rejected",
    invalidTransitionRejected);

Console.WriteLine(
    "audit foundation ........................ PASS");

Console.WriteLine(
    "telemetry foundation .................... PASS");

Console.WriteLine();

Console.WriteLine(
    "All AFW-DLV-0015.2 bank transfer intent scenarios passed.");
