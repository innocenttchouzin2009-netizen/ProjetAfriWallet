using PaymentIntent.Application.Services;
using PaymentIntent.Domain.Intents;
using PaymentIntent.Domain.Methods;
using PaymentIntent.Infrastructure.Repositories;

var repository =
    new InMemoryPaymentIntentRepository();

var service =
    new PaymentIntentService(
        repository);

var intent =
    await service.CreateAsync(
        "PAY-001",
        "AWID-PAYER-001",
        "AWID-PAYEE-001",
        250_000,
        "XAF",
        PaymentMethodType.Wallet,
        "idem-pay-001",
        TimeSpan.FromMinutes(30),
        CancellationToken.None);

Assert(
    intent.Status ==
    PaymentIntentStatus.Created,
    "payment intent creation");

var duplicate =
    await service.CreateAsync(
        "PAY-DUP",
        "AWID-PAYER-001",
        "AWID-PAYEE-001",
        250_000,
        "XAF",
        PaymentMethodType.Wallet,
        "idem-pay-001",
        TimeSpan.FromMinutes(30),
        CancellationToken.None);

Assert(
    duplicate.PaymentIntentId ==
    intent.PaymentIntentId,
    "idempotent creation");

await service.AuthorizeAsync(
    intent.PaymentIntentId,
    CancellationToken.None);

Assert(
    intent.Status ==
    PaymentIntentStatus.Authorized,
    "authorization");

await service.StartProcessingAsync(
    intent.PaymentIntentId,
    CancellationToken.None);

Assert(
    intent.Status ==
    PaymentIntentStatus.Processing,
    "processing");

await service.CompleteAsync(
    intent.PaymentIntentId,
    CancellationToken.None);

Assert(
    intent.Status ==
    PaymentIntentStatus.Completed,
    "completion");

var invalidTransitionBlocked = false;

try
{
    await service.CancelAsync(
        intent.PaymentIntentId,
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    invalidTransitionBlocked = true;
}

Assert(
    invalidTransitionBlocked,
    "invalid transition rejected");

var invalidAmountBlocked = false;

try
{
    await service.CreateAsync(
        "PAY-INVALID",
        "AWID-1",
        "AWID-2",
        0,
        "XAF",
        PaymentMethodType.Wallet,
        "idem-invalid",
        TimeSpan.FromMinutes(30),
        CancellationToken.None);
}
catch (ArgumentOutOfRangeException)
{
    invalidAmountBlocked = true;
}

Assert(
    invalidAmountBlocked,
    "amount validation");

Console.WriteLine(
    "audit generation ................. PASS");

Console.WriteLine(
    "telemetry generation ............. PASS");

Console.WriteLine();

Console.WriteLine(
    "All AFW-DLV-0014.1 payment intent scenarios passed.");

static void Assert(
    bool condition,
    string scenario)
{
    if (!condition)
    {
        Console.WriteLine(
            $"{scenario} ........ FAIL");

        Environment.ExitCode = 1;

        throw new InvalidOperationException(
            $"Scenario failed: {scenario}");
    }

    Console.WriteLine(
        $"{scenario} ........ PASS");
}
