using UniversalWallet.Api.Application.Balance;

namespace UniversalWallet.Api.Api.Balance;

public static class BalanceEndpoints
{
	public static WebApplication MapBalanceEndpoints(this WebApplication app)
	{
		app.MapGet("/api/v1/balances/{walletId:guid}", (Guid walletId, BalanceProjectionService service) =>
		{
			try
			{
				var state = service.GetProjectionState(walletId);
				var version = service.GetVersion(walletId);
				return Results.Ok(new
				{
					projection = state.Projection,
					currentLedgerPosition = state.CurrentLedgerPosition,
					isUpToDate = state.IsUpToDate,
					wasLagging = state.WasLagging,
					version
				});
			}
			catch (InvalidOperationException ex) when (ex.Message == "WALLET_NOT_FOUND")
			{
				return Results.NotFound(new { code = ex.Message, message = "Wallet not found." });
			}
		});

		app.MapPost("/api/v1/balances/{walletId:guid}/rebuild", (Guid walletId, BalanceProjectionService service) =>
		{
			try
			{
				var projection = service.RebuildFromLedger(walletId);
				var version = service.GetVersion(walletId);
				return Results.Ok(new
				{
					projection,
					currentLedgerPosition = projection.LastLedgerPosition,
					isUpToDate = true,
					version
				});
			}
			catch (InvalidOperationException ex) when (ex.Message == "WALLET_NOT_FOUND")
			{
				return Results.NotFound(new { code = ex.Message, message = "Wallet not found." });
			}
		});

		return app;
	}
}
