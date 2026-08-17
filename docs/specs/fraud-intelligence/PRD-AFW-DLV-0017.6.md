# AFW-DLV-0017.6 - Fraud Intelligence & Pattern Correlation Engine

## Objective

Correlate normalized fraud-related entities and historical observations into deterministic, explainable intelligence findings.

## Inputs

AWIDs, accounts, devices, beneficiaries, transactions, fraud decisions, and investigation cases may be represented by source snapshots.

## Initial patterns

- shared device;
- shared beneficiary;
- repeated high-risk transactions;
- repeated fraud cases;
- compound risk.

Every pattern exposes its type, score contribution, reason, entity identifiers, and timestamp.

## Restrictions

This delivery does not execute payment declines, account restrictions, wallet freezes, device revocations, legal fraud determinations, or opaque machine-learning classification. Successful validation means `READY FOR REVIEW`.