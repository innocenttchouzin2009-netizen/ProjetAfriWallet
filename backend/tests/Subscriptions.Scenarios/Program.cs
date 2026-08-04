using Subscriptions.Application.Services;
using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var failures = new List<string>();
var providerRepository = new InMemorySubscriptionProviderRepository(SeedProviders());
var planRepository = new InMemorySubscriptionPlanRepository(SeedPlans(providerRepository));
var service = new SubscriptionProviderCatalogService(providerRepository, planRepository);

Run("creates provider", () =>
{
    var provider = providerRepository.Create(new SubscriptionProvider
    {
        Code = "SPOTIFY",
        DisplayName = "Spotify",
        Description = "Music streaming",
        Category = SubscriptionCategory.Music,
        SupportedCountries = new List<string> { "CM", "FR" },
        SupportedCurrencies = new List<string> { "XOF", "EUR" },
        IntegrationType = SubscriptionIntegrationType.DirectApi,
        Status = SubscriptionProviderStatus.Active,
        Version = 1
    });

    Assert(!string.IsNullOrWhiteSpace(provider.ProviderId), "provider should get an id");
});

Run("searches providers", () =>
{
    var results = service.ListProviders(q: "net");
    Assert(results.Any(p => p.DisplayName.Contains("Netflix", StringComparison.OrdinalIgnoreCase)), "search should find Netflix");
});

Run("lists providers", () =>
{
    var results = service.ListProviders(page: 1, pageSize: 10);
    Assert(results.Count >= 4, "listing should return providers");
});

Run("lists plans", () =>
{
    var plans = service.GetPlans("netflix");
    Assert(plans.Any(), "Netflix should have plans");
});

Run("filters by country", () =>
{
    var results = service.ListProviders(country: "FR");
    Assert(results.Any(p => p.ProviderId == "netflix"), "French country filter should include Netflix");
});

Run("filters by currency", () =>
{
    var results = service.ListProviders(currency: "EUR");
    Assert(results.Any(p => p.ProviderId == "netflix"), "EUR filter should include Netflix");
});

Run("suspended provider is returned", () =>
{
    var provider = providerRepository.Create(new SubscriptionProvider
    {
        ProviderId = "suspended-provider",
        Code = "SUSPENDED",
        DisplayName = "Suspended Demo",
        Description = "Suspended provider",
        Category = SubscriptionCategory.Other,
        SupportedCountries = new List<string> { "CM" },
        SupportedCurrencies = new List<string> { "XOF" },
        IntegrationType = SubscriptionIntegrationType.Manual,
        Status = SubscriptionProviderStatus.Suspended,
        Version = 1
    });

    Assert(provider.Status == SubscriptionProviderStatus.Suspended, "suspended provider should be stored");
});

Run("missing provider is not found", () =>
{
    var provider = service.GetProvider("does-not-exist");
    Assert(provider is null, "missing provider should return null");
});

Run("pagination works", () =>
{
    var page1 = service.ListProviders(page: 1, pageSize: 2);
    var page2 = service.ListProviders(page: 2, pageSize: 2);
    Assert(page1.Count == 2 && page2.Count >= 1, "pagination should split results");
});

Run("idempotence", () =>
{
    var first = providerRepository.Create(new SubscriptionProvider
    {
        ProviderId = "idempotent",
        Code = "IDEMPOTENT",
        DisplayName = "Idempotent Demo",
        Description = "Idempotent provider",
        Category = SubscriptionCategory.Other,
        SupportedCountries = new List<string> { "CM" },
        SupportedCurrencies = new List<string> { "XOF" },
        IntegrationType = SubscriptionIntegrationType.Manual,
        Status = SubscriptionProviderStatus.ComingSoon,
        Version = 1
    });

    var duplicate = providerRepository.Create(new SubscriptionProvider
    {
        ProviderId = "idempotent",
        Code = "IDEMPOTENT",
        DisplayName = "Idempotent Demo",
        Description = "Idempotent provider",
        Category = SubscriptionCategory.Other,
        SupportedCountries = new List<string> { "CM" },
        SupportedCurrencies = new List<string> { "XOF" },
        IntegrationType = SubscriptionIntegrationType.Manual,
        Status = SubscriptionProviderStatus.ComingSoon,
        Version = 1
    });

    Assert(first.ProviderId == duplicate.ProviderId, "idempotent create should reuse provider id");
});

