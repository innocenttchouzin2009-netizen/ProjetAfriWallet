using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Domain;

var scenarios = new (string Name, Action Run)[]
{
    ("valid creation normalizes and starts Created", ValidCreation),
    ("invalid E.164 beneficiary is rejected", InvalidE164),
    ("zero and negative amounts are rejected", InvalidAmounts),
    ("invalid currency is rejected", InvalidCurrency),
    ("missing idempotency key is rejected", MissingIdempotencyKey),
    ("Created -> Processing -> Submitted -> Succeeded", SuccessfulLifecycle),
    ("payout can fail before terminal success", FailureLifecycle),
    ("Created payout can be cancelled", CancellationLifecycle),
    ("terminal statuses are immutable", TerminalStatusesAreImmutable),
    ("contracts preserve payout request/response surface", ContractsSurface)
};

foreach (var scenario in scenarios)
{
    scenario.Run();
    Console.WriteLine($"PASS: {scenario.Name}");
}

Console.WriteLine($"MobileMoney payout domain/contracts scenarios: {scenarios.Length}/{scenarios.Length} passed.");

PayoutOrchestrationScenarios.RunAll();

static void ValidCreation()
{
    var now = Utc(2026, 10, 3, 12, 0);
    var beneficiary = new MobileMoneyBeneficiary(
        "  +237690000001  ",
        " cm ",
        " mtn-cm ",
        "  Ada  ");

    var payout = MobileMoneyPayout.Create(
        " wallet-001 ",
        25_000,
        " xaf ",
        beneficiary,
        " payout-key-001 ",
        now);

    Assert(payout.PayoutId != Guid.Empty, "Payout id must be generated.");
    AssertEqual("wallet-001", payout.SourceWalletId);
    AssertEqual(25_000L, payout.AmountMinor);
    AssertEqual("XAF", payout.Currency);
    AssertEqual("+237690000001", payout.Beneficiary.Msisdn);
    AssertEqual("CM", payout.Beneficiary.CountryCode);
    AssertEqual("MTN-CM", payout.Beneficiary.OperatorCode);
    AssertEqual("Ada", payout.Beneficiary.DisplayName);
    AssertEqual("payout-key-001", payout.IdempotencyKey);
    AssertEqual(MobileMoneyPayoutStatus.Created, payout.Status);
    AssertEqual(now, payout.CreatedAtUtc);
    AssertEqual(now, payout.UpdatedAtUtc);
    Assert(payout.ProviderReference is null, "Provider reference must start empty.");
    Assert(payout.FailureCode is null, "Failure code must start empty.");
}

static void InvalidE164()
{
    AssertThrows<ArgumentException>(() =>
        new MobileMoneyBeneficiary(
            "237690000001",
            "CM",
            "MTN-CM"));
}

static void InvalidAmounts()
{
    var beneficiary = ValidBeneficiary();
    var now = Utc(2026, 10, 3, 12, 0);

    AssertThrows<ArgumentOutOfRangeException>(() =>
        MobileMoneyPayout.Create(
            "wallet-001",
            0,
            "XAF",
            beneficiary,
            "key-zero",
            now));

    AssertThrows<ArgumentOutOfRangeException>(() =>
        MobileMoneyPayout.Create(
            "wallet-001",
            -1,
            "XAF",
            beneficiary,
            "key-negative",
            now));
}

static void InvalidCurrency()
{
    AssertThrows<ArgumentException>(() =>
        MobileMoneyPayout.Create(
            "wallet-001",
            100,
            "XA",
            ValidBeneficiary(),
            "key-currency",
            Utc(2026, 10, 3, 12, 0)));
}

static void MissingIdempotencyKey()
{
    AssertThrows<ArgumentException>(() =>
        MobileMoneyPayout.Create(
            "wallet-001",
            100,
            "XAF",
            ValidBeneficiary(),
            "   ",
            Utc(2026, 10, 3, 12, 0)));
}

static void SuccessfulLifecycle()
{
    var createdAt = Utc(2026, 10, 3, 12, 0);
    var payout = ValidPayout(createdAt);

    payout.Start(createdAt.AddMinutes(1));
    AssertEqual(MobileMoneyPayoutStatus.Processing, payout.Status);

    payout.MarkSubmitted(" provider-ref-001 ", createdAt.AddMinutes(2));
    AssertEqual(MobileMoneyPayoutStatus.Submitted, payout.Status);
    AssertEqual("provider-ref-001", payout.ProviderReference);
    Assert(payout.FailureCode is null, "Failure code must be cleared on submission.");

    payout.Succeed(createdAt.AddMinutes(3));
    AssertEqual(MobileMoneyPayoutStatus.Succeeded, payout.Status);
    AssertEqual(createdAt.AddMinutes(3), payout.UpdatedAtUtc);
    Assert(payout.FailureCode is null, "Succeeded payout cannot carry a failure code.");
}

static void FailureLifecycle()
{
    var createdAt = Utc(2026, 10, 3, 12, 0);
    var payout = ValidPayout(createdAt);

    payout.Start(createdAt.AddMinutes(1));
    payout.Fail(" PROVIDER_REJECTED ", createdAt.AddMinutes(2));

    AssertEqual(MobileMoneyPayoutStatus.Failed, payout.Status);
    AssertEqual("PROVIDER_REJECTED", payout.FailureCode);
    AssertEqual(createdAt.AddMinutes(2), payout.UpdatedAtUtc);
}

