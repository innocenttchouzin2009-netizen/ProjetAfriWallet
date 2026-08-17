# AFW-DLV-0017.4 - Fraud Rules & Decision Engine

## Objective

Produce a canonical fraud decision from previously calculated fraud signals and risk outputs.

## Responsibilities

- normalize device and transaction risk inputs;
- evaluate explicit, explainable fraud rules;
- combine transaction fraud at 65% and device risk at 35%;
- determine a decision band and action;
- persist the decision and its audit evidence.

## Execution boundary

`DECLINE_RECOMMENDED` is a recommendation only. This delivery does not decline payments, cancel transfers, freeze wallets, suspend accounts, revoke devices, execute MFA, or modify ledger state.

## Inputs

- AFW-DLV-0017.2 Device & Account Risk Engine;
- AFW-DLV-0017.3 Transaction Fraud Detection Engine.

Successful validation means `READY FOR REVIEW`, not payment declined, account blocked, or fraud legally confirmed.