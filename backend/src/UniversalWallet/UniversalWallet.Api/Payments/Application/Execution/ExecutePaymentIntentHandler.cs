using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Application.Execution;

public sealed record ExecutePaymentIntentResponse(
    Guid IntentId,
    PaymentIntentStatus Status,
    Guid ExecutionId,
    FundsReservationStatus ReservationStatus,
    PostingResult? PostingResult,
    string NextAction);

public sealed class ExecutePaymentIntentHandler
{
    private readonly IPaymentIntentRepository _intentRepository;
    private readonly IPaymentAuthorizationRepository _authorizationRepository;
    private readonly IPaymentReservationRepository _reservationRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly PostTransactionHandler _postTransactionHandler;
    private readonly BalanceProjectionService _balanceProjectionService;

    public ExecutePaymentIntentHandler(
        IPaymentIntentRepository intentRepository,
        IPaymentAuthorizationRepository authorizationRepository,
        IPaymentReservationRepository reservationRepository,
        IWalletRepository walletRepository,
        PostTransactionHandler postTransactionHandler,
        BalanceProjectionService balanceProjectionService)
    {
        _intentRepository = intentRepository;
        _authorizationRepository = authorizationRepository;
        _reservationRepository = reservationRepository;
        _walletRepository = walletRepository;
        _postTransactionHandler = postTransactionHandler;
        _balanceProjectionService = balanceProjectionService;
    }

    public async Task<ExecutePaymentIntentResponse> HandleAsync(Guid intentId, Guid payerAwid, string deviceId, string sessionId, CancellationToken cancellationToken = default)
    {
        var intent = await _intentRepository.GetAsync(intentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        if (intent.Status == PaymentIntentStatus.Completed)
        {
            var existingAuthorization = await _authorizationRepository.GetByIntentAsync(intentId, cancellationToken);
            var existingReservation = await _reservationRepository.GetByIntentAsync(intentId, cancellationToken);
            return new ExecutePaymentIntentResponse(intent.Id, intent.Status, existingAuthorization?.Id ?? Guid.Empty, existingReservation?.Status ?? FundsReservationStatus.Active, null, "COMPLETE");
        }

        if (intent.Status != PaymentIntentStatus.Authorized)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_EXECUTABLE");
        }

        var wallet = _walletRepository.GetById(intent.SourceWalletId);
        var destinationWallet = _walletRepository.GetById(intent.DestinationWalletId ?? Guid.Empty);
        if (wallet is null || destinationWallet is null)
        {
            throw new InvalidOperationException("PAYMENT_WALLET_NOT_FOUND");
        }

        if (wallet.AwidId != payerAwid)
        {
            throw new InvalidOperationException("PAYMENT_SOURCE_WALLET_FORBIDDEN");
        }

        if (wallet.Status != WalletStatus.Active || destinationWallet.Status != WalletStatus.Active)
        {
            throw new InvalidOperationException("PAYMENT_WALLET_NOT_ACTIVE");
        }

        var authorization = await _authorizationRepository.GetByIntentAsync(intentId, cancellationToken);
        if (authorization is null || authorization.Decision != PaymentAuthorizationDecision.Approved)
        {
            throw new InvalidOperationException("PAYMENT_AUTHORIZATION_REQUIRED");
        }

        var reservation = await _reservationRepository.GetByIntentAsync(intentId, cancellationToken);
        if (reservation is null)
        {
            throw new InvalidOperationException("PAYMENT_RESERVATION_NOT_FOUND");
        }

        if (!reservation.CanConsume)
        {
            throw new InvalidOperationException("PAYMENT_RESERVATION_NOT_ACTIVE");
        }

        var amount = (decimal)intent.AmountMinor / 100m;
        var postingRequest = new PostTransactionRequest
        {
            IdempotencyKey = $"payment-exec-{intent.Id:N}",
            Awid = wallet.AwidId.ToString(),
            Currency = intent.CurrencyCode,
            Reference = $"PAYMENT-{intent.Id:N}",
            CorrelationId = intent.Id.ToString(),
            PostedBy = "backend",
            Session = sessionId,
            Device = deviceId,
            Lines =
            [
                new LedgerLineRequest { WalletId = wallet.Id, EntryType = EntryType.Debit, Amount = amount, Description = $"Payment out {intent.Id:N}", Compartment = LedgerBalanceCompartment.Available },
                new LedgerLineRequest { WalletId = destinationWallet.Id, EntryType = EntryType.Credit, Amount = amount, Description = $"Payment in {intent.Id:N}", Compartment = LedgerBalanceCompartment.Available }
            ]
        };

        var postingResult = _postTransactionHandler.Handle(postingRequest);
        if (!postingResult.Accepted)
        {
            throw new InvalidOperationException(postingResult.Code ?? "PAYMENT_POSTING_FAILED");
        }

        reservation.Consume();
        await _reservationRepository.UpdateAsync(reservation, cancellationToken);

        intent.Status = PaymentIntentStatus.Completed;
        await _intentRepository.AddAsync(intent, cancellationToken);

        _balanceProjectionService.RebuildFromLedger(wallet.Id);
        _balanceProjectionService.RebuildFromLedger(destinationWallet.Id);

        return new ExecutePaymentIntentResponse(intent.Id, intent.Status, authorization.Id, reservation.Status, postingResult, "COMPLETE");
    }
}
