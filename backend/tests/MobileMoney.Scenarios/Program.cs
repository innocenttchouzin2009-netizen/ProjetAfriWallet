using AfriWallet.PaymentPlatform.MobileMoney.Application;
using AfriWallet.PaymentPlatform.MobileMoney.Domain;

static void Check(string name, bool condition)
{
    if (!condition)
    {
        Console.WriteLine($"{name,-36} FAIL");
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    Console.WriteLine($"{name,-36} PASS");
}

var provider = new ScenarioProvider();
var registry = new MobileMoneyProviderRegistry([provider]);
var gateway = new MobileMoneyGateway(registry);

Check("provider registry", registry.GetRequired("MTN") == provider);

var request = new InitiateMobileMoneyRequest(
    "pi-afw-001",
    "MTN",
    "CM",
    "XAF",
    "237670000000",
    10_000m,
    "idem-001");

var payment = await gateway.InitiateAsync(request);

Check(
    "payment initiation",
    payment.Status == MobileMoneyPaymentStatus.Processing);

Check(
    "provider reference",
    !string.IsNullOrWhiteSpace(payment.ProviderReference));

var duplicate = await gateway.InitiateAsync(request);

Check(
    "idempotency",
    duplicate.Id == payment.Id && provider.InitiationCount == 1);

var refreshed = await gateway.RefreshStatusAsync(payment.Id);

Check(
    "status polling",
    refreshed.Status == MobileMoneyPaymentStatus.Succeeded);

Check(
    "audit generation",
    gateway.AuditEvents.Count >= 2);

Check(
    "telemetry generation",
    gateway.TelemetryEvents.Count >= 2);

var unsupportedCurrencyRejected = false;

try
{
    await gateway.InitiateAsync(request with
    {
        Currency = "EUR",
        IdempotencyKey = "idem-002"
    });
}
catch (MobileMoneyException exception)
    when (exception.Code == "currency_not_supported")
{
    unsupportedCurrencyRejected = true;
}

Check("currency validation", unsupportedCurrencyRejected);

var invalidAmountRejected = false;

try
{
    await gateway.InitiateAsync(request with
    {
        Amount = 0,
        IdempotencyKey = "idem-003"
    });
}
catch (MobileMoneyException exception)
    when (exception.Code == "invalid_amount")
{
    invalidAmountRejected = true;
}

Check("amount validation", invalidAmountRejected);

var callbackPayment = await gateway.ProcessCallbackAsync(
    new MobileMoneyCallback(
        "MTN",
        payment.ProviderReference!,
        "SUCCESSFUL",
        Signature: null));

Check(
    "callback processing",
    callbackPayment.Status == MobileMoneyPaymentStatus.Succeeded);

Console.WriteLine();
Console.WriteLine("All AFW-DLV-0014.5 mobile money gateway scenarios passed.");

internal sealed class ScenarioProvider : IMobileMoneyProvider
{
    private int _initiationCount;

    public int InitiationCount => _initiationCount;

    public MobileMoneyProvider Definition { get; } = new(
        "MTN",
        "MTN Mobile Money Scenario Provider",
        new HashSet<string>(["CM"], StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(["XAF"], StringComparer.OrdinalIgnoreCase));

    public Task<ProviderPaymentResult> InitiateAsync(
        ProviderPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _initiationCount);
        return Task.FromResult(new ProviderPaymentResult(
            "MTN-SCENARIO-001",
            MobileMoneyPaymentStatus.Processing));
    }

    public Task<ProviderStatusResult> GetStatusAsync(
        string providerReference,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProviderStatusResult(
            providerReference,
            MobileMoneyPaymentStatus.Succeeded));

    public Task<MobileMoneyPaymentStatus> ProcessCallbackAsync(
        MobileMoneyCallback callback,
        CancellationToken cancellationToken = default)
        => Task.FromResult(MobileMoneyPaymentStatus.Succeeded);
}