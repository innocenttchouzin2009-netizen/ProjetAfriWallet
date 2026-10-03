using AfriWallet.Merchant.Application.Contracts.QrPayments;
using AfriWallet.Merchant.Application.QrPayments;
using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

var passed = 0;
var legacy = new QrPaymentService();
var contract = new QrPaymentContractService(legacy);
var owner = Guid.Parse("11111111-2222-3333-4444-555555555555");
var foreign = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

var generated = legacy.GenerateQr(
    new GenerateQrCommand(
        "merchant-001",
        QrPaymentType.Static,
        7500m,
        "XAF",
        "AfWal Market",
        "Order 42"));

var decoded = await contract.DecodeAsync(new DecodeQrRequest(generated.Code));
Check(
    "decode resolves authoritative qrId",
    decoded.QrId == generated.QrId &&
    decoded.MerchantId == generated.MerchantId &&
    decoded.AmountMinor == 7500,
    ref passed);

var request = new InitiateQrPaymentRequest(
    generated.QrId,
    "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
    7500,
    "XAF",
    "idem-001",
    owner);

var first = await contract.InitiateAsync(request);
Check(
    "authenticated initiation creates one transfer intent",
    first.Status == QrPaymentContractValues.Statuses.Initiated &&
    !string.IsNullOrWhiteSpace(first.TransferIntentId),
    ref passed);

var replay = await contract.InitiateAsync(request);
Check(
    "identical idempotent replay returns same transfer intent",
    replay.TransferIntentId == first.TransferIntentId &&
    replay.UpdatedAt == first.UpdatedAt,
    ref passed);

await ExpectCodeAsync(
    "idempotency key reuse with different request is rejected",
    QrPaymentContractValues.ErrorCodes.IdempotencyConflict,
    () => contract.InitiateAsync(request with { AmountMinor = 7600 }));
passed++;

var beforeStatus = generated.UpdatedAt;
var status1 = await contract.GetStatusAsync(owner, first.TransferIntentId);
var status2 = await contract.GetStatusAsync(owner, first.TransferIntentId);

Check(
    "status read is side-effect free",
    status1 == status2 &&
    generated.UpdatedAt == beforeStatus,
    ref passed);

await ExpectCodeAsync(
    "status is concealed from a different authenticated user",
    QrPaymentContractValues.ErrorCodes.NotFound,
    () => contract.GetStatusAsync(foreign, first.TransferIntentId));
passed++;

var dynamic = legacy.GenerateQr(
    new GenerateQrCommand(
        "merchant-002",
        QrPaymentType.Dynamic,
        0m,
        "EUR",
        "AfWal Services",
        "Open amount"));

var dynamicResult = await contract.InitiateAsync(
    new InitiateQrPaymentRequest(
        dynamic.QrId,
        "cccccccc-dddd-eeee-ffff-000000000000",
        4200,
        "EUR",
        "idem-002",
        owner));

Check(
    "dynamic initiation captures payer-selected minor amount",
    dynamicResult.AmountMinor == 4200 &&
    dynamicResult.Currency == "EUR",
    ref passed);

Console.WriteLine($"QR payment application scenarios passed: {passed}/7");

static async Task ExpectCodeAsync(
    string name,
    string expectedCode,
    Func<Task> action)
{
    try
    {
        await action();
    }
    catch (QrPaymentContractException exception) when (exception.Code == expectedCode)
    {
        Console.WriteLine($"PASS: {name}");
        return;
    }

    throw new InvalidOperationException($"Scenario failed: {name}");
}

static void Check(string name, bool condition, ref int passed)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    passed++;
    Console.WriteLine($"PASS: {name}");
}
