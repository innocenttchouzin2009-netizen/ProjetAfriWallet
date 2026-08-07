using Accounting.Domain.Accounts;

namespace Accounting.Contracts.Requests;

public sealed record CreateGeneralLedgerAccountRequest(
    string AccountCode,
    string DisplayName,
    string CurrencyCode,
    GeneralLedgerAccountType Type);