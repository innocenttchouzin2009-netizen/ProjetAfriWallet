using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MobileMoney.Production.Configuration;
using MobileMoney.Production.Diagnostics;
using MobileMoney.Production.Extensions;
using MobileMoney.Production.Secrets;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["MtnMomo:Environment"] = "Development",
        ["MtnMomo:EnableProduction"] = "false",
        ["MtnMomo:BaseUrl"] = "https://sandbox.example.com",
        ["MtnMomo:TimeoutSeconds"] = "30"
    })
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddMtnMomoProductionConfiguration(configuration);

using var provider = services.BuildServiceProvider();
var options = provider.GetRequiredService<IOptions<MtnMomoProductionOptions>>().Value;
var guard = provider.GetRequiredService<ProductionEnvironmentGuard>();
var diagnostics = provider.GetRequiredService<ConfigurationDiagnosticService>();
var secretProvider = provider.GetRequiredService<ISecretProvider>();

if (options.Environment != "Development")
{
    throw new InvalidOperationException("Expected development environment configuration.");
}

if (guard.IsProductionAllowed())
{
    throw new InvalidOperationException("Production should remain disabled by default.");
}

if (secretProvider is not EnvironmentSecretProvider)
{
    throw new InvalidOperationException("Expected environment-based secret provider.");
}

var snapshot = diagnostics.GetDiagnosticSnapshot();
Console.WriteLine("All AFW-DLV-0007.3.4.1 configuration and secret-management scenarios passed.");
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(snapshot));
