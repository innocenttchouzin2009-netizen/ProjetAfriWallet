using Microsoft.AspNetCore.Http.HttpResults;
using Subscriptions.Api;
using Subscriptions.Api.Configuration;
using Subscriptions.Application.Services;
using Subscriptions.Contracts.Dtos;
using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEnterpriseConfiguration(builder.Configuration);

var secretProvider = new EnvironmentSecretProvider();
var mtnOptions = builder.Configuration.GetSection(MtnMomoOptions.SectionName).Get<MtnMomoOptions>() ?? new MtnMomoOptions();
if (builder.Environment.IsProduction())
{
    _ = secretProvider.GetRequiredSecret("MTN_API_KEY");
}

var storageRoot = Path.Combine(AppContext.BaseDirectory, "state");
var providerRepository = new InMemorySubscriptionProviderRepository(SeedProviders());
var planRepository = new InMemorySubscriptionPlanRepository(SeedPlans(providerRepository));
var offerRepository = new InMemorySubscriptionCatalogOfferRepository(SeedOffers());
var catalogService = new SubscriptionProviderCatalogService(providerRepository, planRepository);
var lifecycleRepository = new JsonAtomicSubscriptionRepository(storageRoot);
var lifecycleService = new UserSubscriptionLifecycleService(lifecycleRepository);
var invoiceRepository = new JsonAtomicSubscriptionInvoiceRepository(storageRoot);
var billingService = new SubscriptionBillingService(invoiceRepository, new FakePaymentIntentGateway());
var jobRepository = new JsonAtomicAutoRenewJobRepository(storageRoot);
var notificationGateway = new FakeNotificationGateway();
var autoRenewService = new AutoRenewService(jobRepository, billingService, lifecycleService, notificationGateway);
var connectorRegistry = new SubscriptionProviderConnectorRegistry();
connectorRegistry.Register(new SandboxNetflixConnector());
connectorRegistry.Register(new SandboxCanalPlusConnector());
connectorRegistry.Register(new SandboxMyBouquetAfricainConnector());
connectorRegistry.Register(new SandboxCinafConnector());

builder.Services.AddSingleton<ISubscriptionProviderRepository>(providerRepository);
builder.Services.AddSingleton<ISubscriptionPlanRepository>(planRepository);
builder.Services.AddSingleton(offerRepository);
builder.Services.AddSingleton(catalogService);
builder.Services.AddSingleton<IUserSubscriptionRepository>(lifecycleRepository);
builder.Services.AddSingleton(lifecycleService);
builder.Services.AddSingleton<ISubscriptionInvoiceRepository>(invoiceRepository);
builder.Services.AddSingleton(billingService);
builder.Services.AddSingleton<IAutoRenewJobRepository>(jobRepository);
builder.Services.AddSingleton(autoRenewService);
builder.Services.AddSingleton(connectorRegistry);

var app = builder.Build();

app.UseProductionReadiness();
app.UseMiddleware<TechnicalKeyMiddleware>();

app.MapHealthEndpoints();
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

app.MapPost("/api/v1/subscriptions/lifecycle", (CreateUserSubscriptionRequest request, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.Create(request);
    return Results.Created($"/api/v1/subscriptions/lifecycle/{subscription.SubscriptionId}", MapLifecycle(subscription));
});

