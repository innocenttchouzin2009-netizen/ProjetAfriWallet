namespace AfriWallet.PaymentRequests.Domain;

public enum PaymentRequestStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Expired = 4,
    Cancelled = 5,
    Paid = 6
}
