namespace Accounting.Contracts.Requests;

public sealed record ReverseJournalEntryRequest(
    string Reference,
    string Reason);