using UniversalWallet.Api.Api.Balance;
using UniversalWallet.Api.Api.Currency;
using UniversalWallet.Api.Api.Fx;
using UniversalWallet.Api.Api.FxQuotes;
using UniversalWallet.Api.Api.Ledger;
using UniversalWallet.Api.Api.Payments;
using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Notifications.Api;
using UniversalWallet.Api.Notifications.Application;
using UniversalWallet.Api.Notifications.Infrastructure;
using UniversalWallet.Api.Payments.Api.Authorization;
using UniversalWallet.Api.Payments.Api.MerchantPayments;
using UniversalWallet.Api.Payments.Api.Execution;
using UniversalWallet.Api.Payments.Api.Receipts;
using UniversalWallet.Api.Payments.Api.Settlements;
using UniversalWallet.Api.Payments.Api.Timeline;
using UniversalWallet.Api.Payments.Api.Transfers;
using UniversalWallet.Api.Payments.Api.Validation;
using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Execution;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Application.MerchantPayments;
using UniversalWallet.Api.Payments.Application.Receipts;
using UniversalWallet.Api.Payments.Application.Settlements;
using UniversalWallet.Api.Payments.Application.Timeline;
using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Application.Validation;
using UniversalWallet.Api.Payments.Infrastructure.Authorizations;
using UniversalWallet.Api.Payments.Infrastructure.Intents;
using UniversalWallet.Api.Payments.Infrastructure.MerchantPayments;
using UniversalWallet.Api.Payments.Infrastructure.Receipts;
using UniversalWallet.Api.Payments.Infrastructure.Reservations;
using UniversalWallet.Api.Payments.Infrastructure.Risk;
using UniversalWallet.Api.Payments.Infrastructure.Settlements;
using UniversalWallet.Api.Payments.Infrastructure.Timeline;
using UniversalWallet.Api.Payments.Infrastructure.Transfers;
using UniversalWallet.Api.Application.Fx;
using UniversalWallet.Api.Application.FxQuotes;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Application.Wallets;
using UniversalWallet.Api.Infrastructure.Balance;
using UniversalWallet.Api.Infrastructure.Currency;
using UniversalWallet.Api.Infrastructure.Ledger;
using UniversalWallet.Api.Infrastructure.Observability;
using UniversalWallet.Api.WalletEngine;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<IWalletCurrencyReader, WalletCurrencyReader>();
builder.Services.AddSingleton<InMemoryLedgerRepository>();
builder.Services.AddSingleton<ILedgerRepository>(sp => sp.GetRequiredService<InMemoryLedgerRepository>());
builder.Services.AddSingleton<ILedgerProjectionReader>(sp => sp.GetRequiredService<InMemoryLedgerRepository>());
builder.Services.AddSingleton<ILedgerJournalRepository, InMemoryLedgerJournalRepository>();
builder.Services.AddSingleton<LedgerValidator>();
builder.Services.AddSingleton<LedgerPostingService>();
builder.Services.AddSingleton<PostTransactionHandler>();
builder.Services.AddSingleton<ReverseTransactionHandler>();
builder.Services.AddSingleton<IBalanceProjectionRepository, InMemoryBalanceProjectionRepository>();
builder.Services.AddSingleton<IBalanceSnapshotRepository, InMemoryBalanceSnapshotRepository>();
builder.Services.AddSingleton<IProjectionVersionRepository, InMemoryProjectionVersionRepository>();
builder.Services.AddSingleton<BalanceProjectionService>();
builder.Services.AddSingleton<WalletReadModelService>();
builder.Services.AddSingleton<InMemoryPaymentIntentRepository>();
builder.Services.AddSingleton<IPaymentIntentRepository>(sp => sp.GetRequiredService<InMemoryPaymentIntentRepository>());
builder.Services.AddSingleton<IPaymentRecipientResolver, PaymentRecipientResolver>();
builder.Services.AddSingleton<IPaymentWalletReader, PaymentWalletReader>();
builder.Services.AddSingleton<CreatePaymentIntentHandler>();
builder.Services.AddSingleton<GetPaymentIntentHandler>();
builder.Services.AddSingleton<ListPaymentIntentsHandler>();
builder.Services.AddSingleton<CancelPaymentIntentHandler>();
builder.Services.AddSingleton<ExpirePaymentIntentsHandler>();
builder.Services.AddSingleton<InMemoryAuthorizationRepository>();
builder.Services.AddSingleton<IPaymentAuthorizationRepository>(sp => sp.GetRequiredService<InMemoryAuthorizationRepository>());
builder.Services.AddSingleton<InMemoryReservationRepository>();
builder.Services.AddSingleton<IPaymentReservationRepository>(sp => sp.GetRequiredService<InMemoryReservationRepository>());
builder.Services.AddSingleton<IPaymentRiskEngine, DefaultRiskEngine>();
builder.Services.AddSingleton<IPaymentLimitEngine, DefaultLimitEngine>();
builder.Services.AddSingleton<ValidatePaymentIntentHandler>();
builder.Services.AddSingleton<AuthorizePaymentIntentHandler>();
builder.Services.AddSingleton<ExecutePaymentIntentHandler>();
builder.Services.AddSingleton<CreateTransferHandler>();
builder.Services.AddSingleton<CreateSettlementHandler>();
builder.Services.AddSingleton<PaymentTimelineProjector>();
builder.Services.AddSingleton<GetPaymentTimelineHandler>();
builder.Services.AddSingleton<LookupPaymentTimelineHandler>();
builder.Services.AddSingleton<GenerateReceiptHandler>();
builder.Services.AddSingleton<VerifyReceiptHandler>();
builder.Services.AddSingleton<IPaymentTransferRepository, InMemoryPaymentTransferRepository>();
builder.Services.AddSingleton<ISettlementRepository, InMemorySettlementRepository>();
builder.Services.AddSingleton<ISettlementProvider, InternalSettlementProvider>();
builder.Services.AddSingleton<IPaymentTimelineRepository, InMemoryTimelineItemRepository>();
builder.Services.AddSingleton<IPaymentReceiptRepository, InMemoryReceiptRepository>();
builder.Services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();
builder.Services.AddSingleton<IMerchantProfileRepository, InMemoryMerchantProfileRepository>();
builder.Services.AddSingleton<IMerchantPaymentRequestRepository, InMemoryMerchantPaymentRequestRepository>();
builder.Services.AddSingleton<IMerchantQrTokenRepository, InMemoryMerchantQrTokenRepository>();
builder.Services.AddSingleton<ResolveMerchantQrHandler>();
builder.Services.AddSingleton<CreateMerchantPaymentRequestHandler>();
builder.Services.AddSingleton<INotificationPreferencesRepository, InMemoryNotificationPreferencesRepository>();
builder.Services.AddSingleton<CreateNotificationHandler>();
builder.Services.AddSingleton<NotificationPreferencesHandler>();
builder.Services.AddSingleton<INotificationChannelProvider, InAppProvider>();
builder.Services.AddSingleton<INotificationChannelProvider, PushProvider>();
builder.Services.AddSingleton<INotificationChannelProvider, EmailProvider>();
builder.Services.AddFxEngine();
builder.Services.AddFxQuotes();
builder.Services.AddSingleton<IWalletRepository>(sp => new InMemoryWalletRepository(sp.GetRequiredService<ICurrencyRegistryRepository>()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<HttpCorrelationMiddleware>();
HealthChecks.MapHealthChecks(app);

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

app.MapGet("/api/v1/wallets/{id:guid}/read-model", (Guid id, WalletReadModelService service) =>
{
	try
	{
		return Results.Ok(service.GetDetail(id));
	}
	catch (InvalidOperationException ex) when (ex.Message == "WALLET_NOT_FOUND")
	{
		return Results.NotFound(new { code = ex.Message, message = "Wallet not found." });
	}
});

app.MapGet("/api/v1/wallets/portfolio/{awid}", (string awid, WalletReadModelService service) =>
{
	if (string.IsNullOrWhiteSpace(awid))
	{
		return Results.BadRequest(new { code = "AWID_REQUIRED", message = "awid path parameter is required." });
	}

	return Results.Ok(service.GetPortfolioSummary(awid));
});

app.MapLedgerEndpoints();
app.MapBalanceEndpoints();
app.MapCurrencyEndpoints();
app.MapFxEndpoints();
app.MapQuoteEndpoints();
app.MapPaymentIntentEndpoints();
app.MapPaymentValidationEndpoints();
app.MapPaymentAuthorizationEndpoints();
app.MapPaymentExecutionEndpoints();
app.MapPaymentTransferEndpoints();
app.MapPaymentSettlementEndpoints();
app.MapPaymentTimelineEndpoints();
app.MapPaymentReceiptEndpoints();
app.MapNotificationEndpoints();
app.MapMerchantPaymentEndpoints();

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
