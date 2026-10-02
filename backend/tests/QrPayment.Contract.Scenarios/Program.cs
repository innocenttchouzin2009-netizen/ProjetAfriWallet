using AfriWallet.Merchant.Application.Contracts.QrPayments;

var passed = 0;

Check("type values are stable",
    QrPaymentContractValues.Types.Static == "Static" &&
    QrPaymentContractValues.Types.Dynamic == "Dynamic",
    ref passed);

Check("status values are stable",
    QrPaymentContractValues.Statuses.Active == "Active" &&
    QrPaymentContractValues.Statuses.Initiated == "Initiated" &&
    QrPaymentContractValues.Statuses.Paid == "Paid" &&
    QrPaymentContractValues.Statuses.Expired == "Expired",
    ref passed);

var generate = new GenerateQrRequest(
    "merchant-001",
    QrPaymentContractValues.Types.Static,
    7500m,
    "XAF",
    "AfWal Market",
    "Order 42");

Check("generate request preserves public fields",
    generate.MerchantId == "merchant-001" &&
    generate.Type == "Static" &&
    generate.Amount == 7500m &&
    generate.Currency == "XAF" &&
    generate.MerchantName == "AfWal Market" &&
    generate.Description == "Order 42",
    ref passed);

var dynamicGenerate = generate with
{
    Type = QrPaymentContractValues.Types.Dynamic,
    Amount = 0m
};

Check("dynamic contract can represent zero amount",
    dynamicGenerate.Type == "Dynamic" && dynamicGenerate.Amount == 0m,
    ref passed);

var decoded = new DecodedQrResponse(
    QrPaymentContractValues.Types.Static,
    "merchant-001",
    7500m,
    "XAF",
    "AfWal Market",
    "Order 42");

Check("decode response mirrors QR payload",
    decoded.Type == generate.Type &&
    decoded.MerchantId == generate.MerchantId &&
    decoded.Amount == generate.Amount &&
    decoded.Currency == generate.Currency,
    ref passed);

var initiation = new InitiateQrPaymentRequest(
    "qr-001",
    "wallet-001",
    7500m,
    "XAF");

Check("initiation request preserves payer and amount",
    initiation.QrId == "qr-001" &&
    initiation.PayerWalletId == "wallet-001" &&
    initiation.Amount == 7500m &&
    initiation.Currency == "XAF",
    ref passed);

var createdAt = DateTimeOffset.Parse("2026-10-02T18:00:00+00:00");
var updatedAt = createdAt.AddMinutes(1);
var expiresAt = createdAt.AddHours(24);

var payment = new QrPaymentResponse(
    "payment-001",
    "qr-001",
    "merchant-001",
    QrPaymentContractValues.Types.Dynamic,
    "AFW|Dynamic|merchant-001|0|XAF|AfWal Market|Order 42",
    QrPaymentContractValues.Statuses.Initiated,
    0m,
    "XAF",
    "AfWal Market",
    "Order 42",
    "transfer-001",
    null,
    null,
    createdAt,
    updatedAt,
    expiresAt);

Check("payment response carries lifecycle metadata",
    payment.PaymentId == "payment-001" &&
    payment.TransferIntentId == "transfer-001" &&
    payment.Status == "Initiated" &&
    payment.CreatedAt == createdAt &&
    payment.UpdatedAt == updatedAt &&
    payment.ExpiresAt == expiresAt,
    ref passed);

var receiptRequest = new QrReceiptRequest("transfer-001");
var receipt = new QrReceiptResponse("receipt-001", "AFW-TRANSFER");

Check("receipt contracts preserve identifiers",
    receiptRequest.TransferIntentId == "transfer-001" &&
    receipt.ReceiptId == "receipt-001" &&
    receipt.ReceiptCode == "AFW-TRANSFER",
    ref passed);

var timeline = new QrTimelineResponse(
    "transfer-001",
    new[] { "QR received", "Payment initiated" });

Check("timeline contract preserves order",
    timeline.TransferIntentId == "transfer-001" &&
    timeline.Items.Count == 2 &&
    timeline.Items[0] == "QR received" &&
    timeline.Items[1] == "Payment initiated",
    ref passed);

Console.WriteLine($"QR payment contract scenarios passed: {passed}/9");

static void Check(string name, bool condition, ref int passed)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    passed++;
    Console.WriteLine($"PASS: {name}");
}
