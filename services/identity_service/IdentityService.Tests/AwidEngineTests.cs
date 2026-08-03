using System.Security.Claims;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Handlers;
using IdentityService.Application.Services;
using IdentityService.Contracts.Requests;
using IdentityService.Infrastructure.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdentityService.Tests;

[TestClass]
public class AwidEngineTests
{
    [TestMethod]
    public async Task CreateAwid_ShouldCreatePermanentPublicAwidAndAlias()
    {
        var repo = new InMemoryAwidRepository();
        var handler = new CreateAwidHandler(repo, new InMemoryAuthenticationEventRepository(), new AwidPolicy());
        var principal = CreatePrincipal("user-awid-1");

        var result = await handler.HandleAsync(new CreateAwidRequest
        {
            Alias = "innocent",
            PrivacyMode = "PRIVATE"
        }, principal, CancellationToken.None);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("ACTIVE", result.Status);
        Assert.IsTrue(result.PublicAwid.StartsWith("AW-237-"));
        Assert.AreEqual("@innocent", result.Alias);

        var stored = await repo.GetBySubjectIdAsync("user-awid-1", CancellationToken.None);
        Assert.IsNotNull(stored);
        Assert.AreEqual("innocent", stored!.AliasCanonical);
        Assert.IsNotNull(await repo.GetByPublicAwidAsync(result.PublicAwid, CancellationToken.None));
    }

    [TestMethod]
    public async Task ChangeAlias_ShouldRejectRapidChangesAndPreservePermanentAwid()
    {
        var repo = new InMemoryAwidRepository();
        var policy = new AwidPolicy();
        var createHandler = new CreateAwidHandler(repo, new InMemoryAuthenticationEventRepository(), policy);
        var changeHandler = new ChangeAliasHandler(repo, new InMemoryAuthenticationEventRepository(), policy);
        var principal = CreatePrincipal("user-awid-2");

        await createHandler.HandleAsync(new CreateAwidRequest { Alias = "innocent", PrivacyMode = "PRIVATE" }, principal, CancellationToken.None);

        var firstChange = await changeHandler.HandleAsync(new ChangeAliasRequest { Alias = "innocent237" }, principal, CancellationToken.None);
        var secondChange = await changeHandler.HandleAsync(new ChangeAliasRequest { Alias = "innocent84" }, principal, CancellationToken.None);

        Assert.IsTrue(firstChange.Success);
        Assert.IsFalse(secondChange.Success);
        Assert.AreEqual("ALIAS_CHANGE_TOO_SOON", secondChange.ErrorCode);

        var awid = await repo.GetBySubjectIdAsync("user-awid-2", CancellationToken.None);
        Assert.IsNotNull(awid);
        Assert.AreEqual("innocent237", awid!.AliasCanonical);

        var history = await repo.ListAliasHistoryAsync("user-awid-2", CancellationToken.None);
        Assert.AreEqual(1, history.Count);
        Assert.AreEqual("innocent", history[0].PreviousAlias);
        Assert.AreEqual("innocent237", history[0].NewAlias);
    }

    [TestMethod]
    public async Task CreateAwid_ShouldAllowOnlyOnePrimaryAwidPerUser()
    {
        var repo = new InMemoryAwidRepository();
        var handler = new CreateAwidHandler(repo, new InMemoryAuthenticationEventRepository(), new AwidPolicy());
        var principal = CreatePrincipal("user-awid-3");

        var first = await handler.HandleAsync(new CreateAwidRequest { Alias = "marie", PrivacyMode = "PRIVATE" }, principal, CancellationToken.None);
        var second = await handler.HandleAsync(new CreateAwidRequest { Alias = "marie237", PrivacyMode = "PRIVATE" }, principal, CancellationToken.None);

        Assert.IsTrue(first.Success);
        Assert.IsFalse(second.Success);
        Assert.AreEqual("AWID_ALREADY_EXISTS", second.ErrorCode);
    }

    [TestMethod]
    public async Task CreateAwid_ShouldRejectConcurrentCreationForSameAlias()
    {
        var repo = new InMemoryAwidRepository();
        var handler = new CreateAwidHandler(repo, new InMemoryAuthenticationEventRepository(), new AwidPolicy());

        var task1 = handler.HandleAsync(new CreateAwidRequest { Alias = "samealias", PrivacyMode = "PRIVATE" }, CreatePrincipal("user-awid-4a"), CancellationToken.None);
        var task2 = handler.HandleAsync(new CreateAwidRequest { Alias = "samealias", PrivacyMode = "PRIVATE" }, CreatePrincipal("user-awid-4b"), CancellationToken.None);
        var results = await Task.WhenAll(task1, task2);

        Assert.AreEqual(1, results.Count(x => x.Success));
        Assert.AreEqual(1, results.Count(x => !x.Success && x.ErrorCode == "ALIAS_ALREADY_TAKEN"));
    }

    [TestMethod]
    public async Task ChangeAlias_ShouldUseConfigurableCooldown()
    {
        var repo = new InMemoryAwidRepository();
        var policy = new AwidPolicy { AliasChangeCooldown = TimeSpan.Zero };
        var createHandler = new CreateAwidHandler(repo, new InMemoryAuthenticationEventRepository(), policy);
        var changeHandler = new ChangeAliasHandler(repo, new InMemoryAuthenticationEventRepository(), policy);
        var principal = CreatePrincipal("user-awid-5");

        await createHandler.HandleAsync(new CreateAwidRequest { Alias = "coolstart", PrivacyMode = "PRIVATE" }, principal, CancellationToken.None);
        var first = await changeHandler.HandleAsync(new ChangeAliasRequest { Alias = "coolnext" }, principal, CancellationToken.None);
        var second = await changeHandler.HandleAsync(new ChangeAliasRequest { Alias = "coolthird" }, principal, CancellationToken.None);

        Assert.IsTrue(first.Success);
        Assert.IsTrue(second.Success);
    }

    [TestMethod]
    public async Task AliasAvailability_ShouldBeCaseInsensitive()
    {
        var repo = new InMemoryAwidRepository();
        var createHandler = new CreateAwidHandler(repo, new InMemoryAuthenticationEventRepository(), new AwidPolicy());
        var availabilityHandler = new CheckAliasAvailabilityHandler(repo);

        await createHandler.HandleAsync(new CreateAwidRequest { Alias = "Innocent", PrivacyMode = "PRIVATE" }, CreatePrincipal("user-awid-6"), CancellationToken.None);
        var availability = await availabilityHandler.HandleAsync(new GetAliasAvailabilityRequest { Alias = "@INNOCENT" }, CancellationToken.None);

        Assert.IsTrue(availability.Success);
        Assert.IsFalse(availability.Available);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");
        return new ClaimsPrincipal(identity);
    }
}
