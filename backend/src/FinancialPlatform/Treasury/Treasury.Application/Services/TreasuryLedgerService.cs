using Treasury.Contracts;
using Treasury.Domain.Accounts;
using Treasury.Domain.Balances;
using Treasury.Domain.Ledger;
using Treasury.Domain.Liquidity;
using Treasury.Domain.Reservations;
using Treasury.Domain.Settlement;
using Treasury.Infrastructure.Stores;

namespace Treasury.Application.Services;

public sealed class TreasuryLedgerService
{
    private readonly TreasuryLedgerStore _store;

    public TreasuryLedgerService(TreasuryLedgerStore store)
    {
        _store = store;
    }

    public TreasuryAccountResponse CreateAccount(
        CreateTreasuryAccountRequest request)
    {
        var account = new TreasuryAccount(
            request.AccountId,
            request.Name,
            request.Currency);

        _store.Accounts[request.AccountId] = account;
        _store.Balances[request.AccountId] = new TreasuryBalance(
            request.AccountId,
            0m,
            0m);

        _store.AuditTrail.Add($"AccountCreated:{request.AccountId}");
        _store.TelemetryEvents.Add("treasury.account.created");

        return new TreasuryAccountResponse(
            account.AccountId,
            account.Name,
            account.Currency);
    }

    public void PostLedgerTransaction(
        PostLedgerTransactionRequest request)
    {
        var debitEntry = new TreasuryEntry(
            request.DebitAccountId,
            request.Amount,
            "DEBIT");

        var creditEntry = new TreasuryEntry(
            request.CreditAccountId,
            request.Amount,
            "CREDIT");

        var transaction = new TreasuryTransaction(
            request.TransactionId,
            new[] { debitEntry, creditEntry },
            DateTime.UtcNow);

        _store.Ledger.Post(transaction);

        _store.Balances[request.DebitAccountId].ApplyDebit(request.Amount);
        _store.Balances[request.CreditAccountId].ApplyCredit(request.Amount);

        _store.AuditTrail.Add($"TransactionPosted:{request.TransactionId}");
        _store.TelemetryEvents.Add("treasury.ledger.posted");
    }

    public void CreateReservation(
        CreateReservationRequest request)
    {
        var reservation = new TreasuryReservation(
            request.ReservationId,
            request.AccountId,
            request.Amount);

        _store.Reservations[request.ReservationId] = reservation;
        _store.Balances[request.AccountId].Reserve(request.Amount);

        _store.AuditTrail.Add($"ReservationCreated:{request.ReservationId}");
        _store.TelemetryEvents.Add("treasury.reservation.created");
    }

    public void ReleaseReservation(string reservationId)
    {
        var reservation = _store.Reservations[reservationId];

        if (!reservation.Active)
        {
            return;
        }

        reservation.Release();
        _store.Balances[reservation.AccountId].Release(reservation.Amount);

        _store.AuditTrail.Add($"ReservationReleased:{reservationId}");
        _store.TelemetryEvents.Add("treasury.reservation.released");
    }

    public TreasuryBalanceResponse GetBalance(string accountId)
    {
        var balance = _store.Balances[accountId];

        return new TreasuryBalanceResponse(
            balance.AccountId,
            balance.Available,
            balance.Reserved,
            balance.Total);
    }

    public LiquiditySnapshot GetLiquiditySnapshot()
    {
        return new LiquiditySnapshot(
            DateTime.UtcNow,
            _store.Balances.Values.Sum(x => x.Available),
            _store.Balances.Values.Sum(x => x.Reserved));
    }

    public SettlementPositionResponse GetSettlementPosition(
        string partner,
        string currency)
    {
        var net = _store.Balances.Values.Sum(x => x.Total);

        var position = new SettlementPosition(
            partner,
            currency,
            net);

        return new SettlementPositionResponse(
            position.Partner,
            position.Currency,
            position.NetAmount);
    }

    public int AuditCount() => _store.AuditTrail.Count;

    public int TelemetryCount() => _store.TelemetryEvents.Count;
}
