using Treasury.Domain.Accounts;
using Treasury.Domain.Balances;
using Treasury.Domain.Ledger;
using Treasury.Domain.Reservations;

namespace Treasury.Infrastructure.Stores;

public sealed class TreasuryLedgerStore
{
    public Dictionary<string, TreasuryAccount> Accounts { get; } = new();

    public Dictionary<string, TreasuryBalance> Balances { get; } = new();

    public Dictionary<string, TreasuryReservation> Reservations { get; } = new();

    public TreasuryLedger Ledger { get; } = new();

    public List<string> AuditTrail { get; } = new();

    public List<string> TelemetryEvents { get; } = new();
}
