using AfriWallet.Merchant.Application.Services;
using AfriWallet.Merchant.Domain.Entities;

var service = new PosService();

var terminal = service.RegisterTerminal(new PosTerminal
{
    MerchantId = "merchant-100",
    TerminalCode = "T-1001",
    DisplayName = "Checkout A",
    Status = PosTerminalStatus.Active,
    CountryCode = "CM",
    CurrencyCode = "XAF",
    Capabilities = ["QR", "WEB_CHECKOUT"]
});

AssertTrue(terminal.TerminalId is not null, "terminal registration");

var heartbeat = service.Heartbeat(terminal.TerminalId!);
AssertTrue(heartbeat.Status == PosTerminalStatus.Active, "heartbeat");

var checkout = service.CreateCheckout(new PosCheckoutRequest("merchant-100", "T-1001", 1500m, "XAF", "Coffee purchase"));
AssertTrue(checkout.TransactionId is not null, "checkout creation");

var payment = service.InitiatePayment(new PosPaymentRequest
{
    MerchantId = "merchant-100",
    TerminalId = terminal.TerminalId!,
    AmountMinor = 1500m,
    CurrencyCode = "XAF",
    Channel = PosChannel.QrCode,
    Description = "Coffee purchase"
});
AssertTrue(payment.TransferIntentId is not null, "pos payment request");
AssertTrue(payment.Status == PosTransactionStatus.Initiated, "transfer intent created");

var completed = service.CompletePayment(payment.TransactionId!);
AssertTrue(completed.Status == PosTransactionStatus.Completed, "payment completed");

var receipt = service.GenerateReceipt(completed.TransactionId!);
AssertTrue(receipt.ReceiptId is not null, "receipt generation");

AssertTrue(service.GetAuditEvents("merchant-100").Count >= 3, "audit generation");
AssertTrue(service.GetTelemetryEvents("merchant-100").Count >= 3, "telemetry generation");

Console.WriteLine("terminal registration ............... PASS");
Console.WriteLine("heartbeat ........................... PASS");
Console.WriteLine("checkout creation ................... PASS");
Console.WriteLine("pos payment request ................. PASS");
Console.WriteLine("transfer intent created ............. PASS");
Console.WriteLine("payment completed ................... PASS");
Console.WriteLine("receipt generation .................. PASS");
Console.WriteLine("audit generation .................... PASS");
Console.WriteLine("telemetry generation ................ PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0009.4 merchant POS scenarios passed.");

static void AssertTrue(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{label} failed");
    }
}
