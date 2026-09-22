using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Domain.Settlements;
using Settlement.Application.Services;
using Settlement.Domain.Instructions;

namespace AfriWallet.Merchants.Settlement.Infrastructure;

public sealed class MultiCurrencySettlementHandoffProvider(
    IMerchantSettlementAccountResolver accountResolver,
    MultiCurrencySettlementService settlementService)
    : IMerchantSettlementProvider
{
    public async Task<MerchantSettlementProviderResult> SubmitAsync(
        MerchantSettlementProviderRequest request,
        CancellationToken ct = default)
    {
        if (request.Route != MerchantSettlementRoute.MerchantSettlement)
            return new(
                MerchantSettlementProviderStatus.PermanentFailure,
                null,
                "Merchant payout is outside AFW-BE-MERCHANT-SETTLEMENT-HANDOFF-1.");

        var route = await accountResolver.ResolveAsync(request.MerchantId, request.Route, request.Currency, ct);
        if (route is null)
            return new(
                MerchantSettlementProviderStatus.PermanentFailure,
                null,
                "Merchant settlement account route is not configured.");

        if (!string.Equals(route.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            return new(
                MerchantSettlementProviderStatus.PermanentFailure,
                null,
                "Merchant settlement account currency does not match the payment decision currency.");

        try
        {
            var instruction = await settlementService.CreateInstructionAsync(
                route.SourceAccountId,
                route.DestinationAccountId,
                request.Currency,
                route.Currency,
                request.AmountMinor,
                ct);

            var executed = await settlementService.ExecuteInstructionAsync(instruction.InstructionId, ct);

            return executed.Status switch
            {
                SettlementInstructionStatus.Settled => new(
                    MerchantSettlementProviderStatus.Accepted,
                    executed.InstructionId.ToString("D"),
                    "Merchant settlement handed off to durable Settlement core and settled."),
                SettlementInstructionStatus.Rejected => new(
                    MerchantSettlementProviderStatus.PermanentFailure,
                    executed.InstructionId.ToString("D"),
                    executed.RejectionReason ?? "Settlement core rejected the instruction."),
                _ => new(
                    MerchantSettlementProviderStatus.TemporaryFailure,
                    executed.InstructionId.ToString("D"),
                    "Settlement core did not reach a terminal result.")
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            return new(MerchantSettlementProviderStatus.PermanentFailure, null, exception.Message);
        }
        catch (Exception exception)
        {
            return new(MerchantSettlementProviderStatus.TemporaryFailure, null, exception.Message);
        }
    }

    public Task<bool> CompensateAsync(string providerReference, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }
}
