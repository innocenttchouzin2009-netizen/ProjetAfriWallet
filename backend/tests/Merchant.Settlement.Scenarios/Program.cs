using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

var service = new SettlementService();

var created = service.CreateInstruction(new SettlementInstruction
{
    MerchantId = "merchant-300",
    MerchantWalletId = "wallet-300",
    PaymentReference = "payment-001",
    GrossAmountMinor = 10000m,
    FeeAmountMinor = 250m,
    TaxAmountMinor = 100m,
    CurrencyCode = "XAF",
    SettlementMethod = SettlementMethod.AFRIWALLET_WALLET,
    DestinationAccount = "wallet-300",
    Status = SettlementStatus.CREATED
});
AssertTrue(created.SettlementId is not null, "settlement creation");

var fee = service.CalculateFee(created.GrossAmountMinor, created.SettlementMethod);
AssertTrue(fee == 250m, "fee calculation");

var tax = service.CalculateTax(created.GrossAmountMinor, created.SettlementMethod);
AssertTrue(tax == 100m, "tax calculation");

var batch = service.CreateBatch("merchant-300", "batch-001", [created.SettlementId]);
AssertTrue(batch.BatchId is not null, "batch settlement");

var wallet = service.ExecuteInstruction(created.SettlementId, SettlementMethod.AFRIWALLET_WALLET);
AssertTrue(wallet.Status == SettlementStatus.COMPLETED, "wallet settlement");

var bank = service.CreateInstruction(new SettlementInstruction
{
    MerchantId = "merchant-301",
    MerchantWalletId = "wallet-301",
    PaymentReference = "payment-002",
    GrossAmountMinor = 20000m,
    FeeAmountMinor = 400m,
    TaxAmountMinor = 200m,
    CurrencyCode = "XAF",
    SettlementMethod = SettlementMethod.BANK_TRANSFER,
    DestinationAccount = "bank-001",
    Status = SettlementStatus.CREATED
});
var bankSettlement = service.ExecuteInstruction(bank.SettlementId, SettlementMethod.BANK_TRANSFER);
AssertTrue(bankSettlement.Status == SettlementStatus.COMPLETED, "bank settlement");

var momo = service.CreateInstruction(new SettlementInstruction
{
    MerchantId = "merchant-302",
    MerchantWalletId = "wallet-302",
    PaymentReference = "payment-003",
    GrossAmountMinor = 15000m,
    FeeAmountMinor = 300m,
    TaxAmountMinor = 150m,
    CurrencyCode = "XAF",
    SettlementMethod = SettlementMethod.MTN_MOMO,
    DestinationAccount = "mtn-001",
    Status = SettlementStatus.CREATED
});
var momoSettlement = service.ExecuteInstruction(momo.SettlementId, SettlementMethod.MTN_MOMO);
AssertTrue(momoSettlement.Status == SettlementStatus.COMPLETED, "mobile money settlement");

var failed = service.CreateInstruction(new SettlementInstruction
{
    MerchantId = "merchant-303",
    MerchantWalletId = "wallet-303",
    PaymentReference = "payment-004",
    GrossAmountMinor = 5000m,
    FeeAmountMinor = 100m,
    TaxAmountMinor = 50m,
    CurrencyCode = "XAF",
    SettlementMethod = SettlementMethod.ORANGE_MONEY,
    DestinationAccount = "orange-001",
    Status = SettlementStatus.CREATED
});
var failedInstruction = service.FailInstruction(failed.SettlementId, "simulated failure");
var recovered = service.RecoverInstruction(failedInstruction.SettlementId);
AssertTrue(recovered.Status == SettlementStatus.COMPLETED, "failed settlement recovery");

AssertTrue(service.GetAuditEvents("merchant-300").Count >= 3, "audit generation");
AssertTrue(service.GetTelemetryEvents("merchant-300").Count >= 3, "telemetry generation");

Console.WriteLine("settlement creation ................. PASS");
Console.WriteLine("fee calculation ..................... PASS");
Console.WriteLine("tax calculation ..................... PASS");
Console.WriteLine("batch settlement .................... PASS");
Console.WriteLine("wallet settlement ................... PASS");
Console.WriteLine("bank settlement ..................... PASS");
Console.WriteLine("mobile money settlement .............. PASS");
Console.WriteLine("failed settlement recovery .......... PASS");
Console.WriteLine("audit generation .................... PASS");
Console.WriteLine("telemetry generation ................ PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0009.5 merchant settlement scenarios passed.");

static void AssertTrue(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{label} failed");
    }
}
