using MerchantSettlement.Application.Interfaces;
using MerchantSettlement.Domain.Profiles;
using MerchantSettlement.Domain.Reconciliation;

namespace MerchantSettlement.Application.Services;

public sealed class MerchantSettlementService
{
    private readonly IMerchantSettlementRepository _repository;
    private readonly MerchantSettlementPositionService _positions;
    private readonly IFinancialSettlementGateway _financialSettlement;
    private readonly IFinancialReconciliationGateway _reconciliation;

    public MerchantSettlementService(
        IMerchantSettlementRepository repository,
        MerchantSettlementPositionService positions,
        IFinancialSettlementGateway financialSettlement,
        IFinancialReconciliationGateway reconciliation)
    {
        _repository = repository;
        _positions = positions;
        _financialSettlement = financialSettlement;
        _reconciliation = reconciliation;
    }

    public async Task<MerchantSettlementProfile> CreateProfileAsync(
        string merchantId,
        string settlementCurrency,
        SettlementFrequency frequency,
        int settlementDelayDays,
        long minimumSettlementMinor,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetProfileAsync(merchantId, cancellationToken);
        if (existing is not null)
            return existing;

        var profile = new MerchantSettlementProfile(
            Guid.NewGuid(),
            merchantId,
            settlementCurrency,
            frequency,
            settlementDelayDays);

        profile.ConfigureMinimum(minimumSettlementMinor);

        await _repository.AddProfileAsync(profile, cancellationToken);

        return profile;
    }

    public async Task<MerchantSettlement.Domain.Settlements.MerchantSettlement> CreateSettlementAsync(
        string merchantId,
        DateTime fromUtc,
        DateTime toUtc,
        long adjustmentsMinor,
        long reserveMinor,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetSettlementByIdempotencyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
            return existing;

        var profile = await _repository.GetProfileAsync(merchantId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant settlement profile not found.");

        if (profile.Status != MerchantSettlementProfileStatus.Active)
            throw new InvalidOperationException("Merchant settlement profile is not active.");

        var position = await _positions.CalculateAsync(
            merchantId,
            profile.SettlementCurrency,
            fromUtc,
            toUtc,
            adjustmentsMinor,
            reserveMinor,
            cancellationToken);

        if (position.NetPayableMinor < profile.MinimumSettlementMinor)
            throw new InvalidOperationException("Merchant settlement minimum has not been reached.");

        var settlement = new MerchantSettlement.Domain.Settlements.MerchantSettlement(
            Guid.NewGuid(),
            merchantId,
            profile.SettlementCurrency,
            position.GrossMinor,
            position.FeesMinor,
            position.RefundsMinor,
            position.AdjustmentsMinor,
            position.ReserveMinor,
            idempotencyKey);

        await _repository.AddSettlementAsync(settlement, cancellationToken);

        return settlement;
    }

    public async Task<MerchantSettlement.Domain.Settlements.MerchantSettlement> ExecuteAsync(
        Guid settlementId,
        CancellationToken cancellationToken)
    {
        var settlement = await RequireSettlementAsync(settlementId, cancellationToken);

        if (settlement.Status == MerchantSettlement.Domain.Settlements.MerchantSettlementStatus.Completed)
            return settlement;

        settlement.Start();

        var financialReference = await _financialSettlement.ExecuteAsync(
            settlement.SettlementId,
            settlement.MerchantId,
            settlement.CurrencyCode,
            settlement.NetPayableMinor,
            cancellationToken);

        settlement.AttachFinancialSettlement(financialReference);
        settlement.Complete();

        return settlement;
    }

    public async Task<MerchantReconciliationResult> ReconcileAsync(
        Guid settlementId,
        CancellationToken cancellationToken)
    {
        var settlement = await RequireSettlementAsync(settlementId, cancellationToken);

        if (settlement.Status != MerchantSettlement.Domain.Settlements.MerchantSettlementStatus.Completed)
            throw new InvalidOperationException("Only completed merchant settlements can be reconciled.");

        if (settlement.FinancialSettlementReference is null)
            throw new InvalidOperationException("Financial settlement reference is missing.");

        return await _reconciliation.ReconcileAsync(
            settlement.SettlementId,
            settlement.MerchantId,
            settlement.FinancialSettlementReference,
            settlement.NetPayableMinor,
            cancellationToken);
    }

    private async Task<MerchantSettlement.Domain.Settlements.MerchantSettlement> RequireSettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken)
    {
        return await _repository.GetSettlementAsync(settlementId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant settlement not found.");
    }
}
