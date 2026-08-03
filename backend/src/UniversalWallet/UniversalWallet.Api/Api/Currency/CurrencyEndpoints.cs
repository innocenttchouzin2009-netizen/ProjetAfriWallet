using UniversalWallet.Api.Application.Fx;
using UniversalWallet.Api.Infrastructure.Currency;

namespace UniversalWallet.Api.Api.Currency;

public static class CurrencyEndpoints
{
	public static WebApplication MapCurrencyEndpoints(this WebApplication app)
	{
		app.MapGet("/api/v1/currencies", (CurrencyRegistryService service) =>
		{
			return Results.Ok(service.List().Select(currency => new CurrencyResponse
			{
				Code = currency.Code,
				NumericCode = currency.NumericCode,
				Name = currency.Name,
				MinorUnits = currency.MinorUnits,
				Symbol = currency.Symbol,
				Region = currency.Region,
				Status = currency.Status,
				CreatedAt = currency.CreatedAt
			}));
		});

		app.MapGet("/api/v1/currencies/{code}", (string code, CurrencyRegistryService service) =>
		{
			try
			{
				var currency = service.GetRequired(code);
				return Results.Ok(new CurrencyResponse
				{
					Code = currency.Code,
					NumericCode = currency.NumericCode,
					Name = currency.Name,
					MinorUnits = currency.MinorUnits,
					Symbol = currency.Symbol,
					Region = currency.Region,
					Status = currency.Status,
					CreatedAt = currency.CreatedAt
				});
			}
			catch (InvalidOperationException ex) when (ex.Message == "CURRENCY_NOT_FOUND")
			{
				return Results.NotFound(new { code = ex.Message, message = "Currency not found." });
			}
		});

		app.MapGet("/api/v1/currencies/{code}/support", (string code, CurrencyRegistryService service) =>
		{
			try
			{
				service.RequireActive(code);
				return Results.Ok(new { code = code.Trim().ToUpperInvariant(), supported = true, walletCreationAllowed = true });
			}
			catch (InvalidOperationException)
			{
				return Results.Ok(new { code = code.Trim().ToUpperInvariant(), supported = false, walletCreationAllowed = false });
			}
		});

		return app;
	}
}