if (failures.Count == 0)
{
    Console.WriteLine("All AFW-DLV-0006.1 subscription provider scenarios passed.");
    return;
}

Console.WriteLine("Subscription provider scenarios failed:");
foreach (var failure in failures)
{
    Console.WriteLine($" - {failure}");
}
Environment.ExitCode = 1;

void Run(string name, Action scenario)
{
    try
    {
        scenario();
        Console.WriteLine($"[OK] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[KO] {name} -> {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

static IEnumerable<SubscriptionProvider> SeedProviders()
{
    return new List<SubscriptionProvider>
    {
        new()
        {
            ProviderId = "netflix",
            Code = "NETFLIX",
            DisplayName = "Netflix",
            Description = "Video streaming provider",
            Category = SubscriptionCategory.VideoStreaming,
            SupportedCountries = new List<string>{"CM","FR","SN","CI"},
            SupportedCurrencies = new List<string>{"XOF","EUR"},
            IntegrationType = SubscriptionIntegrationType.DirectApi,
            Status = SubscriptionProviderStatus.ComingSoon,
            Version = 1
        },
        new()
        {
            ProviderId = "canalplus",
            Code = "CANALPLUS",
            DisplayName = "Canal+",
            Description = "Television provider",
            Category = SubscriptionCategory.Tv,
            SupportedCountries = new List<string>{"CM","FR","CI"},
            SupportedCurrencies = new List<string>{"XOF","EUR"},
            IntegrationType = SubscriptionIntegrationType.Partner,
            Status = SubscriptionProviderStatus.ComingSoon,
            Version = 1
        },
        new()
        {
            ProviderId = "mybouquetafricain",
            Code = "MYBOUQUETAFRICAIN",
            DisplayName = "MyBouquetAfricain",
            Description = "African bouquet provider",
            Category = SubscriptionCategory.Tv,
            SupportedCountries = new List<string>{"CM","SN","CI"},
            SupportedCurrencies = new List<string>{"XOF"},
            IntegrationType = SubscriptionIntegrationType.Voucher,
            Status = SubscriptionProviderStatus.ComingSoon,
            Version = 1
        },
        new()
        {
            ProviderId = "cinaf",
            Code = "CINAF",
            DisplayName = "Cinaf",
            Description = "Cinema and entertainment provider",
            Category = SubscriptionCategory.VideoStreaming,
            SupportedCountries = new List<string>{"CM","CI","SN"},
            SupportedCurrencies = new List<string>{"XOF"},
            IntegrationType = SubscriptionIntegrationType.Redirect,
            Status = SubscriptionProviderStatus.Active,
            Version = 1
        }
    };
}

static IEnumerable<SubscriptionPlan> SeedPlans(ISubscriptionProviderRepository providerRepository)
{
    var providers = providerRepository.ListAll();
    foreach (var provider in providers)
    {
        yield return new SubscriptionPlan
        {
            ProviderId = provider.ProviderId,
            Name = provider.ProviderId == "netflix" ? "Basic" : provider.ProviderId == "canalplus" ? "Access" : provider.ProviderId == "mybouquetafricain" ? "Essentiel" : "Starter",
            Description = "Standard monthly plan",
            BillingCycle = "monthly",
            Currency = "XOF",
            AmountMinor = 500000,
            Country = provider.SupportedCountries.FirstOrDefault() ?? "CM",
            Status = SubscriptionPlanStatus.Active
        };
    }
}
