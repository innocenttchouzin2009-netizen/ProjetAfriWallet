using RegulatoryReporting.Domain;

namespace RegulatoryReporting.Application;

public sealed class RegulatorySubmissionService
{
    public RegulatorySubmission Submit(RegulatoryReport report, string actor)
    {
        var submission = new RegulatorySubmission
        {
            AuthorityCode = report.AuthorityCode,
            SubmittedBy = actor,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            Status = "SUBMITTED"
        };

        report.Submissions.Add(submission);
        return submission;
    }

    public void Accept(RegulatoryReport report, string responseCode, string responseMessage)
    {
        var latest = report.Submissions.LastOrDefault() ?? throw new InvalidOperationException("No submission found.");
        latest.Status = "ACCEPTED";
        latest.ResponseCode = responseCode;
        latest.ResponseMessage = responseMessage;
        latest.RespondedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Reject(RegulatoryReport report, string responseCode, string responseMessage)
    {
        var latest = report.Submissions.LastOrDefault() ?? throw new InvalidOperationException("No submission found.");
        latest.Status = "REJECTED";
        latest.ResponseCode = responseCode;
        latest.ResponseMessage = responseMessage;
        latest.RespondedAtUtc = DateTimeOffset.UtcNow;
    }
}
