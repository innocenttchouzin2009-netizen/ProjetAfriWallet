namespace Accounting.Contracts.Requests;

public sealed record CreateAccountingPeriodRequest(
    string PeriodCode,
    DateOnly StartDate,
    DateOnly EndDate);