namespace Accounting.Domain.Periods;

public enum AccountingPeriodStatus
{
    Draft,
    Open,
    Closed
}

public sealed class AccountingPeriod
{
    public Guid PeriodId { get; }
    public string PeriodCode { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public AccountingPeriodStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? OpenedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public AccountingPeriod(
        Guid periodId,
        string periodCode,
        DateOnly startDate,
        DateOnly endDate,
        DateTime? createdAtUtc = null)
    {
        if (periodId == Guid.Empty)
            throw new ArgumentException("Period identifier is required.", nameof(periodId));

        if (endDate < startDate)
            throw new ArgumentException("End date must be on or after the start date.", nameof(endDate));

        PeriodId = periodId;
        PeriodCode = RequireText(periodCode, nameof(periodCode));
        StartDate = startDate;
        EndDate = endDate;
        Status = AccountingPeriodStatus.Draft;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public void Open()
    {
        if (Status == AccountingPeriodStatus.Open)
            return;

        if (Status == AccountingPeriodStatus.Closed)
            throw new InvalidOperationException("Closed accounting periods cannot be reopened.");

        Status = AccountingPeriodStatus.Open;
        OpenedAtUtc = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status == AccountingPeriodStatus.Closed)
            return;

        Status = AccountingPeriodStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
    }

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        return value.Trim();
    }
}