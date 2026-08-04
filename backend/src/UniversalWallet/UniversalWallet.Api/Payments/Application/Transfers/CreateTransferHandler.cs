using UniversalWallet.Api.Application.Balance;
using UniversalWallet.Api.Application.Ledger;
using UniversalWallet.Api.Domain.Ledger;
using UniversalWallet.Api.Payments.Application.Authorization;
using UniversalWallet.Api.Payments.Application.Intents;
using UniversalWallet.Api.Payments.Domain.Authorizations;
using UniversalWallet.Api.Payments.Domain.Intents;
using UniversalWallet.Api.Payments.Domain.Reservations;
using UniversalWallet.Api.Payments.Domain.Transfers;
using UniversalWallet.Api.WalletEngine;

namespace UniversalWallet.Api.Payments.Application.Transfers;

public sealed record CreateTransferRequest(Guid PaymentIntentId);

public sealed record CreateTransferResponse(Guid TransferId, PaymentTransferStatus Status, Guid? LedgerTransactionId);

public interface IPaymentTransferRepository
{
    Task<PaymentTransfer?> GetByIntentAsync(Guid paymentIntentId, CancellationToken cancellationToken = default);
    Task<PaymentTransfer?> GetAsync(Guid transferId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentTransfer>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransfer transfer, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentTransfer transfer, CancellationToken cancellationToken = default);
}

public sealed class CreateTransferHandler
{
    private readonly IPaymentIntentRepository _intentRepository;
    private readonly IPaymentAuthorizationRepository _authorizationRepository;
    private readonly IPaymentReservationRepository _reservationRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPaymentTransferRepository _transferRepository;
    private readonly PostTransactionHandler _postTransactionHandler;
    private readonly BalanceProjectionService _balanceProjectionService;

    public CreateTransferHandler(
        IPaymentIntentRepository intentRepository,
        IPaymentAuthorizationRepository authorizationRepository,
        IPaymentReservationRepository reservationRepository,
        IWalletRepository walletRepository,
        IPaymentTransferRepository transferRepository,
        PostTransactionHandler postTransactionHandler,
        BalanceProjectionService balanceProjectionService)
    {
        _intentRepository = intentRepository;
        _authorizationRepository = authorizationRepository;
        _reservationRepository = reservationRepository;
        _walletRepository = walletRepository;
        _transferRepository = transferRepository;
        _postTransactionHandler = postTransactionHandler;
        _balanceProjectionService = balanceProjectionService;
    }

    public async Task<CreateTransferResponse> HandleAsync(CreateTransferRequest request, Guid payerAwid, string deviceId, string sessionId, CancellationToken cancellationToken = default)
    {
        var existingTransfer = await _transferRepository.GetByIntentAsync(request.PaymentIntentId, cancellationToken);
        if (existingTransfer is not null)
        {
            return new CreateTransferResponse(existingTransfer.TransferId, existingTransfer.Status, existingTransfer.LedgerTransactionId);
        }

        var intent = await _intentRepository.GetAsync(request.PaymentIntentId, cancellationToken);
        if (intent is null)
        {
            throw new InvalidOperationException("PAYMENT_INTENT_NOT_FOUND");
        }

        if (intent.Status == PaymentIntentStatus.Completed)
        {
            throw new InvalidOperationException("PAYMENT_ALREADY_EXECUTED");
        }

        var authorization = await _authorizationRepository.GetByIntentAsync(request.PaymentIntentId, cancellationToken);
        if (authorization is null || authorization.Decision != PaymentAuthorizationDecision.Approved)
        {
            throw new InvalidOperationException("PAYMENT_AUTHORIZATION_REQUIRED");
        }

        var reservation = await _reservationRepository.GetByIntentAsync(request.PaymentIntentId, cancellationToken);
        if (reservation is null)
        {
            throw new InvalidOperationException("PAYMENT_RESERVATION_NOT_FOUND");
        }

        if (!reservation.CanConsume)
        {
            throw new InvalidOperationException("PAYMENT_RESERVATION_NOT_ACTIVE");
        }

        var sourceWallet = _walletRepository.GetById(intent.SourceWalletId);
        var destinationWallet = _walletRepository.GetById(intent.DestinationWalletId ?? Guid.Empty);
        if (sourceWallet is null || destinationWallet is null)
        {
            throw new InvalidOperationException("PAYMENT_WALLET_NOT_FOUND");
        }

        if (sourceWallet.AwidId != payerAwid)
        {
            throw new InvalidOperationException("PAYMENT_SOURCE_WALLET_FORBIDDEN");
        }

        var transfer = new PaymentTransfer
        {
            PaymentIntentId = intent.Id,
            AuthorizationId = authorization.Id,
            ReservationId = reservation.Id,
            SourceWalletId = sourceWallet.Id,
            DestinationWalletId = destinationWallet.Id,
            AmountMinor = intent.AmountMinor,
            CurrencyCode = intent.CurrencyCode,
            Status = PaymentTransferStatus.PostingLedger,
            CorrelationId = request.PaymentIntentId.ToString(),
            Version = 1
        };

        await _transferRepository.AddAsync(transfer, cancellationToken);

        var amount = (decimal)intent.AmountMinor / 100m;
        var postingRequest = new PostTransactionRequest
        {
            IdempotencyKey = $"transfer-{transfer.TransferId:N}",
            Awid = sourceWallet.AwidId.ToString(),
            Currency = intent.CurrencyCode,
            Reference = $"TRANSFER-{transfer.TransferId:N}",
            CorrelationId = transfer.CorrelationId,
            PostedBy = "backend",
            Session = sessionId,
            Device = deviceId,
            Lines =
            [
                new LedgerLineRequest { WalletId = sourceWallet.Id, EntryType = EntryType.Debit, Amount = amount, Description = $"Transfer out {transfer.TransferId:N}", Compartment = LedgerBalanceCompartment.Available },
                new LedgerLineRequest { WalletId = destinationWallet.Id, EntryType = EntryType.Credit, Amount = amount, Description = $"Transfer in {transfer.TransferId:N}", Compartment = LedgerBalanceCompartment.Available }
            ]
        };

        var postingResult = _postTransactionHandler.Handle(postingRequest);
        if (!postingResult.Accepted)
        {
            transfer.Status = PaymentTransferStatus.Failed;
            await _transferRepository.UpdateAsync(transfer, cancellationToken);
            throw new InvalidOperationException(postingResult.Code ?? "LEDGER_TRANSACTION_FAILED");
        }

        reservation.Consume();
        await _reservationRepository.UpdateAsync(reservation, cancellationToken);

        transfer.Status = PaymentTransferStatus.Projecting;
        transfer.LedgerTransactionId = postingResult.Transaction?.TransactionId;
        await _transferRepository.UpdateAsync(transfer, cancellationToken);

        intent.Status = PaymentIntentStatus.Completed;
        await _intentRepository.AddAsync(intent, cancellationToken);

        _balanceProjectionService.RebuildFromLedger(sourceWallet.Id);
        _balanceProjectionService.RebuildFromLedger(destinationWallet.Id);

        transfer.Status = PaymentTransferStatus.Completed;
        transfer.ExecutedAt = DateTimeOffset.UtcNow;
        await _transferRepository.UpdateAsync(transfer, cancellationToken);

        return new CreateTransferResponse(transfer.TransferId, transfer.Status, transfer.LedgerTransactionId);
    }
}
