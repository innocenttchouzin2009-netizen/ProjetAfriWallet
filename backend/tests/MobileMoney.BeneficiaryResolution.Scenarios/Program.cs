using MobileMoney.Production.BeneficiaryResolution.Application;
using MobileMoney.Production.BeneficiaryResolution.Application.Models;
using MobileMoney.Production.BeneficiaryResolution.Contracts;
using MobileMoney.Production.BeneficiaryResolution.Domain;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("resolved path is deterministic", ResolvedPath),
    ("operator confirmation required path is deterministic", OperatorConfirmationRequiredPath),
    ("manual entry required path is deterministic", ManualEntryRequiredPath),
    ("invalid phone number is rejected before resolution", InvalidPhoneNumberPath),
    ("unresolved country is rejected before operator lookup", UnresolvedCountryPath)
};

foreach (var scenario in scenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine(
    $"MobileMoney beneficiary resolution scenarios: {scenarios.Length}/{scenarios.Length} passed.");

static async Task ResolvedPath()
{
    var fixedNow = Utc(2026, 10, 5, 8, 0);
    var country = new FakeCountryResolver(
        (_, hint) =>
        {
            AssertEqual("CM", hint);
            return new BeneficiaryCountryResolution(
                " cm ",
                BeneficiaryResolutionSource.NumberPlan);
        });

    var operatorResolver = new FakeOperatorResolver(
        (_, countryCode, hint) =>
        {
            AssertEqual("CM", countryCode);
            AssertEqual("MTN-CM", hint);
            return new BeneficiaryOperatorResolution(
                " mtn-cm ",
                BeneficiaryResolutionSource.NumberPlan,
                RequiresConfirmation: false);
        });

    var nameResolver = new FakeAccountHolderNameResolver(
        (_, countryCode, operatorCode) =>
        {
            AssertEqual("CM", countryCode);
            AssertEqual("MTN-CM", operatorCode);
            return new BeneficiaryAccountHolderNameResolution(
                "  Ada N.  ",
                BeneficiaryResolutionSource.OperatorLookup);
        });

    var service = CreateService(
        country,
        operatorResolver,
        nameResolver,
        fixedNow);

    var result = await service.ResolveAsync(
        new ResolveBeneficiaryRequest(
            " +237 (690) 000-001 ",
            " cm ",
            " mtn-cm "));

    AssertResolutionId(result.ResolutionId);
    AssertEqual("+237690000001", result.NormalizedPhoneNumber);
    AssertEqual("CM", result.CountryCode);
    AssertEqual("MTN-CM", result.OperatorCode);
    AssertEqual("Ada N.", result.AccountHolderName);
    AssertEqual(BeneficiaryResolutionStatus.Resolved, result.Status);
    AssertEqual(BeneficiaryResolutionSource.OperatorLookup, result.Source);
    Assert(!result.RequiresOperatorConfirmation, "Resolved path must not require operator confirmation.");
    Assert(result.ManualEntryAllowed, "Manual entry fallback must remain available.");
    AssertEqual(fixedNow, result.ResolvedAtUtc);
    AssertEqual(1, country.CallCount);
    AssertEqual(1, operatorResolver.CallCount);
    AssertEqual(1, nameResolver.CallCount);
}

static async Task OperatorConfirmationRequiredPath()
{
    var fixedNow = Utc(2026, 10, 5, 8, 5);
    var country = new FakeCountryResolver(
        (_, _) => new BeneficiaryCountryResolution(
            "CM",
            BeneficiaryResolutionSource.NumberPlan));

    var operatorResolver = new FakeOperatorResolver(
        (_, _, _) => new BeneficiaryOperatorResolution(
            "ORANGE-CM",
            BeneficiaryResolutionSource.NumberPlan,
            RequiresConfirmation: true));

    var nameResolver = new FakeAccountHolderNameResolver(
        (_, _, _) => new BeneficiaryAccountHolderNameResolution(
            "Marie",
            BeneficiaryResolutionSource.OperatorLookup));

    var service = CreateService(
        country,
        operatorResolver,
        nameResolver,
        fixedNow);

    var result = await service.ResolveAsync(
        new ResolveBeneficiaryRequest("+237699000001"));

    AssertEqual(BeneficiaryResolutionStatus.OperatorConfirmationRequired, result.Status);
    AssertEqual("ORANGE-CM", result.OperatorCode);
    AssertEqual("Marie", result.AccountHolderName);
    Assert(result.RequiresOperatorConfirmation, "Operator confirmation flag must be true.");
    Assert(result.ManualEntryAllowed, "Manual entry fallback must remain available.");
    AssertEqual(fixedNow, result.ResolvedAtUtc);
    AssertEqual(1, country.CallCount);
    AssertEqual(1, operatorResolver.CallCount);
    AssertEqual(1, nameResolver.CallCount);
}

static async Task ManualEntryRequiredPath()
{
    var fixedNow = Utc(2026, 10, 5, 8, 10);
    var country = new FakeCountryResolver(
        (_, _) => new BeneficiaryCountryResolution(
            "CM",
            BeneficiaryResolutionSource.NumberPlan));

    var operatorResolver = new FakeOperatorResolver(
        (_, _, _) => null);

    var nameResolver = new FakeAccountHolderNameResolver(
        (_, _, _) => throw new InvalidOperationException(
            "Account holder lookup must not run when operator resolution fails."));

    var service = CreateService(
        country,
        operatorResolver,
        nameResolver,
        fixedNow);

    var result = await service.ResolveAsync(
        new ResolveBeneficiaryRequest("+237650000001"));

    AssertEqual(BeneficiaryResolutionStatus.ManualEntryRequired, result.Status);
    AssertEqual("CM", result.CountryCode);
    Assert(result.OperatorCode is null, "Manual path must not invent an operator.");
    Assert(result.AccountHolderName is null, "Manual path must not invent an account holder name.");
    Assert(!result.RequiresOperatorConfirmation, "Manual path cannot require confirmation for an unknown operator.");
    Assert(result.ManualEntryAllowed, "Manual entry must be allowed.");
    AssertEqual(BeneficiaryResolutionSource.NumberPlan, result.Source);
    AssertEqual(fixedNow, result.ResolvedAtUtc);
    AssertEqual(1, country.CallCount);
    AssertEqual(1, operatorResolver.CallCount);
    AssertEqual(0, nameResolver.CallCount);
}

static async Task InvalidPhoneNumberPath()
{
    var country = new FakeCountryResolver(
        (_, _) => throw new InvalidOperationException(
            "Country resolution must not run for an invalid phone number."));
    var operatorResolver = new FakeOperatorResolver(
        (_, _, _) => throw new InvalidOperationException(
            "Operator resolution must not run for an invalid phone number."));
    var nameResolver = new FakeAccountHolderNameResolver(
        (_, _, _) => throw new InvalidOperationException(
            "Name resolution must not run for an invalid phone number."));

    var service = CreateService(
        country,
        operatorResolver,
        nameResolver,
        Utc(2026, 10, 5, 8, 15));

    var exception = await AssertThrowsAsync<BeneficiaryResolutionException>(
        () => service.ResolveAsync(
            new ResolveBeneficiaryRequest("237690000001")));

    AssertEqual(BeneficiaryResolutionErrorCode.InvalidPhoneNumber, exception.Code);
    AssertEqual(0, country.CallCount);
    AssertEqual(0, operatorResolver.CallCount);
    AssertEqual(0, nameResolver.CallCount);
}

static async Task UnresolvedCountryPath()
{
    var country = new FakeCountryResolver((_, _) => null);
    var operatorResolver = new FakeOperatorResolver(
        (_, _, _) => throw new InvalidOperationException(
            "Operator resolution must not run when country resolution fails."));
    var nameResolver = new FakeAccountHolderNameResolver(
        (_, _, _) => throw new InvalidOperationException(
            "Name resolution must not run when country resolution fails."));

    var service = CreateService(
        country,
        operatorResolver,
        nameResolver,
        Utc(2026, 10, 5, 8, 20));

    var exception = await AssertThrowsAsync<BeneficiaryResolutionException>(
        () => service.ResolveAsync(
            new ResolveBeneficiaryRequest("+237690000001")));

    AssertEqual(BeneficiaryResolutionErrorCode.UnsupportedCountry, exception.Code);
    AssertEqual(1, country.CallCount);
    AssertEqual(0, operatorResolver.CallCount);
    AssertEqual(0, nameResolver.CallCount);
}

static BeneficiaryResolutionApplicationService CreateService(
    FakeCountryResolver countryResolver,
    FakeOperatorResolver operatorResolver,
    FakeAccountHolderNameResolver nameResolver,
    DateTimeOffset now) =>
    new(
        countryResolver,
        operatorResolver,
        nameResolver,
        new FixedTimeProvider(now));

static DateTimeOffset Utc(
    int year,
    int month,
    int day,
    int hour,
    int minute) =>
    new(year, month, day, hour, minute, 0, TimeSpan.Zero);

static void AssertResolutionId(string resolutionId)
{
    Assert(
        Guid.TryParseExact(resolutionId, "N", out _),
        "Resolution id must be a 32-character GUID in N format.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"Expected '{expected}', got '{actual}'.");
    }
}

static async Task<TException> AssertThrowsAsync<TException>(
    Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException exception)
    {
        return exception;
    }

    throw new InvalidOperationException(
        $"Expected exception {typeof(TException).Name}.");
}