app.MapGet("/api/v1/subscriptions/lifecycle/{subscriptionId}", (string subscriptionId, IUserSubscriptionRepository repository) =>
{
    var subscription = repository.GetById(subscriptionId);
    return subscription is null
        ? Results.NotFound(new { code = "SUBSCRIPTION_NOT_FOUND", message = "Subscription not found." })
        : Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/lifecycle/{subscriptionId}/pending-payment", (string subscriptionId, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.MarkPendingPayment(subscriptionId);
    return Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/lifecycle/{subscriptionId}/activate", (string subscriptionId, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.Activate(subscriptionId);
    return Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/lifecycle/{subscriptionId}/suspend", (string subscriptionId, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.Suspend(subscriptionId);
    return Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/lifecycle/{subscriptionId}/resume", (string subscriptionId, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.Resume(subscriptionId);
    return Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/lifecycle/{subscriptionId}/renew", (string subscriptionId, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.Renew(subscriptionId);
    return Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/lifecycle/{subscriptionId}/cancel", (string subscriptionId, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.Cancel(subscriptionId);
    return Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/lifecycle/{subscriptionId}/expire", (string subscriptionId, UserSubscriptionLifecycleService service) =>
{
    var subscription = service.Expire(subscriptionId);
    return Results.Ok(MapLifecycle(subscription));
});

app.MapPost("/api/v1/subscriptions/invoices", (CreateSubscriptionInvoiceRequest request, SubscriptionBillingService service) =>
{
    var invoice = service.CreateInvoice(request);
    return Results.Created($"/api/v1/subscriptions/invoices/{invoice.InvoiceId}", MapInvoice(invoice));
});

app.MapGet("/api/v1/subscriptions/invoices/{invoiceId}", (string invoiceId, ISubscriptionInvoiceRepository repository) =>
{
    var invoice = repository.GetById(invoiceId);
    return invoice is null
        ? Results.NotFound(new { code = "INVOICE_NOT_FOUND", message = "Invoice not found." })
        : Results.Ok(MapInvoice(invoice));
});

app.MapPost("/api/v1/subscriptions/invoices/{invoiceId}/pay", (string invoiceId, SubscriptionBillingService service) =>
{
    var attempt = service.ProcessPayment(invoiceId);
    return Results.Ok(new { invoiceId, attempt.Status, attempt.GatewayReference });
});

app.MapPost("/internal/subscriptions/auto-renew/jobs", (ScheduleAutoRenewRequest request, AutoRenewService service) =>
{
    var job = service.ScheduleRenewal(request);
    return Results.Created($"/internal/subscriptions/auto-renew/jobs/{job.JobId}", MapAutoRenew(job));
});

app.MapPost("/internal/subscriptions/auto-renew/process", (DateTimeOffset asOf, AutoRenewService service) =>
{
    var jobs = service.ProcessDueRenewals(asOf);
    return Results.Ok(jobs.Select(MapAutoRenew));
});

app.MapGet("/internal/subscriptions/connectors/{providerId}/capabilities", (string providerId, SubscriptionProviderConnectorRegistry registry) =>
{
    var capabilities = registry.DiscoverCapabilities(providerId);
    return capabilities is null
        ? Results.NotFound(new { code = "CONNECTOR_NOT_FOUND", message = "Connector not found." })
        : Results.Ok(MapCapabilities(capabilities));
});

app.MapPost("/internal/subscriptions/connectors/{providerId}/activate", (string providerId, string subscriptionId, SubscriptionProviderConnectorRegistry registry) =>
{
    var response = registry.Activate(providerId, subscriptionId, new Dictionary<string, string> { ["requestId"] = Guid.NewGuid().ToString("N") });
    return Results.Ok(MapResponse(response));
});

app.MapPost("/internal/subscriptions/connectors/{providerId}/renew", (string providerId, string subscriptionId, SubscriptionProviderConnectorRegistry registry) =>
{
    var response = registry.Renew(providerId, subscriptionId);
    return Results.Ok(MapResponse(response));
});

app.MapPost("/internal/subscriptions/connectors/{providerId}/suspend", (string providerId, string subscriptionId, SubscriptionProviderConnectorRegistry registry) =>
{
    var response = registry.Suspend(providerId, subscriptionId);
    return Results.Ok(MapResponse(response));
});

app.MapPost("/internal/subscriptions/connectors/{providerId}/resume", (string providerId, string subscriptionId, SubscriptionProviderConnectorRegistry registry) =>
{
    var response = registry.Resume(providerId, subscriptionId);
    return Results.Ok(MapResponse(response));
});

app.MapPost("/internal/subscriptions/connectors/{providerId}/cancel", (string providerId, string subscriptionId, SubscriptionProviderConnectorRegistry registry) =>
{
    var response = registry.Cancel(providerId, subscriptionId);
    return Results.Ok(MapResponse(response));
});

app.MapGet("/internal/subscriptions/connectors/{providerId}/status", (string providerId, string subscriptionId, SubscriptionProviderConnectorRegistry registry) =>
{
    var response = registry.GetStatus(providerId, subscriptionId);
    return Results.Ok(MapResponse(response));
});

app.MapGet("/internal/subscriptions/connectors/{providerId}/health", (string providerId, SubscriptionProviderConnectorRegistry registry) =>
{
    var health = registry.HealthCheck(providerId);
    return Results.Ok(MapHealth(health));
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

static UserSubscriptionDto MapLifecycle(UserSubscription subscription) => new(
    subscription.SubscriptionId,
    subscription.UserId,
    subscription.ProviderId,
    subscription.PlanId,
    subscription.OfferId,
    subscription.Currency,
    subscription.AmountMinor,
    subscription.BillingCycle,
    subscription.GracePeriodDays,
    subscription.Status.ToString(),
    subscription.CreatedAt,
    subscription.UpdatedAt,
    subscription.StartedAt,
    subscription.EndedAt,
    subscription.RenewalAt,
    subscription.LastPaymentAt,
    subscription.History.Select(h => $"{h.Status}:{h.Reason ?? ""}").ToList());

static SubscriptionInvoiceDto MapInvoice(SubscriptionInvoice invoice) => new(
    invoice.InvoiceId,
    invoice.SubscriptionId,
    invoice.BillingPeriodStart,
    invoice.BillingPeriodEnd,
    invoice.Currency,
    invoice.AmountMinor,
    invoice.BillingCycle.ToString(),
    invoice.DueAt,
    invoice.Status.ToString(),
    invoice.CreatedAt,
    invoice.UpdatedAt,
    invoice.PaidAt,
    invoice.RetryCount,
    invoice.MaxRetries,
    invoice.Attempts.Select(a => $"{a.Status}:{a.GatewayReference ?? ""}").ToList());

static AutoRenewJobDto MapAutoRenew(AutoRenewJob job) => new(
    job.JobId,
    job.SubscriptionId,
    job.ScheduledFor,
    job.Status.ToString(),
    job.RetryCount,
    job.MaxRetries,
    job.LastError,
    job.CreatedAt,
    job.StartedAt,
    job.CompletedAt);

static SubscriptionConnectorCapabilityDto MapCapabilities(SubscriptionProviderConnectorCapabilities capabilities) => new(
    capabilities.SupportsActivation,
    capabilities.SupportsRenewal,
    capabilities.SupportsSuspension,
    capabilities.SupportsResumption,
    capabilities.SupportsCancellation,
    capabilities.SupportsStatusLookup);

static SubscriptionConnectorResponseDto MapResponse(SubscriptionProviderConnectorResponse response) => new(
    response.ProviderId,
    response.Operation,
    response.Status.ToString(),
    response.CorrelationId,
    response.Message,
    response.Payload);

static SubscriptionConnectorHealthDto MapHealth(SubscriptionProviderConnectorHealth health) => new(
    health.ProviderId,
    health.Status.ToString(),
    health.Message);
