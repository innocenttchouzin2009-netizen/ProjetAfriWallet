using System.Security.Cryptography;
using AfriWallet.Merchants.Registry.Application.Abstractions;
using AfriWallet.Merchants.Registry.Application.Commands;
using AfriWallet.Merchants.Registry.Application.Results;
using AfriWallet.Merchants.Registry.Domain.Merchants;

namespace AfriWallet.Merchants.Registry.Application.Services;

public sealed class MerchantRegistryService(
    IMerchantRepository repository,
    IMerchantAuditStore audit,
    IMerchantClock clock)
{
    public async Task<MerchantResult> CreateAsync(CreateMerchantCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.OwnerAwid))
            throw new ArgumentException("Owner AWID is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Actor))
            throw new ArgumentException("Actor is required.", nameof(command));
        ArgumentNullException.ThrowIfNull(command.Profile);

        var existingOwner = await repository.GetByOwnerAwidAsync(command.OwnerAwid, cancellationToken);
        if (existingOwner is not null)
            throw new InvalidOperationException("Owner AWID already has a registered merchant.");

        var duplicateLegalName = await repository.ExistsByLegalNameAsync(command.Profile.LegalName, command.Profile.CountryCode, cancellationToken);
        if (duplicateLegalName)
            throw new InvalidOperationException("A merchant with this legal name already exists in this country.");

        var merchant = new Merchant(GenerateMerchantId(), command.OwnerAwid, command.Profile, clock.UtcNow);
        await repository.AddAsync(merchant, cancellationToken);
        await AuditAsync(merchant, "merchant.created", command.Actor, cancellationToken);
        return Map(merchant);
    }

    public Task<MerchantResult> RegisterAsync(RegisterMerchantCommand command, CancellationToken cancellationToken = default) =>
        MutateAsync(command.MerchantId, command.Actor, "merchant.registered", (merchant, now) => merchant.Register(now), cancellationToken);

    public Task<MerchantResult> SetCapabilitiesAsync(SetMerchantCapabilitiesCommand command, CancellationToken cancellationToken = default) =>
        MutateAsync(command.MerchantId, command.Actor, "merchant.capabilities_set",
            (merchant, now) => merchant.SetCapabilities(command.Capabilities, now), cancellationToken);

    public Task<MerchantResult> UpdateProfileAsync(UpdateMerchantProfileCommand command, CancellationToken cancellationToken = default) =>
        MutateAsync(command.MerchantId, command.Actor, "merchant.profile_updated",
            (merchant, now) => merchant.UpdateProfile(command.Profile, now), cancellationToken);

    public Task<MerchantResult> ChangeStatusAsync(ChangeMerchantStatusCommand command, CancellationToken cancellationToken = default) =>
        MutateAsync(command.MerchantId, command.Actor, $"merchant.status_changed.{command.TargetStatus.ToString().ToLowerInvariant()}",
            (merchant, now) => ApplyStatusTransition(merchant, command.TargetStatus, now), cancellationToken);

    public async Task<MerchantResult> GetAsync(string merchantId, CancellationToken cancellationToken = default) =>
        Map(await RequireAsync(merchantId, cancellationToken));

    private static void ApplyStatusTransition(Merchant merchant, MerchantStatus targetStatus, DateTimeOffset now)
    {
        switch (targetStatus)
        {
            case MerchantStatus.PendingVerification:
                merchant.BeginVerification(now);
                break;
            case MerchantStatus.Active when merchant.Status == MerchantStatus.Suspended:
                merchant.Resume(now);
                break;
            case MerchantStatus.Active:
                merchant.Activate(now);
                break;
            case MerchantStatus.Suspended:
                merchant.Suspend(now);
                break;
            case MerchantStatus.Closed:
                merchant.Close(now);
                break;
            default:
                throw new InvalidOperationException("Unsupported merchant status transition.");
        }
    }

    private async Task<MerchantResult> MutateAsync(
        string merchantId,
        string actor,
        string eventType,
        Action<Merchant, DateTimeOffset> mutation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor is required.", nameof(actor));

        var merchant = await RequireAsync(merchantId, cancellationToken);
        mutation(merchant, clock.UtcNow);
        await repository.SaveAsync(merchant, cancellationToken);
        await AuditAsync(merchant, eventType, actor, cancellationToken);
        return Map(merchant);
    }

    private async Task<Merchant> RequireAsync(string merchantId, CancellationToken cancellationToken) =>
        await repository.GetAsync(new MerchantId(merchantId), cancellationToken)
        ?? throw new KeyNotFoundException("Merchant not found.");

    private async Task AuditAsync(Merchant merchant, string eventType, string actor, CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new MerchantAuditEvent(
                Guid.NewGuid(),
                merchant.MerchantId.ToString(),
                merchant.OwnerAwid,
                eventType,
                actor,
                clock.UtcNow,
                new Dictionary<string, string>
                {
                    ["status"] = merchant.Status.ToString(),
                    ["country"] = merchant.Profile.CountryCode,
                    ["currency"] = merchant.Profile.SettlementCurrency,
                    ["capabilityCount"] = merchant.Capabilities.Count.ToString(),
                    ["kybPerformed"] = "false",
                    ["paymentAcceptancePerformed"] = "false",
                    ["paymentCapturePerformed"] = "false",
                    ["settlementPerformed"] = "false",
                    ["payoutPerformed"] = "false",
                    ["moneyMovementPerformed"] = "false",
                    ["ledgerMutationPerformed"] = "false"
                }),
            cancellationToken);
    }

    private static MerchantId GenerateMerchantId()
    {
        var bytes = RandomNumberGenerator.GetBytes(8);
        return new MerchantId($"AFM-{Convert.ToHexString(bytes)}");
    }

    private static MerchantResult Map(Merchant merchant) => new(
        merchant.MerchantId.ToString(),
        merchant.OwnerAwid,
        merchant.Status,
        merchant.Profile,
        merchant.Capabilities.ToArray(),
        merchant.CreatedAtUtc,
        merchant.UpdatedAtUtc);
}
