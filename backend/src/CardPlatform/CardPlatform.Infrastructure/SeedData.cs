using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Infrastructure;

internal static class SeedData
{
    public static List<CardProgram> CreateSeedPrograms()
    {
        return
        [
            new CardProgram
            {
                ProgramId = "card-program-visa-virtual-sandbox",
                ProgramCode = "afriwallet-visa-virtual-sandbox",
                DisplayName = "AfriWallet Visa Virtual Sandbox",
                Network = "Visa",
                CardType = "virtual",
                FundingType = "prepaid",
                CountryCode = "CM",
                BaseCurrency = "XAF",
                SupportedCurrencies = ["XAF", "EUR", "USD"],
                Environment = "Sandbox",
                Status = "Active",
                Capabilities = new CardProgramCapabilities
                {
                    OnlinePayments = true,
                    InStorePayments = true,
                    ContactlessPayments = true,
                    InternationalPayments = true,
                    RecurringPayments = true
                },
                Limits = new CardProgramLimits
                {
                    SingleTransactionLimitMinor = 500_000,
                    DailyLimitMinor = 2_000_000,
                    MonthlyLimitMinor = 10_000_000
                },
                Fees = new CardProgramFees
                {
                    CardIssueFeeMinor = 0,
                    AnnualFeeMinor = 0,
                    ForeignExchangeMarkupPercent = 0.5m
                },
                Priority = 100
            },
            new CardProgram
            {
                ProgramId = "card-program-mastercard-virtual-sandbox",
                ProgramCode = "afriwallet-mastercard-virtual-sandbox",
                DisplayName = "AfriWallet Mastercard Virtual Sandbox",
                Network = "Mastercard",
                CardType = "virtual",
                FundingType = "prepaid",
                CountryCode = "CM",
                BaseCurrency = "XAF",
                SupportedCurrencies = ["XAF", "EUR", "USD"],
                Environment = "Sandbox",
                Status = "Active",
                Capabilities = new CardProgramCapabilities
                {
                    OnlinePayments = true,
                    InStorePayments = true,
                    ContactlessPayments = true,
                    InternationalPayments = true,
                    RecurringPayments = false
                },
                Limits = new CardProgramLimits
                {
                    SingleTransactionLimitMinor = 400_000,
                    DailyLimitMinor = 1_500_000,
                    MonthlyLimitMinor = 8_000_000
                },
                Fees = new CardProgramFees
                {
                    CardIssueFeeMinor = 0,
                    AnnualFeeMinor = 0,
                    ForeignExchangeMarkupPercent = 0.4m
                },
                Priority = 90
            },
            new CardProgram
            {
                ProgramId = "card-program-visa-prepaid-sandbox",
                ProgramCode = "afriwallet-visa-prepaid-sandbox",
                DisplayName = "AfriWallet Visa Prepaid Sandbox",
                Network = "Visa",
                CardType = "physical",
                FundingType = "prepaid",
                CountryCode = "CM",
                BaseCurrency = "XAF",
                SupportedCurrencies = ["XAF", "USD"],
                Environment = "Sandbox",
                Status = "Active",
                Capabilities = new CardProgramCapabilities
                {
                    OnlinePayments = true,
                    InStorePayments = true,
                    AtmWithdrawals = true,
                    ContactlessPayments = true,
                    InternationalPayments = true,
                    RecurringPayments = false
                },
                Limits = new CardProgramLimits
                {
                    SingleTransactionLimitMinor = 300_000,
                    DailyLimitMinor = 1_000_000,
                    MonthlyLimitMinor = 6_000_000
                },
                Fees = new CardProgramFees
                {
                    CardIssueFeeMinor = 1_000,
                    AnnualFeeMinor = 2_000,
                    ForeignExchangeMarkupPercent = 0.6m
                },
                Priority = 80
            }
        ];
    }
}
