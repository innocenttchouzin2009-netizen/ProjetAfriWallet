namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed class QrPaymentContractException(
    string code,
    string message)
    : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
