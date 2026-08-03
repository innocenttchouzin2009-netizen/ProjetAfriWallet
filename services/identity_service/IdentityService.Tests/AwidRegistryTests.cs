using System.Threading;
using System.Threading.Tasks;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Handlers;
using IdentityService.Contracts.Requests;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdentityService.Tests;

[TestClass]
public class AwidRegistryTests
{
    [TestMethod]
    public async Task CheckAliasAvailability_ShouldReturnSuggestionsWhenTaken()
    {
        var repo = new InMemoryAwidRepository();
        var created = await repo.TryCreateAsync(new Awid
        {
            SubjectId = "user-availability",
            PublicAwid = "AW-237-ABC12345",
            AliasCanonical = "innocent",
            AliasDisplay = "@innocent"
        }, CancellationToken.None);
        Assert.IsTrue(created.Success, $"Unexpected create failure: {created.FailureReason}");

        var handler = new CheckAliasAvailabilityHandler(repo);
        var response = await handler.HandleAsync(new GetAliasAvailabilityRequest { Alias = "innocent" }, CancellationToken.None);

        Assert.IsTrue(response.Success);
        Assert.IsFalse(response.Available);
        Assert.AreEqual(3, response.Suggestions.Count);
    }

    [TestMethod]
    public async Task GetAwidProfile_ShouldReturnPrivacyAndStatus()
    {
        var repo = new InMemoryAwidRepository();
        var created = await repo.TryCreateAsync(new Awid
        {
            SubjectId = "user-profile",
            PublicAwid = "AW-237-XYZ98765",
            AliasCanonical = "marie",
            AliasDisplay = "@marie",
            PrivacyMode = AwidPrivacyMode.Standard,
            Status = AwidStatus.Active
        }, CancellationToken.None);
        Assert.IsTrue(created.Success, $"Unexpected create failure: {created.FailureReason}");

        var handler = new GetAwidProfileHandler(repo);
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-profile")
        }, "Test"));

        var response = await handler.HandleAsync(principal, CancellationToken.None);

        Assert.IsTrue(response.Success);
        Assert.AreEqual("AW-237-XYZ98765", response.PublicAwid);
        Assert.AreEqual("STANDARD", response.PrivacyMode);
        Assert.AreEqual("ACTIVE", response.Status);
    }
}
