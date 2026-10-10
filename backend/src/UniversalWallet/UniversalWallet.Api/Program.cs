using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Api.Ledger;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.WalletEngine;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IWalletRepository, InMemoryWalletRepository>();
builder.Services.AddSingleton<ILedgerRepository, InMemoryLedgerRepository>();
builder.Services.AddSingleton<ILedgerJournalRepository, InMemoryLedgerJournalRepository>();
builder.Services.AddSingleton<LedgerValidator>();
builder.Services.AddSingleton<LedgerPostingService>();
builder.Services.AddSingleton<PostTransactionHandler>();
builder.Services.AddSingleton<ReverseTransactionHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
	service = "universal-wallet-api",
	status = "ok",
	utc = DateTimeOffset.UtcNow
}));

app.MapGet("/api/v1/currencies", (IWalletRepository repository) =>
{
	return Results.Ok(new
	{
		currencies = repository.SupportedCurrencies()
	});
});

app.MapPost("/api/v1/wallets", (CreateWalletRequest request, IWalletRepository repository) =>
{
	if (string.IsNullOrWhiteSpace(request.Awid))
	{
		return Results.BadRequest(new { code = "AWID_REQUIRED", message = "Awid is required." });
	}

	if (string.IsNullOrWhiteSpace(request.Currency))
	{
		return Results.BadRequest(new { code = "CURRENCY_REQUIRED", message = "Currency is required." });
	}

	try
	{
		var wallet = repository.Create(request.Awid, request.WalletType, request.Currency);
		return Results.Created($"/api/v1/wallets/{wallet.Id}", ToWalletResponse(request.Awid, wallet));
	}
	catch (InvalidOperationException ex) when (ex.Message == "WALLET_ALREADY_EXISTS")
	{
		return Results.Conflict(new { code = ex.Message, message = "A wallet already exists for this AWID, currency and type." });
	}
	catch (InvalidOperationException ex) when (ex.Message == "CURRENCY_NOT_SUPPORTED")
	{
		return Results.BadRequest(new { code = ex.Message, message = "Currency is not supported." });
	}
});

app.MapGet("/api/v1/wallets", (string awid, IWalletRepository repository) =>
{
	if (string.IsNullOrWhiteSpace(awid))
	{
		return Results.BadRequest(new { code = "AWID_REQUIRED", message = "awid query parameter is required." });
	}

	var wallets = repository.ListByAwid(awid).Select(wallet => ToWalletResponse(awid, wallet));
	return Results.Ok(new { items = wallets });
});

app.MapGet("/api/v1/wallets/{id:guid}", (Guid id, string awid, IWalletRepository repository) =>
{
	var wallet = repository.GetById(id);
	if (wallet is null)
	{
		return Results.NotFound(new { code = "WALLET_NOT_FOUND", message = "Wallet not found." });
	}

	return Results.Ok(ToWalletResponse(awid, wallet));
});

app.MapPatch("/api/v1/wallets/{id:guid}/status", (Guid id, UpdateWalletStatusRequest request, IWalletRepository repository) =>
{
	try
	{
		var wallet = repository.UpdateStatus(id, request.Status);
		if (wallet is null)
		{
			return Results.NotFound(new { code = "WALLET_NOT_FOUND", message = "Wallet not found." });
		}

		return Results.Ok(new
		{
			id = wallet.Id,
			status = wallet.Status.ToString(),
			updatedAt = wallet.UpdatedAt
		});
	}
	catch (InvalidOperationException ex) when (ex.Message == "WALLET_CLOSED")
	{
		return Results.Conflict(new { code = ex.Message, message = "Wallet is closed and cannot change status." });
	}
});

app.MapGet("/api/v1/wallets/{id:guid}/ledger", (Guid id, IWalletRepository repository) =>
{
	var wallet = repository.GetById(id);
	if (wallet is null)
	{
		return Results.NotFound(new { code = "WALLET_NOT_FOUND", message = "Wallet not found." });
	}

	var entries = repository.GetLedger(id);
	return Results.Ok(new
	{
		walletId = id,
		currency = wallet.Currency,
		entries
	});
});

app.MapLedgerEndpoints();

app.Run();

static WalletResponse ToWalletResponse(string awid, Wallet wallet)
{
	return new WalletResponse
	{
		Id = wallet.Id,
		WalletNumber = wallet.WalletNumber,
		Awid = awid,
		WalletType = wallet.WalletType.ToString(),
		Currency = wallet.Currency,
		Status = wallet.Status.ToString(),
		AvailableBalance = wallet.AvailableBalance,
		PendingBalance = wallet.PendingBalance,
		ReservedBalance = wallet.ReservedBalance,
		CreatedAt = wallet.CreatedAt,
		UpdatedAt = wallet.UpdatedAt
	};
}
