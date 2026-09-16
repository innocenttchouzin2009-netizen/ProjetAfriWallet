using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public sealed record PushDeliveryTargetOutcome(
    PushDeviceRegistrationId RegistrationId,
    bool Accepted,
    PushDeliveryFailureKind? FailureKind);

public sealed record PushDeliveryBatchResult(
    Guid UserId,
    int Targets,
    int Delivered,
    int Failed,
    int InvalidTokensDeactivated)
{
    public IReadOnlyList<PushDeliveryTargetOutcome> Outcomes { get; init; } = [];
}

public sealed class PushDeliveryOrchestrationService(
    IPushDeviceRegistrationRepository repository,
    IPushDeliveryPort deliveryPort)
{
    public async Task<PushDeliveryBatchResult> DeliverToUserAsync(
        Guid userId,
        PushNotificationMessage message,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? includedRegistrationIds = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        ArgumentNullException.ThrowIfNull(message);
        if (attemptedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Attempt timestamp must be UTC.", nameof(attemptedAtUtc));

        cancellationToken.ThrowIfCancellationRequested();
        var registrations = await repository.ListActiveByUserAsync(userId, cancellationToken);

        var delivered = 0;
        var failed = 0;
        var invalidTokensDeactivated = 0;
        var outcomes = new List<PushDeliveryTargetOutcome>();

        foreach (var registration in registrations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!registration.IsActive) continue;
            if (includedRegistrationIds is not null && !includedRegistrationIds.Contains(registration.Id.Value)) continue;

            var result = await deliveryPort.DeliverAsync(
                new PushDeliveryTarget(
                    registration.Id,
                    registration.UserId,
                    registration.Platform,
                    registration.PushToken),
                message,
                cancellationToken);

            outcomes.Add(new PushDeliveryTargetOutcome(registration.Id, result.Accepted, result.FailureKind));

            if (result.Accepted)
            {
                delivered++;
                continue;
            }

            failed++;
            if (result.FailureKind != PushDeliveryFailureKind.InvalidToken) continue;

            registration.Deactivate(attemptedAtUtc);
            await repository.UpdateAsync(registration, cancellationToken);
            invalidTokensDeactivated++;
        }

        return new PushDeliveryBatchResult(
            userId,
            outcomes.Count,
            delivered,
            failed,
            invalidTokensDeactivated)
        {
            Outcomes = outcomes
        };
    }
}
