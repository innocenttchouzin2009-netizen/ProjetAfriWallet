using BankRouting.Application.Services;
using BankRouting.Infrastructure.Registries;
using BankRouting.Infrastructure.Repositories;
using BankRouting.Application.Contracts;
using BankRouting.Domain.Rails;

var railRegistry = new InMemoryBankRailRegistry();
var decisions = new InMemoryBankRoutingDecisionRepository();
var service = new BankRoutingService(railRegistry, decisions);

var sepaInstantScenario = await service.EvaluateAsync(new RoutingRequest(
    TransferIntentId: Guid.NewGuid(),
    OwnerAwid: "owner-001",
    CountryCode: "DE",
    CurrencyCode: "EUR",
    AmountMinor: 2500,
    IdempotencyKey: "route-de-eur-sepa-instant"), CancellationToken.None);

if (sepaInstantScenario.SelectedRail != BankRailType.SepaInstant)
    throw new InvalidOperationException("Expected SepaInstant for DE/EUR eligible case.");

var localScenario = await service.EvaluateAsync(new RoutingRequest(
    TransferIntentId: Guid.NewGuid(),
    OwnerAwid: "owner-002",
    CountryCode: "NG",
    CurrencyCode: "NGN",
    AmountMinor: 1500,
    IdempotencyKey: "route-ng-local"), CancellationToken.None);

if (localScenario.SelectedRail != BankRailType.LocalBankTransfer)
    throw new InvalidOperationException("Expected LocalBankTransfer for NG/NGN eligible case.");

var idempotentScenario = await service.EvaluateAsync(new RoutingRequest(
    TransferIntentId: Guid.NewGuid(),
    OwnerAwid: "owner-003",
    CountryCode: "FR",
    CurrencyCode: "EUR",
    AmountMinor: 4000,
    IdempotencyKey: "route-fr-eur-idempotent"), CancellationToken.None);

var duplicate = await service.EvaluateAsync(new RoutingRequest(
    TransferIntentId: Guid.NewGuid(),
    OwnerAwid: "owner-003",
    CountryCode: "FR",
    CurrencyCode: "EUR",
    AmountMinor: 4000,
    IdempotencyKey: "route-fr-eur-idempotent"), CancellationToken.None);

if (duplicate.DecisionId != idempotentScenario.DecisionId)
    throw new InvalidOperationException("Expected idempotent routing decision reuse.");

Console.WriteLine("All AFW-DLV-0015.3 bank routing scenarios passed.");
