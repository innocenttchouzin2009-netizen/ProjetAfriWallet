# ADR-0216 - Settlement Orchestration Boundary

Settlement orchestrates instructions and validates prerequisites.
All balance-impacting operations are delegated to Treasury through ITreasurySettlementGateway.
Settlement never modifies account balances directly.
