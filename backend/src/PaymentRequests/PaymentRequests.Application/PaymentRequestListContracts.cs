using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestMailbox
{
    Inbox = 1,
    Outbox = 2
}

public sealed record PaymentRequestListQuery(
    Guid UserId,
    PaymentRequestMailbox Mailbox,
    PaymentRequestStatus? Status = null,
    int PageSize = 50,
    string? Cursor = null);

public sealed record PaymentRequestListPage(
    IReadOnlyList<PaymentRequestSnapshot> Items,
    string? NextCursor);
