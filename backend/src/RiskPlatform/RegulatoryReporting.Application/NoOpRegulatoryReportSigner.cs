namespace RegulatoryReporting.Application;

public sealed class NoOpRegulatoryReportSigner : IRegulatoryReportSigner
{
    public string? Sign(string reportReference, int reportVersion, DateTimeOffset generatedAtUtc, string checksum)
    {
        return null;
    }
}
