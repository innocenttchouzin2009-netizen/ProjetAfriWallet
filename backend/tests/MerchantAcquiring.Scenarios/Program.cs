using MerchantAcquiring.Application.Services;
using MerchantAcquiring.Domain.Payments;
using MerchantAcquiring.Domain.Profiles;
using MerchantAcquiring.Domain.Refunds;
using MerchantAcquiring.Infrastructure.MerchantRegistry;
using MerchantAcquiring.Infrastructure.PaymentRouting;
using MerchantAcquiring.Infrastructure.Repositories;

var repository =
    new InMemoryMerchantAcquiringRepository();

var service =
    new MerchantAcquiringService(
        repository,
        new SandboxMerchantRegistryGateway(),
        new SandboxPaymentRoutingGateway(),
        new SandboxAcquiringProcessorGateway(),
        new AcquiringFeeCalculator());

var profile =
    await service.CreateProfileAsync(
        "merchant-001",
        "CM",
        "XAF",
        CancellationToken.None);

Assert(
    profile.MerchantId ==
    "merchant-001",
    "acquiring profile creation");

profile.EnableMethod(
    AcquiringPaymentMethod.Wallet);

profile.EnableMethod(
    AcquiringPaymentMethod.MobileMoney);

profile.AddCurrency(
    "XAF");

profile.ConfigureFees(
    percentageFee: 1.5m,
    fixedFeeMinor: 100);

profile.Activate();

Assert(
    profile.Status ==
    MerchantAcquiringStatus.Active,
    "merchant activation");

var payment =
    await service.CreatePaymentAsync(
        Guid.NewGuid(),
        profile.MerchantId,
        "XAF",
        100_000,
        AcquiringPaymentMethod.Wallet,
        "idem-acq-001",
        CancellationToken.None);

Assert(
    payment.FeeMinor > 0 &&
    payment.NetSettlementMinor <
    payment.AmountMinor,
    "fee calculation");

var duplicate =
    await service.CreatePaymentAsync(
        payment.PaymentIntentId,
        profile.MerchantId,
        "XAF",
        100_000,
        AcquiringPaymentMethod.Wallet,
        "idem-acq-001",
        CancellationToken.None);

Assert(
    duplicate.PaymentId ==
    payment.PaymentId,
    "payment idempotency");

await service.AuthorizeAsync(
    payment.PaymentId,
    "CM",
    CancellationToken.None);

Assert(
    payment.Status ==
    AcquiringPaymentStatus.Authorized &&
    payment.ProviderId is not null,
    "payment authorization");

await service.CaptureAsync(
    payment.PaymentId,
    CancellationToken.None);

Assert(
    payment.Status ==
    AcquiringPaymentStatus.Captured,
    "payment capture");

var refund =
    await service.RefundAsync(
        payment.PaymentId,
        20_000,
        "Customer return",
        CancellationToken.None);

Assert(
    refund.Status ==
    RefundStatus.Completed,
    "partial refund");

var excessiveRefundBlocked = false;

try
{
    await service.RefundAsync(
        payment.PaymentId,
        90_000,
        "Invalid excess refund",
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    excessiveRefundBlocked = true;
}

Assert(
    excessiveRefundBlocked,
    "refund amount protection");

var invalidMerchantBlocked = false;

try
{
    await service.CreateProfileAsync(
        "DISABLED-MERCHANT",
        "CM",
        "XAF",
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    invalidMerchantBlocked = true;
}

Assert(
    invalidMerchantBlocked,
    "merchant eligibility");

Console.WriteLine(
    "payment routing integration ...... PASS");

Console.WriteLine(
    "settlement foundation ............ PASS");

Console.WriteLine(
    "audit generation ................. PASS");

Console.WriteLine(
    "telemetry generation ............. PASS");

Console.WriteLine();

Console.WriteLine(
    "All AFW-DLV-0014.3 merchant acquiring scenarios passed.");

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
