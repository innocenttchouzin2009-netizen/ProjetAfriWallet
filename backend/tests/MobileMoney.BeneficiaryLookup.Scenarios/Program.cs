using MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;

using MobileMoney.Production.Payout.BeneficiaryLookup.Application;
using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

var scenarios = new (string Name, Action Run)[]
{
    ("normalizes +237 format", NormalizePlus237),
    ("normalizes 237 format", Normalize237),
    ("normalizes national 9-digit format", NormalizeNational),
    ("resolves MTN 67 range", ResolveMtn67),
    ("resolves MTN 650-654 range", ResolveMtn650To654),
    ("resolves Orange 69 range", ResolveOrange69),
    ("resolves Orange 655-659 range", ResolveOrange655To659),
    ("leaves unsupported mobile prefix unresolved", UnsupportedPrefixIsUnresolved),
    ("rejects invalid national length", RejectsInvalidLength)
};

foreach (var scenario in scenarios)
{
    scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine(
    $"MobileMoney beneficiary lookup scenarios: {scenarios.Length}/{scenarios.Length} passed.");

static void NormalizePlus237()
{
    AssertEqual(
        "+237670123456",
        CameroonPhoneNumberNormalizer.Normalize("+237670123456"));
}

static void Normalize237()
{
    AssertEqual(
        "+237699123456",
        CameroonPhoneNumberNormalizer.Normalize("237699123456"));
}

static void NormalizeNational()
{
    AssertEqual(
        "+237655123456",
        CameroonPhoneNumberNormalizer.Normalize("655123456"));
}

static void ResolveMtn67()
{
    AssertResolved("670123456", CameroonMobileOperator.Mtn);
}

static void ResolveMtn650To654()
{
    foreach (var prefix in new[] { "650", "651", "652", "653", "654" })
        AssertResolved($"{prefix}123456", CameroonMobileOperator.Mtn);
}

static void ResolveOrange69()
{
    AssertResolved("+237690123456", CameroonMobileOperator.Orange);
}

static void ResolveOrange655To659()
{
    foreach (var prefix in new[] { "655", "656", "657", "658", "659" })
        AssertResolved($"237{prefix}123456", CameroonMobileOperator.Orange);
}

static void UnsupportedPrefixIsUnresolved()
{
    var result = new CameroonOperatorResolver().Resolve("660123456");

    AssertEqual("+237660123456", result.NormalizedPhoneNumber);
    Assert(!result.IsResolved, "Unsupported Cameroon mobile prefix must remain unresolved.");
    Assert(result.Operator is null, "Unsupported Cameroon mobile prefix must not be assigned.");
}

static void RejectsInvalidLength()
{
    try
    {
        CameroonPhoneNumberNormalizer.Normalize("67012345");
        throw new InvalidOperationException("Expected invalid Cameroon number to be rejected.");
    }
    catch (ArgumentException)
    {
    }
}

static void AssertResolved(string input, CameroonMobileOperator expectedOperator)
{
    var result = new CameroonOperatorResolver().Resolve(input);

    Assert(result.IsResolved, $"Expected {input} to resolve.");
    AssertEqual<CameroonMobileOperator?>(expectedOperator, result.Operator);
    Assert(result.NormalizedPhoneNumber.StartsWith("+237", StringComparison.Ordinal),
        "Resolved number must use +237 E.164 format.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException(
            $"Expected '{expected}', got '{actual}'.");
}


var holderLookupScenarios = new (string Name, Func<Task> Run)[]
{
    ("resolves beneficiary holder from normalized number and operator", ResolveBeneficiaryHolder),
    ("returns unresolved when operator lookup has no holder", HolderLookupUnavailable),
    ("rejects non-normalized input before provider lookup", RejectsNonNormalizedHolderLookupInput)
};

foreach (var scenario in holderLookupScenarios)
{
    await scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

static async Task ResolveBeneficiaryHolder()
{
    var resolver = new FakeBeneficiaryAccountHolderResolver(
        (phoneNumber, @operator) =>
        {
            AssertEqual("+237670123456", phoneNumber);
            AssertEqual(CameroonMobileOperator.Mtn, @operator);
            return new BeneficiaryAccountHolderResolution("  Ada N.  ");
        });

    var service = new BeneficiaryAccountHolderLookupService(resolver);
    var result = await service.LookupAsync(
        new BeneficiaryAccountHolderLookupRequest(
            "+237670123456",
            CameroonMobileOperator.Mtn));

    AssertEqual("+237670123456", result.NormalizedPhoneNumber);
    AssertEqual("CM", result.CountryCode);
    AssertEqual(CameroonMobileOperator.Mtn, result.Operator);
    AssertEqual("Ada N.", result.AccountHolderName);
    Assert(result.BeneficiaryResolved, "Holder lookup must be resolved.");
    AssertEqual(1, resolver.CallCount);
}

static async Task HolderLookupUnavailable()
{
    var resolver = new FakeBeneficiaryAccountHolderResolver((_, _) => null);
    var service = new BeneficiaryAccountHolderLookupService(resolver);

    var result = await service.LookupAsync(
        new BeneficiaryAccountHolderLookupRequest(
            "+237699123456",
            CameroonMobileOperator.Orange));

    AssertEqual(CameroonMobileOperator.Orange, result.Operator);
    Assert(result.AccountHolderName is null, "Unavailable holder name must remain null.");
    Assert(!result.BeneficiaryResolved, "Unavailable holder lookup must remain unresolved.");
    AssertEqual(1, resolver.CallCount);
}

static async Task RejectsNonNormalizedHolderLookupInput()
{
    var resolver = new FakeBeneficiaryAccountHolderResolver(
        (_, _) => throw new InvalidOperationException(
            "Provider lookup must not run for non-normalized input."));
    var service = new BeneficiaryAccountHolderLookupService(resolver);

    try
    {
        await service.LookupAsync(
            new BeneficiaryAccountHolderLookupRequest(
                "670123456",
                CameroonMobileOperator.Mtn));
        throw new InvalidOperationException("Expected non-normalized input to be rejected.");
    }
    catch (ArgumentException)
    {
    }

    AssertEqual(0, resolver.CallCount);
}
