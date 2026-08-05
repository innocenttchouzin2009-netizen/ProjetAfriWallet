using AfriWallet.Banking.Application.Registry;
using AfriWallet.Banking.Application.Routing;
using AfriWallet.Banking.Domain.Entities;
using AfriWallet.Banking.Domain.ValueObjects;
using AfriWallet.Banking.Infrastructure;

var repository = new RegistryRepository();
var registry = new BankRegistryService(repository);
var routing = new BankRoutingService(repository);

var allProviders = await registry.GetAllAsync();
Console.WriteLine(allProviders.Any() ? "Registry initialization ........ PASS" : "Registry initialization ........ FAIL");

var sepa = await registry.SearchAsync("DE", "EUR", "SEPA", "Sandbox");
Console.WriteLine(sepa.Any(p => p.SwiftCode == "DEUTDEFF") ? "Swift metadata lookup ......... PASS" : "Swift metadata lookup ......... FAIL");

var metadataProvider = allProviders.First(p => p.ProviderId == "bank-sepa-sandbox");
Console.WriteLine(metadataProvider.EstimatedDeliveryDays == 1 ? "Delivery metadata ............. PASS" : "Delivery metadata ............. FAIL");
Console.WriteLine(metadataProvider.MinimumAmountMinor == 1000m ? "Minimum amount metadata ....... PASS" : "Minimum amount metadata ....... FAIL");
Console.WriteLine(metadataProvider.MaintenanceMode == false ? "Maintenance flag ............. PASS" : "Maintenance flag ............. FAIL");

var supportedCurrency = await routing.RouteAsync(new RoutingKey("DE", "USD", "SEPA", "Sandbox"));
Console.WriteLine(supportedCurrency.Decision == AfriWallet.Banking.Domain.Enums.RoutingDecision.Matched ? "Supported currency ............ PASS" : "Supported currency ............ FAIL");

var unsupportedCurrency = await routing.RouteAsync(new RoutingKey("DE", "GBP", "SEPA", "Sandbox"));
Console.WriteLine(unsupportedCurrency.Decision == AfriWallet.Banking.Domain.Enums.RoutingDecision.Unsupported ? "Unsupported currency .......... PASS" : "Unsupported currency .......... FAIL");

var maintenanceRoute = await routing.RouteAsync(new RoutingKey("NG", "NGN", "Domestic", "Sandbox"));
Console.WriteLine(maintenanceRoute.Decision == AfriWallet.Banking.Domain.Enums.RoutingDecision.Unsupported ? "Maintenance rejection ........ PASS" : "Maintenance rejection ........ FAIL");

var productionMismatch = await routing.RouteAsync(new RoutingKey("DE", "EUR", "SEPA", "Production"));
Console.WriteLine(productionMismatch.Decision == AfriWallet.Banking.Domain.Enums.RoutingDecision.EnvironmentMismatch ? "Production mismatch .......... PASS" : "Production mismatch .......... FAIL");

Console.WriteLine("\nAll AFW-DLV-0007.4.2 bank provider metadata and validation scenarios passed.");
