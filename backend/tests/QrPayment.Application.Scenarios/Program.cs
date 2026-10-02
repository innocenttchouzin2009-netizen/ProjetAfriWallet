using AfriWallet.Merchant.Application.Contracts.QrPayments;
using AfriWallet.Merchant.Application.QrPayments;
using AfriWallet.Merchant.Application.Services;

var passed = 0;
var gateway = new QrPaymentGateway(new QrPaymentService());
var application = new QrPaymentApplicationService(gateway);

var staticQr = await application.GenerateAsync(
    new GenerateQrRequest(
        "merchant-001",
        QrPaymentContractValues.Types.Static,
        7500m,
        "xaf",
        "AfWal Market",
        "Order 42"));

Check(
    "static QR generation maps contract and normalizes currency",
    staticQr.Type == QrPaymentContractValues.Types.Static &&
    staticQr.Amount == 7500m &&
    staticQr.Currency == "XAF" &&
    staticQr.Status == QrPaymentContractValues.Statuses.Active &&
    staticQr.Code.StartsWith("AFW|Static|merchant-001|7500|XAF|", StringComparison.Ordinal),
    ref passed);

var dynamicQr = await application.GenerateAsync(
    new GenerateQrRequest(
        "merchant-002",
        QrPaymentContractValues.Types.Dynamic,
        0m,
        "EUR",
        "AfWal Services",
        "Open amount"));

Check(
    "dynamic QR generation remains open amount and expiring",
    dynamicQr.Type == QrPaymentContractValues.Types.Dynamic &&
    dynamicQr.Amount == 0m &&
    dynamicQr.ExpiresAt is not null,
    ref passed);

var decoded = await application.DecodeAsync(
    new DecodeQrRequest(staticQr.Code));

Check(
    "decode maps authoritative QR fields",
    decoded.Type == staticQr.Type &&
    decoded.MerchantId == staticQr.MerchantId &&
    decoded.Amount == staticQr.Amount &&
    decoded.Currency == staticQr.Currency &&
    decoded.MerchantName == staticQr.MerchantName,
    ref passed);

await ExpectInvalidOperationAsync(
    "decode rejects unsupported QR type",
    () => application.DecodeAsync(
        new DecodeQrRequest(
            "AFW|Unknown|merchant-001|7500|XAF|AfWal Market|Order 42")));
passed++;

await ExpectInvalidOperationAsync(
    "static initiation rejects amount mismatch",
    () => application.InitiateAsync(
        new InitiateQrPaymentRequest(
            staticQr.QrId,
            "wallet-001",
            7000m,
            "XAF")));
passed++;

var stillActive = await gateway.FindByQrIdAsync(staticQr.QrId);
Check(
    "failed static initiation leaves QR active",
    stillActive?.Status == QrPaymentContractValues.Statuses.Active &&
    stillActive.TransferIntentId is null,
    ref passed);

var initiated = await application.InitiateAsync(
    new InitiateQrPaymentRequest(
        staticQr.QrId,
        "wallet-001",
        7500m,
        "XAF"));

Check(
    "valid static QR initiation creates transfer intent",
    initiated.Status == QrPaymentContractValues.Statuses.Initiated &&
    !string.IsNullOrWhiteSpace(initiated.TransferIntentId),
    ref passed);

var timeline = await application.GetTimelineAsync(
    initiated.TransferIntentId!);

Check(
    "timeline exposes ordered QR application events",
    timeline.Items.Count == 2 &&
    timeline.Items[0].Contains("QR received", StringComparison.Ordinal) &&
    timeline.Items[1].Contains("Payment initiated", StringComparison.Ordinal),
    ref passed);

var receipt = await application.GenerateReceiptAsync(
    new QrReceiptRequest(initiated.TransferIntentId!));

Check(
    "receipt generation returns certified identifiers",
    receipt.ReceiptId.StartsWith("receipt-", StringComparison.Ordinal) &&
    receipt.ReceiptCode.StartsWith("AFW-", StringComparison.Ordinal),
    ref passed);

var repeatedReceipt = await application.GenerateReceiptAsync(
    new QrReceiptRequest(initiated.TransferIntentId!));

Check(
    "receipt generation is idempotent after first success",
    repeatedReceipt == receipt,
    ref passed);

var paid = await gateway.FindByQrIdAsync(staticQr.QrId);
Check(
    "receipt completion marks QR payment paid",
    paid?.Status == QrPaymentContractValues.Statuses.Paid &&
    paid.ReceiptId == receipt.ReceiptId &&
    paid.ReceiptCode == receipt.ReceiptCode,
    ref passed);

var dynamicInitiated = await application.InitiateAsync(
    new InitiateQrPaymentRequest(
        dynamicQr.QrId,
        "wallet-002",
        4200m,
        "EUR"));

Check(
    "dynamic initiation captures payer-selected amount",
    dynamicInitiated.Amount == 4200m &&
    dynamicInitiated.Currency == "EUR" &&
    dynamicInitiated.Status == QrPaymentContractValues.Statuses.Initiated,
    ref passed);

await ExpectInvalidOperationAsync(
    "receipt rejects unknown transfer",
    () => application.GenerateReceiptAsync(
        new QrReceiptRequest("missing-transfer")));
passed++;

Console.WriteLine(
    $"QR payment application scenarios passed: {passed}/13");

static void Check(
    string name,
    bool condition,
    ref int passed)
{
    if (!condition)
    {
        throw new InvalidOperationException(
            $"Scenario failed: {name}");
    }

    passed++;
    Console.WriteLine($"PASS: {name}");
}

static async Task ExpectInvalidOperationAsync(
    string name,
    Func<Task> action)
{
    try
    {
        await action();
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine($"PASS: {name}");
        return;
    }

    throw new InvalidOperationException(
        $"Scenario failed: {name}");
}