static void CancellationLifecycle()
{
    var createdAt = Utc(2026, 10, 3, 12, 0);
    var payout = ValidPayout(createdAt);

    payout.Cancel(createdAt.AddMinutes(1));

    AssertEqual(MobileMoneyPayoutStatus.Cancelled, payout.Status);
    AssertEqual(createdAt.AddMinutes(1), payout.UpdatedAtUtc);
}

static void TerminalStatusesAreImmutable()
{
    var succeeded = ValidPayout(Utc(2026, 10, 3, 12, 0));
    succeeded.Start(Utc(2026, 10, 3, 12, 1));
    succeeded.MarkSubmitted("provider-success", Utc(2026, 10, 3, 12, 2));
    succeeded.Succeed(Utc(2026, 10, 3, 12, 3));
    var succeededUpdatedAt = succeeded.UpdatedAtUtc;
    AssertThrows<InvalidOperationException>(() =>
        succeeded.Fail("LATE_FAILURE", Utc(2026, 10, 3, 12, 4)));
    AssertEqual(MobileMoneyPayoutStatus.Succeeded, succeeded.Status);
    AssertEqual(succeededUpdatedAt, succeeded.UpdatedAtUtc);

    var failed = ValidPayout(Utc(2026, 10, 3, 13, 0));
    failed.Start(Utc(2026, 10, 3, 13, 1));
    failed.Fail("PROVIDER_REJECTED", Utc(2026, 10, 3, 13, 2));
    var failedUpdatedAt = failed.UpdatedAtUtc;
    AssertThrows<InvalidOperationException>(() =>
        failed.Succeed(Utc(2026, 10, 3, 13, 3)));
    AssertEqual(MobileMoneyPayoutStatus.Failed, failed.Status);
    AssertEqual(failedUpdatedAt, failed.UpdatedAtUtc);

    var cancelled = ValidPayout(Utc(2026, 10, 3, 14, 0));
    cancelled.Cancel(Utc(2026, 10, 3, 14, 1));
    var cancelledUpdatedAt = cancelled.UpdatedAtUtc;
    AssertThrows<InvalidOperationException>(() =>
        cancelled.Start(Utc(2026, 10, 3, 14, 2)));
    AssertEqual(MobileMoneyPayoutStatus.Cancelled, cancelled.Status);
    AssertEqual(cancelledUpdatedAt, cancelled.UpdatedAtUtc);
}

static void ContractsSurface()
{
    var beneficiary = new MobileMoneyBeneficiaryRequest(
        "+237690000001",
        "CM",
        "MTN-CM",
        "Ada");

    var request = new CreateMobileMoneyPayoutRequest(
        "wallet-001",
        "DE",
        "EUR",
        25_000,
        "XAF",
        beneficiary,
        "payout-key-001");

    AssertEqual("wallet-001", request.SourceWalletId);
    AssertEqual("DE", request.SourceCountryCode);
    AssertEqual("EUR", request.SourceCurrency);
    AssertEqual(25_000L, request.AmountMinor);
    AssertEqual("XAF", request.Currency);
    AssertEqual("+237690000001", request.Beneficiary.Msisdn);
    AssertEqual("payout-key-001", request.IdempotencyKey);

    var now = Utc(2026, 10, 3, 12, 0);
    var response = new MobileMoneyPayoutResponse(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        MobileMoneyPayoutStatus.Submitted,
        request.SourceWalletId,
        request.AmountMinor,
        request.Currency,
        new MobileMoneyBeneficiaryResponse(
            beneficiary.Msisdn,
            beneficiary.CountryCode,
            beneficiary.OperatorCode,
            beneficiary.DisplayName),
        "provider-ref-001",
        null,
        now,
        now.AddMinutes(1));

    AssertEqual(MobileMoneyPayoutStatus.Submitted, response.Status);
    AssertEqual("provider-ref-001", response.ProviderReference);
    Assert(response.FailureCode is null, "Submitted response must not require a failure code.");

    var error = new MobileMoneyPayoutErrorResponse(
        "PAYOUT_INVALID_REQUEST",
        "Invalid payout request.",
        "correlation-001");

    AssertEqual("PAYOUT_INVALID_REQUEST", error.Code);
    AssertEqual("correlation-001", error.CorrelationId);
}

static MobileMoneyBeneficiary ValidBeneficiary() =>
    new("+237690000001", "CM", "MTN-CM", "Ada");

static MobileMoneyPayout ValidPayout(DateTimeOffset now) =>
    MobileMoneyPayout.Create(
        "wallet-001",
        25_000,
        "XAF",
        ValidBeneficiary(),
        $"payout-{now:HHmmss}",
        now);

static DateTimeOffset Utc(
    int year,
    int month,
    int day,
    int hour,
    int minute) =>
    new(year, month, day, hour, minute, 0, TimeSpan.Zero);

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(
        $"Expected exception {typeof(TException).Name}.");
}
