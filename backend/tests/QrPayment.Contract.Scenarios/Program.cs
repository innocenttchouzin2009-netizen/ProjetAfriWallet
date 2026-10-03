using AfriWallet.Merchant.Application.Contracts.QrPayments;

var passed = 0;

Check(
    "public QR status values remain stable",
    QrPaymentContractValues.Statuses.Active == "Active" &&
    QrPaymentContractValues.Statuses.Initiated == "Initiated" &&
    QrPaymentContractValues.Statuses.Paid == "Paid" &&
    QrPaymentContractValues.Statuses.Expired == "Expired",
    ref passed);

Check(
    "public QR error codes remain stable",
    QrPaymentContractValues.ErrorCodes.Validation == "QR_PAYMENT_VALIDATION_ERROR" &&
    QrPaymentContractValues.ErrorCodes.NotFound == "QR_PAYMENT_NOT_FOUND" &&
    QrPaymentContractValues.ErrorCodes.IdempotencyConflict == "QR_PAYMENT_IDEMPOTENCY_CONFLICT",
    ref passed);

var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
var initiation = new InitiateQrPaymentRequest(
    "qr-001",
    "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    7500,
    "XAF",
    "idem-001",
    userId);

Check(
    "initiation contract carries qrId, authenticated actor and idempotency",
    initiation.QrId == "qr-001" &&
    initiation.AuthenticatedUserId == userId &&
    initiation.IdempotencyKey == "idem-001" &&
    initiation.AmountMinor == 7500,
    ref passed);

var decoded = new DecodedQrResponse(
    "qr-001",
    QrPaymentContractValues.Types.Static,
    "merchant-001",
    7500,
    "XAF",
    "AfWal Market",
    "Order 42",
    QrPaymentContractValues.Statuses.Active,
    null);

Check(
    "decode response carries authoritative qrId",
    decoded.QrId == initiation.QrId &&
    decoded.AmountMinor == initiation.AmountMinor,
    ref passed);

var updatedAt = DateTimeOffset.Parse("2026-10-03T09:00:00+00:00");
var status = new QrPaymentStatusResponse(
    "payment-001",
    "qr-001",
    "transfer-001",
    QrPaymentContractValues.Statuses.Initiated,
    7500,
    "XAF",
    null,
    null,
    updatedAt);

Check(
    "status response is read-model only and preserves identifiers",
    status.QrId == "qr-001" &&
    status.TransferIntentId == "transfer-001" &&
    status.UpdatedAt == updatedAt,
    ref passed);

Console.WriteLine($"QR payment contract scenarios passed: {passed}/5");

static void Check(string name, bool condition, ref int passed)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Scenario failed: {name}");
    }

    passed++;
    Console.WriteLine($"PASS: {name}");
}
