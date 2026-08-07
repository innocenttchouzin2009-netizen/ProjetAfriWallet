# Settlement Guide

## Execution flow

1. Create a settlement instruction with source and destination accounts.
2. Resolve FX quote when currencies differ.
3. Check source liquidity through Treasury gateway.
4. Post settlement through Treasury gateway.
5. Mark instruction as settled or rejected.

## Treasury isolation

Settlement never updates balances directly. All financial impact is posted through ITreasurySettlementGateway.
