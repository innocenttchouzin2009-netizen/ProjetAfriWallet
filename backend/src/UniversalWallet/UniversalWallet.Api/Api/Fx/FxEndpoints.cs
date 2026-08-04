using UniversalWallet.Api.Application.Fx;

namespace UniversalWallet.Api.Api.Fx;

public static class FxEndpoints
{
	public static WebApplication MapFxEndpoints(this WebApplication app)
	{
		app.MapGet("/api/v1/fx/rates", (string from, string to, GetExchangeRateHandler handler) =>
		{
			try
			{
				return Results.Ok(handler.Handle(from, to));
			}
			catch (InvalidOperationException ex) when (ex.Message is "CURRENCY_NOT_FOUND" or "CURRENCY_DISABLED")
			{
				return Results.BadRequest(new { code = ex.Message, message = ex.Message == "CURRENCY_DISABLED" ? "Currency is disabled." : "Currency not found." });
			}
			catch (InvalidOperationException ex) when (ex.Message == "FX_PROVIDER_UNAVAILABLE" || ex.Message == "FX_RATE_NOT_FOUND" || ex.Message == "FX_RATE_PERIOD_INVALID")
			{
				return Results.Problem(ex.Message);
			}
		});

		app.MapGet("/api/v1/fx/history", (string from, string to, GetHistoryHandler handler) =>
		{
			try
			{
				return Results.Ok(handler.Handle(from, to));
			}
			catch (InvalidOperationException ex) when (ex.Message is "CURRENCY_NOT_FOUND" or "CURRENCY_DISABLED")
			{
				return Results.BadRequest(new { code = ex.Message, message = ex.Message == "CURRENCY_DISABLED" ? "Currency is disabled." : "Currency not found." });
			}
			catch (InvalidOperationException ex) when (ex.Message == "FX_PROVIDER_UNAVAILABLE" || ex.Message == "FX_RATE_NOT_FOUND" || ex.Message == "FX_RATE_PERIOD_INVALID")
			{
				return Results.Problem(ex.Message);
			}
		});

		app.MapPost("/api/v1/fx/rates/refresh", (string from, string to, RefreshExchangeRateHandler handler) =>
		{
			try
			{
				return Results.Ok(handler.Handle(from, to));
			}
			catch (InvalidOperationException ex) when (ex.Message is "CURRENCY_NOT_FOUND" or "CURRENCY_DISABLED")
			{
				return Results.BadRequest(new { code = ex.Message, message = ex.Message == "CURRENCY_DISABLED" ? "Currency is disabled." : "Currency not found." });
			}
			catch (InvalidOperationException ex) when (ex.Message == "FX_PROVIDER_UNAVAILABLE" || ex.Message == "FX_RATE_NOT_FOUND" || ex.Message == "FX_RATE_PERIOD_INVALID")
			{
				return Results.Problem(ex.Message);
			}
		});

		app.MapGet("/api/v1/fx/providers", (FxEngineService service) =>
		{
			var providers = new[]
			{
				new { name = service.GetCurrency("EUR").Code, description = "Static test provider", isAvailable = true }
			};
			return Results.Ok(providers);
		});

		app.MapPost("/api/v1/fx/convert", (ConvertCurrencyRequest request, ConvertCurrencyHandler handler) =>
		{
			try
			{
				return Results.Ok(handler.Handle(request));
			}
			catch (InvalidOperationException ex) when (ex.Message is "CURRENCY_NOT_FOUND" or "CURRENCY_DISABLED")
			{
				return Results.BadRequest(new { code = ex.Message, message = ex.Message == "CURRENCY_DISABLED" ? "Currency is disabled." : "Currency not found." });
			}
			catch (InvalidOperationException ex) when (ex.Message == "FX_PROVIDER_UNAVAILABLE" || ex.Message == "FX_RATE_NOT_FOUND" || ex.Message == "FX_RATE_PERIOD_INVALID")
			{
				return Results.Problem(ex.Message);
			}
		});

		return app;
	}
}
