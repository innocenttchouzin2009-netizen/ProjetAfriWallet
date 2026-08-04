using Subscriptions.Domain.Models;
using Subscriptions.Infrastructure.Repositories;

var repository = new InMemorySubscriptionCatalogOfferRepository(new[]
{
    new SubscriptionCatalogOffer
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
        PromotionCode = "WELCOME10",
        DiscountPercent = 10,
        IsNew = true
    },
    new SubscriptionCatalogOffer
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
    }
});

var results = repository.List(country: "FR", currency: "EUR", category: null, featured: null, q: null, sort: "price-asc", page: 1, pageSize: 20).ToList();
if (results.Count != 1 || results[0].OfferId != "canalplus-access")
{
    Console.Error.WriteLine("Catalog offer filtering scenario failed.");
    Environment.Exit(1);
}

var featuredResults = repository.List(country: null, currency: null, category: null, featured: true, q: null, sort: null, page: 1, pageSize: 20).ToList();
if (featuredResults.Count != 1 || featuredResults[0].OfferId != "netflix-basic")
{
    Console.Error.WriteLine("Catalog feature filtering scenario failed.");
    Environment.Exit(1);
}

Console.WriteLine("All AFW-DLV-0006.2 subscription catalog scenarios passed.");
