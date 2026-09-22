using Microsoft.EntityFrameworkCore;
using Treasury.Application.Interfaces;
using Treasury.Domain.Accounts;
using Treasury.Domain.Ledger;
using Treasury.Domain.Reservations;
using Treasury.Infrastructure.Persistence;

namespace Treasury.Infrastructure.Repositories;

public sealed class EfTreasuryRepository(TreasuryDbContext dbContext) : ITreasuryRepository
{
    public async Task AddAccountAsync(TreasuryAccount account, CancellationToken cancellationToken)
    {
        if (await dbContext.Accounts.AnyAsync(x => x.AccountId == account.AccountId || x.AccountCode == account.AccountCode, cancellationToken))
            throw new InvalidOperationException("Treasury account already exists.");

        dbContext.Accounts.Add(new TreasuryAccountEntity
        {
            AccountId = account.AccountId,
            AccountCode = account.AccountCode,
            DisplayName = account.DisplayName,
            CurrencyCode = account.CurrencyCode,
            Type = (int)account.Type,
            Status = (int)account.Status,
            CreatedAtUtc = account.CreatedAtUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TreasuryAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
        return row is null ? null : TreasuryAccount.Restore(
            row.AccountId, row.AccountCode, row.DisplayName, row.CurrencyCode,
            (TreasuryAccountType)row.Type, (TreasuryAccountStatus)row.Status, row.CreatedAtUtc);
    }

    public async Task AddTransactionAsync(TreasuryTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.Status != TreasuryTransactionStatus.Posted)
            throw new InvalidOperationException("Only posted transactions can enter the journal.");

        if (await dbContext.Transactions.AnyAsync(
            x => x.TransactionId == transaction.TransactionId || x.CorrelationId == transaction.CorrelationId,
            cancellationToken))
            throw new InvalidOperationException("Treasury transaction already exists.");

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Transactions.Add(new TreasuryTransactionEntity
        {
            TransactionId = transaction.TransactionId,
            Reference = transaction.Reference,
            CorrelationId = transaction.CorrelationId,
            Status = (int)transaction.Status,
            CreatedAtUtc = transaction.CreatedAtUtc,
            PostedAtUtc = transaction.PostedAtUtc
        });

        dbContext.Entries.AddRange(transaction.Entries.Select(entry => new TreasuryEntryEntity
        {
            EntryId = entry.EntryId,
            TransactionId = entry.TransactionId,
            AccountId = entry.AccountId,
            CurrencyCode = entry.CurrencyCode,
            DebitMinor = entry.DebitMinor,
            CreditMinor = entry.CreditMinor,
            Reference = entry.Reference,
            PostedAtUtc = entry.PostedAtUtc
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<TreasuryTransaction?> GetTransactionByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(correlationId)) return null;
        var transaction = await dbContext.Transactions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CorrelationId == correlationId.Trim(), cancellationToken);
        if (transaction is null) return null;

        var entries = await dbContext.Entries.AsNoTracking()
            .Where(x => x.TransactionId == transaction.TransactionId)
            .OrderBy(x => x.PostedAtUtc)
            .ThenBy(x => x.EntryId)
            .Select(x => new TreasuryEntry(
                x.EntryId, x.TransactionId, x.AccountId, x.CurrencyCode,
                x.DebitMinor, x.CreditMinor, x.Reference, x.PostedAtUtc))
            .ToListAsync(cancellationToken);

        return TreasuryTransaction.Restore(
            transaction.TransactionId,
            transaction.Reference,
            transaction.CorrelationId,
            (TreasuryTransactionStatus)transaction.Status,
            transaction.CreatedAtUtc,
            transaction.PostedAtUtc,
            entries);
    }

    public async Task<IReadOnlyCollection<TreasuryEntry>> GetEntriesAsync(Guid accountId, CancellationToken cancellationToken) =>
        await dbContext.Entries.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.PostedAtUtc)
            .ThenBy(x => x.EntryId)
            .Select(x => new TreasuryEntry(
                x.EntryId, x.TransactionId, x.AccountId, x.CurrencyCode,
                x.DebitMinor, x.CreditMinor, x.Reference, x.PostedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task AddReservationAsync(TreasuryReservation reservation, CancellationToken cancellationToken)
    {
        if (await dbContext.Reservations.AnyAsync(x => x.ReservationId == reservation.ReservationId, cancellationToken))
            throw new InvalidOperationException("Treasury reservation already exists.");

        dbContext.Reservations.Add(Map(reservation));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveReservationAsync(TreasuryReservation reservation, CancellationToken cancellationToken)
    {
        var row = await dbContext.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservation.ReservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Treasury reservation not found.");

        row.Status = (int)reservation.Status;
        row.ReleasedAtUtc = reservation.ReleasedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TreasuryReservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Reservations.AsNoTracking().SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);
        return row is null ? null : Restore(row);
    }

    public async Task<IReadOnlyCollection<TreasuryReservation>> GetReservationsAsync(Guid accountId, CancellationToken cancellationToken) =>
        (await dbContext.Reservations.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken))
        .Select(Restore).ToArray();

    private static TreasuryReservationEntity Map(TreasuryReservation value) => new()
    {
        ReservationId = value.ReservationId,
        AccountId = value.AccountId,
        CurrencyCode = value.CurrencyCode,
        AmountMinor = value.AmountMinor,
        Reference = value.Reference,
        Status = (int)value.Status,
        CreatedAtUtc = value.CreatedAtUtc,
        ReleasedAtUtc = value.ReleasedAtUtc
    };

    private static TreasuryReservation Restore(TreasuryReservationEntity row) =>
        TreasuryReservation.Restore(
            row.ReservationId, row.AccountId, row.CurrencyCode, row.AmountMinor,
            row.Reference, (TreasuryReservationStatus)row.Status, row.CreatedAtUtc, row.ReleasedAtUtc);
}
