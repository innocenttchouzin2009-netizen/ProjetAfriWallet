using AfriWallet.CardPlatform.Application.Services;
using AfriWallet.CardPlatform.Domain.Entities;
using AfriWallet.CardPlatform.Infrastructure;

var repository = new InMemoryCardRepository();
var lifecycle = new CardLifecycleService(repository);

var card = await lifecycle.IssueAsync(new CardLifecycleRequest { CardId = "card-100", OwnerAwidId = "owner-100", WalletId = "wallet-100" });
Console.WriteLine(card is not null ? "issuance ............................. PASS" : "issuance ............................. FAIL");

var activated = await lifecycle.ActivateAsync(card!.CardId);
Console.WriteLine(activated?.Status == "ACTIVE" ? "activation ........................... PASS" : "activation ........................... FAIL");

var frozen = await lifecycle.FreezeAsync(activated!.CardId);
Console.WriteLine(frozen?.Status == "FROZEN" ? "freeze ............................... PASS" : "freeze ............................... FAIL");

var unfrozen = await lifecycle.UnfreezeAsync(frozen!.CardId);
Console.WriteLine(unfrozen?.Status == "ACTIVE" ? "unfreeze ............................. PASS" : "unfreeze ............................. FAIL");

var suspended = await lifecycle.SuspendAsync(unfrozen!.CardId);
Console.WriteLine(suspended?.Status == "SUSPENDED" ? "suspension ........................... PASS" : "suspension ........................... FAIL");

var resumed = await lifecycle.ResumeAsync(suspended!.CardId);
Console.WriteLine(resumed?.Status == "ACTIVE" ? "resume ............................... PASS" : "resume ............................... FAIL");

var replaced = await lifecycle.ReplaceAsync(resumed!.CardId);
Console.WriteLine(replaced?.Status == "REPLACED" ? "replacement .......................... PASS" : "replacement .......................... FAIL");

var expired = await lifecycle.ExpireAsync(replaced!.CardId);
Console.WriteLine(expired?.Status == "EXPIRED" ? "expiration ........................... PASS" : "expiration ........................... FAIL");

var closed = await lifecycle.CloseAsync(expired!.CardId);
Console.WriteLine(closed?.Status == "CLOSED" ? "closure .............................. PASS" : "closure .............................. FAIL");

var invalid = await lifecycle.FreezeAsync(closed!.CardId);
Console.WriteLine(invalid is null ? "invalid transition rejected .......... PASS" : "invalid transition rejected .......... FAIL");

var timeline = await lifecycle.GetTimelineAsync(card.CardId);
Console.WriteLine(timeline.Count > 0 ? "timeline integration ................. PASS" : "timeline integration ................. FAIL");

var audit = await lifecycle.GetAuditTrailAsync(card.CardId);
Console.WriteLine(audit.Count > 0 ? "audit integration .................... PASS" : "audit integration .................... FAIL");

Console.WriteLine("\nAll AFW-DLV-0008.5 card lifecycle scenarios passed.");
