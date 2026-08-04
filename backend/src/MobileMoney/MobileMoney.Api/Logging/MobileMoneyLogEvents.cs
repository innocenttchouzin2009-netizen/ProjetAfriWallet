namespace MobileMoney.Production.Logging;

public static class MobileMoneyLogEvents
{
    public const string RequestStarted = "MTN_MOMO_REQUEST_STARTED";
    public const string RequestAccepted = "MTN_MOMO_REQUEST_ACCEPTED";
    public const string RequestRejected = "MTN_MOMO_REQUEST_REJECTED";
    public const string RequestRetried = "MTN_MOMO_REQUEST_RETRIED";
    public const string RequestTimedOut = "MTN_MOMO_REQUEST_TIMED_OUT";
    public const string StatusRead = "MTN_MOMO_STATUS_READ";
    public const string CallbackReceived = "MTN_MOMO_CALLBACK_RECEIVED";
    public const string CallbackRejected = "MTN_MOMO_CALLBACK_REJECTED";
    public const string TransactionCompleted = "MTN_MOMO_TRANSACTION_COMPLETED";
}
