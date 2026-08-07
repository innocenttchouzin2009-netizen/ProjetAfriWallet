# PRD - AFW-DLV-0013.3 Multi-Currency Settlement Engine

## Objective

Implement a settlement orchestration engine that executes multi-currency instructions by delegating all financial postings to Treasury.

## Scope

- Settlement instruction lifecycle: pending, settled, rejected
- Cross-currency conversion with quote traceability
- Liquidity pre-check before execution
- Batch execution support
- Settlement position aggregation
- API endpoints for instruction, batch, positions, and quotes

## Non-Goals

- Direct balance mutation in Settlement
- Accounting entry creation outside Treasury gateway
