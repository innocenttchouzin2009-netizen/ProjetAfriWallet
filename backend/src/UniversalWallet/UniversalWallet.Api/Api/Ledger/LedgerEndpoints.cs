using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Infrastructure.Ledger;

namespace UniversalWallet.Api.Api.Ledger;

public static class LedgerEndpoints
{
	public static WebApplication MapLedgerEndpoints(this WebApplication app)
	{
		app.MapPost("/api/v1/ledger/post", (PostTransactionRequest request, PostTransactionHandler handler) =>
		{
			var result = handler.Handle(request);
			return result.Accepted
				? Results.Created($"/api/v1/ledger/transactions/{result.Transaction!.TransactionId}", result)
				: Results.BadRequest(result);
		});

		app.MapPost("/api/v1/ledger/reversal", (ReverseTransactionRequest request, ReverseTransactionHandler handler) =>
		{
			var result = handler.Handle(request);
			return result.Accepted
				? Results.Created($"/api/v1/ledger/transactions/{result.Transaction!.TransactionId}", result)
				: Results.BadRequest(result);
		});

		app.MapGet("/api/v1/ledger/{walletId:guid}", (Guid walletId, ILedgerRepository ledgerRepository, ILedgerJournalRepository journalRepository) =>
		{
			var journal = journalRepository.GetByWalletId(walletId);
			var entries = ledgerRepository.GetEntriesByWallet(walletId);
			return Results.Ok(new
			{
				journal,
				entries
			});
		});

		app.MapGet("/api/v1/ledger/transactions/{id:guid}", (Guid id, ILedgerRepository ledgerRepository) =>
		{
			var transaction = ledgerRepository.GetTransaction(id);
			if (transaction is null)
			{
				return Results.NotFound(new { code = "TRANSACTION_NOT_FOUND", message = "Transaction not found." });
			}

			return Results.Ok(new
			{
				transaction,
				entries = ledgerRepository.GetEntriesByTransaction(id)
			});
		});

		return app;
	}
}
