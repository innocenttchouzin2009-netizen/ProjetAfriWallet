using UniversalWallet.Api.Application.FxQuotes;
using UniversalWallet.Api.Domain.FxQuotes;

namespace UniversalWallet.Api.Api.FxQuotes;

public static class QuoteEndpoints
{
	public static WebApplication MapQuoteEndpoints(this WebApplication app)
	{
		app.MapPost("/api/v1/fx/quotes", (CreateQuoteRequest request, CreateQuoteHandler handler) =>
		{
			try
			{
				return Results.Ok(handler.Handle(request));
			}
			catch (InvalidOperationException ex) when (ex.Message is "CURRENCY_NOT_FOUND" or "CURRENCY_DISABLED")
			{
				return Results.BadRequest(new { code = ex.Message, message = ex.Message == "CURRENCY_DISABLED" ? "Currency is disabled." : "Currency not found." });
			}
			catch (InvalidOperationException ex) when (ex.Message is "FX_PROVIDER_UNAVAILABLE" or "FX_RATE_NOT_FOUND")
			{
				return Results.Problem(ex.Message);
			}
		});

		app.MapGet("/api/v1/fx/quotes/{quoteId:guid}", (Guid quoteId, GetQuoteHandler handler) =>
		{
			try
			{
				return Results.Ok(handler.Handle(quoteId));
			}
			catch (InvalidOperationException ex) when (ex.Message == "QUOTE_NOT_FOUND")
			{
				return Results.NotFound(new { code = ex.Message, message = "Quote not found." });
			}
		});

		app.MapPost("/api/v1/fx/quotes/{quoteId:guid}/accept", (Guid quoteId, AcceptQuoteHandler handler) =>
		{
			try
			{
				return Results.Ok(handler.Handle(quoteId));
			}
			catch (InvalidOperationException ex) when (ex.Message == "QUOTE_EXPIRED")
			{
				return Results.Conflict(new { code = ex.Message, message = "Quote expired." });
			}
			catch (InvalidOperationException ex) when (ex.Message == "QUOTE_NOT_FOUND")
			{
				return Results.NotFound(new { code = ex.Message, message = "Quote not found." });
			}
		});

		return app;
	}
}
