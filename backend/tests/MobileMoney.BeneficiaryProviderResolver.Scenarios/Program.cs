using MobileMoney.Production.Payout.BeneficiaryLookup.Application;
using MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;
using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("routes MTN to MTN provider", RoutesMtn),
    ("routes Orange to Orange provider", RoutesOrange),
    ("falls back when provider is missing", FallsBackWhenProviderMissing),
    ("falls back when provider is unavailable", FallsBackWhenProviderUnavailable)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
}

Console.WriteLine(
    $"MobileMoney beneficiary provider resolver scenarios: {scenarios.Length}/{scenarios.Length} passed.");

static async Task RoutesMtn()
{
    var mtn = new FakeProvider(
        CameroonMobileOperator.Mtn,
        new BeneficiaryProviderResult(BeneficiaryProviderStatus.Resolved, "MTN Holder"));
    var orange = new FakeProvider(
        CameroonMobileOperator.Orange,
        new BeneficiaryProviderResult(BeneficiaryProviderStatus.Resolved, "Orange Holder"));

    var resolver = new ProviderBeneficiaryAccountHolderResolver([mtn, orange]);

    var result = await resolver.ResolveAsync("+237670000000", CameroonMobileOperator.Mtn);

    AssertEqual("MTN Holder", result?.AccountHolderName);
    AssertEqual(1, mtn.CallCount);
    AssertEqual(0, orange.CallCount);
}

static async Task RoutesOrange()
{
    var mtn = new FakeProvider(
        CameroonMobileOperator.Mtn,
        new BeneficiaryProviderResult(BeneficiaryProviderStatus.Resolved, "MTN Holder"));
    var orange = new FakeProvider(
        CameroonMobileOperator.Orange,
        new BeneficiaryProviderResult(BeneficiaryProviderStatus.Resolved, "Orange Holder"));

    var resolver = new ProviderBeneficiaryAccountHolderResolver([mtn, orange]);

    var result = await resolver.ResolveAsync("+237690000000", CameroonMobileOperator.Orange);

    AssertEqual("Orange Holder", result?.AccountHolderName);
    AssertEqual(0, mtn.CallCount);
    AssertEqual(1, orange.CallCount);
}

static async Task FallsBackWhenProviderMissing()
{
    var resolver = new ProviderBeneficiaryAccountHolderResolver(
        [new FakeProvider(
            CameroonMobileOperator.Mtn,
            new BeneficiaryProviderResult(BeneficiaryProviderStatus.Resolved, "MTN Holder"))]);

    var result = await resolver.ResolveAsync("+237690000000", CameroonMobileOperator.Orange);

    AssertEqual<BeneficiaryAccountHolderResolution?>(null, result);
}

static async Task FallsBackWhenProviderUnavailable()
{
    var orange = new FakeProvider(
        CameroonMobileOperator.Orange,
        new BeneficiaryProviderResult(BeneficiaryProviderStatus.Unavailable, null));

    var resolver = new ProviderBeneficiaryAccountHolderResolver([orange]);

    var result = await resolver.ResolveAsync("+237690000000", CameroonMobileOperator.Orange);

    AssertEqual<BeneficiaryAccountHolderResolution?>(null, result);
    AssertEqual(1, orange.CallCount);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"Expected '{expected}', but got '{actual}'.");
    }
}

internal sealed class FakeProvider(
    CameroonMobileOperator @operator,
    BeneficiaryProviderResult result)
    : IBeneficiaryProvider
{
    public CameroonMobileOperator Operator { get; } = @operator;

    public int CallCount { get; private set; }

    public Task<BeneficiaryProviderResult> ResolveAsync(
        string normalizedPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(result);
    }
}
