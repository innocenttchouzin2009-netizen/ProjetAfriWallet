using Microsoft.AspNetCore.Http.HttpResults;
using Subscriptions.Application.Services;
using Subscriptions.Contracts.Dtos;
using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var providerRepository = new InMemorySubscriptionProviderRepository(SeedProviders());
var planRepository = new InMemorySubscriptionPlanRepository(SeedPlans(providerRepository));
var offerRepository = new InMemorySubscriptionCatalogOfferRepository(SeedOffers());
var catalogService = new SubscriptionProviderCatalogService(providerRepository, planRepository);

builder.Services.AddSingleton<ISubscriptionProviderRepository>(providerRepository);
builder.Services.AddSingleton<ISubscriptionPlanRepository>(planRepository);
builder.Services.AddSingleton(offerRepository);
builder.Services.AddSingleton(catalogService);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/v1/subscriptions/providers", (string? country, string? currency, string? q, int page = 1, int pageSize = 20, SubscriptionProviderCatalogService service = null!) =>
{
    var items = service.ListProviders(country, currency, q, page, pageSize)
        .Select(MapProvider)
        .ToList();

    return Results.Ok(new { items, page, pageSize, total = items.Count });
});

app.MapGet("/api/v1/subscriptions/providers/search", (string q, SubscriptionProviderCatalogService service) =>
{
    var items = service.ListProviders(q: q, page: 1, pageSize: 20)
        .Select(MapProvider)
        .ToList();

    return Results.Ok(new { items, q });
});

app.MapGet("/api/v1/subscriptions/providers/{providerId}", (string providerId, SubscriptionProviderCatalogService service) =>
{
    var provider = service.GetProvider(providerId);
    return provider is null
        ? Results.NotFound(new { code = "PROVIDER_NOT_FOUND", message = "Provider not found." })
        : Results.Ok(MapProvider(provider));
});

app.MapGet("/api/v1/subscriptions/providers/{providerId}/plans", (string providerId, SubscriptionProviderCatalogService service) =>
{
    var provider = service.GetProvider(providerId);
    if (provider is null)
    {
        return Results.NotFound(new { code = "PROVIDER_NOT_FOUND", message = "Provider not found." });
    }

    var plans = service.GetPlans(providerId).Select(MapPlan).ToList();
    return Results.Ok(new { providerId, plans });
});

app.MapGet("/api/v1/subscriptions/catalog", (string? country, string? currency, SubscriptionCategory? category, bool? featured, string? q, string? sort, int page = 1, int pageSize = 20, InMemorySubscriptionCatalogOfferRepository repository = null!) =>
{
    var items = repository.List(country, currency, category, featured, q, sort, page, pageSize)
        .Select(MapOffer)
        .ToList();

    return Results.Ok(new { items, page, pageSize, total = items.Count });
});

app.MapGet("/api/v1/subscriptions/catalog/{offerId}", (string offerId, InMemorySubscriptionCatalogOfferRepository repository) =>
{
    var offer = repository.GetById(offerId);
    return offer is null
        ? Results.NotFound(new { code = "OFFER_NOT_FOUND", message = "Offer not found." })
        : Results.Ok(MapOffer(offer));
});

app.Run();

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
            LogoUrl = "https://example.com/netflix.png",
            Website = "https://www.netflix.com",
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
            Website = "https://www.canalplus.com",
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

static IEnumerable<SubscriptionPlan> SeedPlans(InMemorySubscriptionProviderRepository providerRepository)
{
    foreach (var provider in providerRepository.ListAll())
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

static IEnumerable<SubscriptionCatalogOffer> SeedOffers()
{
    return new List<SubscriptionCatalogOffer>
    {
        new()
        {
            OfferId = "netflix-basic",
            ProviderId = "netflix",
            Name = "Netflix Basic",
            Description = "HD streaming with ads",
            Category = SubscriptionCategory.VideoStreaming,
            Country = "DE",
            Currency = "EUR",
            PriceMinor = 1299,
            BillingCycle = "monthly",
            IsFeatured = true,
            IsAvailable = true,
            ValidFrom = DateTimeOffset.UtcNow.AddDays(-7),
            ValidTo = DateTimeOffset.UtcNow.AddDays(30),
            PromotionCode = "WELCOME10",
            DiscountPercent = 10,
            IsNew = true
        },
        new()
        {
            OfferId = "canalplus-access",
            ProviderId = "canalplus",
            Name = "Canal+ Access",
            Description = "Entertainment bundle",
            Category = SubscriptionCategory.Tv,
            Country = "FR",
            Currency = "EUR",
            PriceMinor = 2499,
            BillingCycle = "monthly",
            IsFeatured = false,
            IsAvailable = true,
            PromotionCode = "LAUNCH",
            DiscountPercent = 5,
            IsNew = false
        },
        new()
        {
            OfferId = "mybouquetafricain-essentiel",
            ProviderId = "mybouquetafricain",
            Name = "MyBouquetAfricain Essentiel",
            Description = "Regional bouquet",
            Category = SubscriptionCategory.Tv,
            Country = "CM",
            Currency = "XOF",
            PriceMinor = 150000,
            BillingCycle = "monthly",
            IsFeatured = true,
            IsAvailable = true,
            IsNew = false
        }
    };
}

static SubscriptionProviderDto MapProvider(SubscriptionProvider provider) => new(
    provider.ProviderId,
    provider.Code,
    provider.DisplayName,
    provider.Description,
    provider.Category.ToString(),
    provider.LogoUrl,
    provider.Website,
    provider.SupportedCountries,
    provider.SupportedCurrencies,
    provider.IntegrationType.ToString(),
    provider.Status.ToString(),
    provider.CreatedAt,
    provider.UpdatedAt,
    provider.Version);

static SubscriptionPlanDto MapPlan(SubscriptionPlan plan) => new(
    plan.PlanId,
    plan.ProviderId,
    plan.Name,
    plan.Description,
    plan.BillingCycle,
    plan.Currency,
    plan.AmountMinor,
    plan.Country,
    plan.Status.ToString());

static SubscriptionCatalogOfferDto MapOffer(SubscriptionCatalogOffer offer) => new(
    offer.OfferId,
    offer.ProviderId,
    offer.Name,
    offer.Description,
    offer.Category.ToString(),
    offer.Country,
    offer.Currency,
    offer.PriceMinor,
    offer.BillingCycle,
    offer.IsFeatured,
    offer.IsAvailable,
    offer.ValidFrom,
    offer.ValidTo,
    offer.PromotionCode,
    offer.DiscountPercent,
    offer.IsNew,
    offer.CreatedAt,
    offer.UpdatedAt);
