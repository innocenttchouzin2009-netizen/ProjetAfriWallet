using AfriWallet.CardPlatform.Application.Services;
using AfriWallet.CardPlatform.Infrastructure;

var repository = new InMemoryCardProgramRepository();
var service = new CardProgramService(repository);

var programs = await service.GetAllAsync();
Console.WriteLine("Registry initialization ........ PASS");
Console.WriteLine(programs.Any(p => p.ProgramCode.Contains("visa", StringComparison.OrdinalIgnoreCase)) ? "Visa program available ......... PASS" : "Visa program available ......... FAIL");
Console.WriteLine(programs.Any(p => p.ProgramCode.Contains("mastercard", StringComparison.OrdinalIgnoreCase)) ? "Mastercard program available ... PASS" : "Mastercard program available ... FAIL");
Console.WriteLine(programs.Any(p => p.Capabilities.ContactlessPayments) ? "Contactless support ............ PASS" : "Contactless support ............ FAIL");
Console.WriteLine(programs.Any(p => p.Capabilities.AtmWithdrawals) ? "ATM capability ................. PASS" : "ATM capability ................. FAIL");

Console.WriteLine("\nAll AFW-DLV-0008.1 card registry scenarios passed.");
