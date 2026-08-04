namespace MobileMoney.Production.Audit;

public sealed class AuditSearchService
{
    private readonly AuditRepository _repository;

    public AuditSearchService(AuditRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<AuditRecord> Search(AuditSearchCriteria criteria)
    {
        return _repository.Search(record =>
            (string.IsNullOrWhiteSpace(criteria.AuditId) || record.AuditId == criteria.AuditId) &&
            (string.IsNullOrWhiteSpace(criteria.TransactionId) || record.TransactionId == criteria.TransactionId) &&
            (string.IsNullOrWhiteSpace(criteria.CorrelationId) || record.CorrelationId == criteria.CorrelationId) &&
            (string.IsNullOrWhiteSpace(criteria.ProviderReference) || record.ProviderReference == criteria.ProviderReference) &&
            (string.IsNullOrWhiteSpace(criteria.WalletId) || record.WalletId == criteria.WalletId) &&
            (!criteria.From.HasValue || record.TimestampUtc >= criteria.From.Value) &&
            (!criteria.To.HasValue || record.TimestampUtc <= criteria.To.Value) &&
            (criteria.Action is null || record.Action == criteria.Action) &&
            (criteria.Result is null || record.Result == criteria.Result));
    }
}

public sealed class AuditSearchCriteria
{
    public string? AuditId { get; init; }
    public string? TransactionId { get; init; }
    public string? CorrelationId { get; init; }
    public string? ProviderReference { get; init; }
    public string? WalletId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public AuditAction? Action { get; init; }
    public AuditResult? Result { get; init; }
}
