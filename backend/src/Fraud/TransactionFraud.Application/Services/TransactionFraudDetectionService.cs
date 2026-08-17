using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;
using AfriWallet.Fraud.TransactionFraud.Application.Policies;
using AfriWallet.Fraud.TransactionFraud.Domain.Detection;
using AfriWallet.Fraud.TransactionFraud.Domain.Factors;
using AfriWallet.Fraud.TransactionFraud.Domain.Signals;
using AfriWallet.Fraud.TransactionFraud.Domain.Transactions;

namespace AfriWallet.Fraud.TransactionFraud.Application.Services;

public sealed class TransactionFraudDetectionService(
    IFraudSignalReader fraudSignals,
    IDeviceRiskReader deviceRisk,
    ITransactionFraudRepository repository,
    ITransactionFraudAuditStore audit,
    ITransactionFraudClock clock,
    TransactionFraudPolicy policy)
{
    public async Task<TransactionFraudResult> DetectAsync(DetectTransactionFraudCommand command, CancellationToken ct = default)
    {
        var transaction = command.Transaction.Normalize();
        var fromUtc = transaction.OccurredAtUtc.AddHours(-24);

        var accountSignals = await fraudSignals.GetBySubjectAsync("AWID", transaction.Awid, fromUtc, ct);
        var beneficiarySignals = await fraudSignals.GetBySubjectAsync("BENEFICIARY", transaction.BeneficiaryId, fromUtc, ct);
        var deviceSignals = await fraudSignals.GetBySubjectAsync("DEVICE", transaction.DeviceId, fromUtc, ct);
        var deviceRiskSnapshot = await deviceRisk.GetLatestAsync(transaction.Awid, transaction.DeviceId, ct);

        var factors = new List<TransactionFraudFactor>();
        AddUnusualAmount(transaction, factors);
        AddNewBeneficiary(transaction.OccurredAtUtc, beneficiarySignals, factors);
        AddHighVelocity(transaction, accountSignals, factors);
        AddRecentDeviceChange(transaction.OccurredAtUtc, deviceSignals, factors);
        AddDeviceRisk(deviceRiskSnapshot, factors);
        AddFailedThenSuccessfulPayment(accountSignals, factors);
        AddGeographicAnomaly(transaction, accountSignals, factors);
        AddRepeatedAttempts(accountSignals, factors);

        var score = Math.Clamp(factors.Sum(x => x.Score), 0, 100);
        var detection = new TransactionFraudDetection(
            Guid.NewGuid(),
            transaction.TransactionId,
            transaction.Awid,
            score,
            policy.ResolveBand(score),
            policy.ResolveRecommendation(score),
            factors,
            clock.UtcNow);

        await repository.SaveAsync(detection, ct);
        await audit.AppendAsync(
            new TransactionFraudAuditEvent(
                Guid.NewGuid(),
                detection.TransactionId,
                detection.DetectionId,
                detection.Awid,
                "transaction.fraud.detected",
                command.Actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["score"] = detection.Score.ToString(),
                    ["band"] = detection.Band.ToString(),
                    ["recommendation"] = detection.Recommendation.ToString()
                }),
            ct);

        return new TransactionFraudResult(
            detection.DetectionId,
            detection.TransactionId,
            detection.Awid,
            detection.Score,
            detection.Band,
            detection.Recommendation,
            detection.Factors,
            detection.DetectedAtUtc);
    }

    private static void AddUnusualAmount(FraudTransaction transaction, ICollection<TransactionFraudFactor> factors)
    {
        if (transaction.AmountMinor >= 10_000_000)
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.UnusualAmount,
                25,
                "large transaction amount outside expected range",
                null));
    }

    private static void AddNewBeneficiary(DateTimeOffset transactionTime, IReadOnlyCollection<FraudSignalSnapshot> beneficiarySignals, ICollection<TransactionFraudFactor> factors)
    {
        var recent = beneficiarySignals
            .Where(x => string.Equals(x.Type, "BeneficiaryAdded", StringComparison.OrdinalIgnoreCase))
            .Where(x => transactionTime - x.OccurredAtUtc <= TimeSpan.FromHours(24))
            .ToArray();

        if (recent.Length > 0)
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.NewBeneficiary,
                20,
                "new beneficiary added within 24 hours",
                recent[0].EventId));
    }

    private static void AddHighVelocity(FraudTransaction transaction, IReadOnlyCollection<FraudSignalSnapshot> accountSignals, ICollection<TransactionFraudFactor> factors)
    {
        var attempts = accountSignals
            .Where(x => string.Equals(x.Type, "PaymentAttempted", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Type, "PaymentFailed", StringComparison.OrdinalIgnoreCase))
            .Where(x => transaction.OccurredAtUtc - x.OccurredAtUtc <= TimeSpan.FromHours(12))
            .ToArray();

        if (attempts.Length >= 4)
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.HighTransactionVelocity,
                25,
                "multiple payment attempts in short window",
                attempts[^1].EventId));
    }

    private static void AddRecentDeviceChange(DateTimeOffset transactionTime, IReadOnlyCollection<FraudSignalSnapshot> deviceSignals, ICollection<TransactionFraudFactor> factors)
    {
        var recent = deviceSignals
            .Where(x => string.Equals(x.Type, "DeviceChanged", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Type, "NewDevice", StringComparison.OrdinalIgnoreCase))
            .Where(x => transactionTime - x.OccurredAtUtc <= TimeSpan.FromHours(12))
            .ToArray();

        if (recent.Length > 0)
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.RecentDeviceChange,
                25,
                "device context changed recently",
                recent[0].EventId));
    }

    private static void AddDeviceRisk(DeviceRiskSnapshot? deviceRiskSnapshot, ICollection<TransactionFraudFactor> factors)
    {
        if (deviceRiskSnapshot is null)
            return;

        var score = deviceRiskSnapshot.Score;
        if (score >= 60)
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.DeviceRisk,
                Math.Clamp(score / 2, 15, 35),
                $"device risk score elevated at {score}",
                null));
    }

    private static void AddFailedThenSuccessfulPayment(IReadOnlyCollection<FraudSignalSnapshot> accountSignals, ICollection<TransactionFraudFactor> factors)
    {
        var failure = accountSignals
            .LastOrDefault(x => string.Equals(x.Type, "PaymentFailed", StringComparison.OrdinalIgnoreCase));
        var success = accountSignals
            .LastOrDefault(x => string.Equals(x.Type, "PaymentAttempted", StringComparison.OrdinalIgnoreCase));

        if (failure is not null && success is not null && failure.OccurredAtUtc < success.OccurredAtUtc)
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.FailedThenSuccessfulPayment,
                15,
                "payment failure followed by subsequent attempt",
                failure.EventId));
    }

    private static void AddGeographicAnomaly(FraudTransaction transaction, IReadOnlyCollection<FraudSignalSnapshot> accountSignals, ICollection<TransactionFraudFactor> factors)
    {
        var knownCountries = accountSignals
            .Where(x => x.Attributes.TryGetValue("countryCode", out var value) && !string.IsNullOrWhiteSpace(value))
            .Select(x => x.Attributes["countryCode"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (knownCountries.Length > 0 && !knownCountries.Contains(transaction.CountryCode, StringComparer.OrdinalIgnoreCase))
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.GeographicAnomaly,
                20,
                $"country {transaction.CountryCode} not previously observed",
                null));
    }

    private static void AddRepeatedAttempts(IReadOnlyCollection<FraudSignalSnapshot> accountSignals, ICollection<TransactionFraudFactor> factors)
    {
        var attempts = accountSignals.Count(x =>
            string.Equals(x.Type, "PaymentAttempted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Type, "PaymentFailed", StringComparison.OrdinalIgnoreCase));

        if (attempts >= 4)
            factors.Add(new TransactionFraudFactor(
                TransactionFraudFactorType.RepeatedAttempts,
                20,
                $"{attempts} attempts associated with account",
                null));
    }
}
