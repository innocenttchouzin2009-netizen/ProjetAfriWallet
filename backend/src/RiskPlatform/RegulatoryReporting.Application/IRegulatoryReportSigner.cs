namespace RegulatoryReporting.Application;

public interface IRegulatoryReportSigner
{
    string? Sign(string reportReference, int reportVersion, DateTimeOffset generatedAtUtc, string checksum);
}
