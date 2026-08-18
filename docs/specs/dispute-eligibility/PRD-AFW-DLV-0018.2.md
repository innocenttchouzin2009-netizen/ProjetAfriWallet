# AFW-DLV-0018.2 - Dispute Eligibility & Classification Engine

## Objective

Evaluate whether a registered dispute claim is technically eligible for further processing, and classify it into an explainable dispute category.

## Inputs

- AFW-DLV-0018.1 Dispute Case & Claim Registry snapshot;
- transaction reference snapshot.

## Eligibility checks

Transaction existence, AWID match, currency match, claim amount validity, submission window, transaction completion state, and supported claim type.

## Statuses

`Eligible`, `Ineligible`, `ManualReviewRequired`.

## Outputs

Eligibility status, primary reason, classification, rule-by-rule evidence, and an audit record.

## Critical boundary

This delivery does not approve refunds, execute refunds, initiate chargebacks, recover merchant funds, reverse ledger entries, or move money.

## Policy boundary

The 120-day submission window is an internal sandbox policy. It is not represented as a universal regulatory requirement.

Successful validation means `READY FOR REVIEW`.
