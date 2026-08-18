# AFW-DLV-0018.1 - Dispute Case & Claim Registry

## Objective

Provide the canonical registry for customer dispute claims and their lifecycle history.

## Responsibilities

- register a claim with its type, reason, amount, currency, description, and source channel;
- retain opaque payment, bank transfer, and merchant references;
- link evidence references, including fraud findings;
- manage the claim lifecycle and reject/cancel paths;
- produce audit evidence for every transition.

## Lifecycle

`DRAFT -> SUBMITTED -> OPEN -> UNDER_REVIEW -> RESOLVED -> CLOSED`, with `REJECTED` and `CANCELLED` as terminal alternatives. Terminal claims are immutable.

## Boundaries

A fraud finding is evidence, not a dispute decision. This delivery does not decide refunds, execute chargebacks, initiate recovery, or mutate ledger state.

Successful validation means `READY FOR REVIEW`.
