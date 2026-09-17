using Reconciliation.Application.Matching;
using Reconciliation.Application.Queries;
using Reconciliation.Domain.Matches;
using Reconciliation.Domain.Records;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var fromUtc = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
var toUtc = fromUtc.AddHours(1);

var internalRecords = new[]
{
    new InternalFinancialRecord("i-exact", "ref-exact", "partner-1", "EUR", 1000, fromUtc.AddMinutes(5), "ledger"),
    new InternalFinancialRecord("i-partial-ref", "ref-partial", "partner-1", "EUR", 2000, fromUtc.AddMinutes(20), "ledger"),
    new InternalFinancialRecord("i-partial-amount", "ref-missing", "partner-1", "EUR", 3000, fromUtc.AddMinutes(30), "ledger"),
    new InternalFinancialRecord("i-unmatched", "ref-none", "partner-1", "EUR", 4000, fromUtc.AddMinutes(40), "ledger")
};

var externalRecords = new[]
{
    new ExternalFinancialRecord("e-exact", "ref-exact", "partner-1", "EUR", 1000, fromUtc.AddMinutes(7), "partner"),
    new ExternalFinancialRecord("e-partial-ref", "ref-partial", "partner-1", "EUR", 2150, fromUtc.AddMinutes(22), "partner"),
    new ExternalFinancialRecord("e-partial-amount", "other-reference", "partner-1", "EUR", 3000, fromUtc.AddMinutes(32), "partner"),
    new ExternalFinancialRecord("e-leftover", "unused", "partner-1", "EUR", 9999, fromUtc.AddMinutes(50), "partner")
};

var batch = new ReconciliationCandidateBatch("partner-1", fromUtc, toUtc, internalRecords, externalRecords);
var service = new AutomaticCandidateClassificationService();
var classified = service.Classify(batch);

Assert(classified.PartnerId == "partner-1", "Partner must be preserved.");
Assert(classified.FromUtc == fromUtc && classified.ToUtc == toUtc, "Window must be preserved.");
Assert(classified.Candidates.Count == 5, "Every internal and leftover external record must be represented exactly once.");

var exact = classified.Candidates.Single(x => x.InternalRecordId == "i-exact");
Assert(exact.ExternalRecordId == "e-exact", "Exact candidate must select the matching external record.");
Assert(exact.Type == ReconciliationMatchType.Exact, "Exact candidate must be classified Exact.");
Assert(exact.ConfidenceScore == 100, "Exact confidence must be 100.");

var partialReference = classified.Candidates.Single(x => x.InternalRecordId == "i-partial-ref");
Assert(partialReference.ExternalRecordId == "e-partial-ref", "Reference-based probable candidate must be selected.");
Assert(partialReference.Type == ReconciliationMatchType.Partial, "Probable candidate must use existing Partial type.");
Assert(partialReference.ConfidenceScore == 60, "Reference + time probable score must be deterministic.");
Assert(partialReference.AmountDifferenceMinor == -150, "Amount delta must be preserved for review.");

var partialAmount = classified.Candidates.Single(x => x.InternalRecordId == "i-partial-amount");
Assert(partialAmount.ExternalRecordId == "e-partial-amount", "Amount/time probable candidate must be selected.");
Assert(partialAmount.Type == ReconciliationMatchType.Partial, "Amount/time candidate must be Partial.");
Assert(partialAmount.ConfidenceScore == 60, "Amount + time probable score must be deterministic.");

var unmatchedInternal = classified.Candidates.Single(x => x.InternalRecordId == "i-unmatched");
Assert(unmatchedInternal.Type == ReconciliationMatchType.Unmatched, "Unmatched internal record must stay unmatched.");
Assert(unmatchedInternal.ExternalRecordId is null, "Unmatched internal record must not consume an external record.");

var unmatchedExternal = classified.Candidates.Single(x => x.ExternalRecordId == "e-leftover");
Assert(unmatchedExternal.Type == ReconciliationMatchType.Unmatched, "Unused external record must be surfaced as unmatched.");
Assert(unmatchedExternal.InternalRecordId is null, "Unused external record must not be fabricated into a pair.");

var tieBatch = new ReconciliationCandidateBatch(
    "partner-1",
    fromUtc,
    toUtc,
    [new InternalFinancialRecord("i-tie", "tie", "partner-1", "EUR", 5000, fromUtc.AddMinutes(10), "ledger")],
    [
        new ExternalFinancialRecord("e-b", "tie", "partner-1", "EUR", 5000, fromUtc.AddMinutes(12), "partner"),
        new ExternalFinancialRecord("e-a", "tie", "partner-1", "EUR", 5000, fromUtc.AddMinutes(12), "partner")
    ]);

var tieFirst = service.Classify(tieBatch);
var tieSecond = service.Classify(tieBatch);
var selectedFirst = tieFirst.Candidates.Single(x => x.InternalRecordId == "i-tie");
var selectedSecond = tieSecond.Candidates.Single(x => x.InternalRecordId == "i-tie");
Assert(selectedFirst.ExternalRecordId == "e-a", "Stable record id must break otherwise equal ties.");
Assert(selectedFirst == selectedSecond, "Classification must be reproducible for identical input.");

var oneToOneBatch = new ReconciliationCandidateBatch(
    "partner-1",
    fromUtc,
    toUtc,
    [
        new InternalFinancialRecord("i-1", "shared", "partner-1", "EUR", 100, fromUtc.AddMinutes(1), "ledger"),
        new InternalFinancialRecord("i-2", "shared", "partner-1", "EUR", 100, fromUtc.AddMinutes(2), "ledger")
    ],
    [new ExternalFinancialRecord("e-only", "shared", "partner-1", "EUR", 100, fromUtc.AddMinutes(1), "partner")]);

var oneToOne = service.Classify(oneToOneBatch);
Assert(oneToOne.Candidates.Count(x => x.ExternalRecordId == "e-only") == 1, "An external record must be consumed at most once.");
Assert(oneToOne.Candidates.Single(x => x.InternalRecordId == "i-2").Type == ReconciliationMatchType.Unmatched,
    "Second internal record must remain unmatched after one-to-one consumption.");

var wrongCurrencyBatch = new ReconciliationCandidateBatch(
    "partner-1",
    fromUtc,
    toUtc,
    [new InternalFinancialRecord("i-currency", "ref", "partner-1", "EUR", 100, fromUtc.AddMinutes(1), "ledger")],
    [new ExternalFinancialRecord("e-currency", "ref", "partner-1", "USD", 100, fromUtc.AddMinutes(1), "partner")]);
var wrongCurrency = service.Classify(wrongCurrencyBatch);
Assert(wrongCurrency.Candidates.Single(x => x.InternalRecordId == "i-currency").Type == ReconciliationMatchType.Unmatched,
    "Cross-currency records must never be classified as a candidate pair.");

Console.WriteLine("AFW-BE-REQUEST-RECONCILIATION-3 automatic candidate matching scenarios: PASS");
