using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subscriptions.Api.Configuration;

namespace Subscriptions.Api.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void AddEnterpriseConfiguration_BindsMtnOptionsAndValidatesOnStart()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MtnMomo:Environment"] = "Development",
                ["MtnMomo:BaseUrl"] = "https://sandbox.example.com",
                ["MtnMomo:TimeoutSeconds"] = "30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddEnterpriseConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MtnMomoOptions>>().Value;

        options.Environment.Should().Be("Development");
        options.BaseUrl.Should().Be("https://sandbox.example.com");
        options.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void AddEnterpriseConfiguration_RejectsInvalidBaseUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MtnMomo:Environment"] = "Production",
                ["MtnMomo:BaseUrl"] = "not-a-url",
                ["MtnMomo:TimeoutSeconds"] = "30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddEnterpriseConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MtnMomoOptions>>();

        var ex = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        ex.Failures.Should().ContainSingle(f => f.Contains("BaseUrl"));
    }
}
