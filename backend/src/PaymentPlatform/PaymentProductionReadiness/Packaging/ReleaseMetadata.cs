using System.Text.Json;

namespace AfriWallet.PaymentPlatform.ProductionReadiness.Packaging;

public sealed record ReleaseMetadata(
    string Delivery,
    string Release,
    DateTimeOffset GeneratedAtUtc)
{
    public static ReleaseMetadata Load(string releaseDirectory)
    {
        var path = Path.Combine(
            releaseDirectory,
            "configuration",
            "release-metadata.json");

        var metadata = JsonSerializer.Deserialize<ReleaseMetadata>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return metadata ?? throw new InvalidOperationException(
            "Release metadata is invalid.");
    }
}