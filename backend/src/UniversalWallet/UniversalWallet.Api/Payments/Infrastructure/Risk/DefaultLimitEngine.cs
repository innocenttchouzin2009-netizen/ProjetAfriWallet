using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Limits;
using UniversalWallet.Api.Payments.Application.Intents;

namespace UniversalWallet.Api.Payments.Infrastructure.Risk;

public sealed class DefaultLimitEngine : IPaymentLimitEngine
{
    public PaymentLimits GetLimits(PaymentIntent intent, PaymentWalletSnapshot wallet, string deviceId, string sessionId)
    {
        return new PaymentLimits
        {
            PerTransactionMinor = 50000,
            DailyMinor = 100000,
            MonthlyMinor = 500000,
            DailyCount = 3,
            CurrencyCode = intent.CurrencyCode,
            Scope = PaymentLimitScope.Wallet
        };
    }

    public LimitValidationResult Validate(PaymentIntent intent, PaymentLimits limits)
    {
        if (intent.AmountMinor > limits.PerTransactionMinor)
        {
            return new LimitValidationResult { IsValid = false, ErrorCode = "PAYMENT_LIMIT_EXCEEDED" };
        }

        if (intent.AmountMinor > limits.DailyMinor)
        {
            return new LimitValidationResult { IsValid = false, ErrorCode = "PAYMENT_DAILY_LIMIT_EXCEEDED" };
        }

        if (intent.AmountMinor > limits.MonthlyMinor)
        {
            return new LimitValidationResult { IsValid = false, ErrorCode = "PAYMENT_MONTHLY_LIMIT_EXCEEDED" };
        }

        return new LimitValidationResult { IsValid = true };
    }
}
