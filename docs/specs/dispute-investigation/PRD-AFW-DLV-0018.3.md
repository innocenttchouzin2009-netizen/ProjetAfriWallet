# AFW-DLV-0018.3 - Evidence & Investigation Platform

## Objective

Manage the evidence collection and investigation workflow for a dispute claim that has already received an eligibility decision, without authorizing any refund, chargeback, or money movement.

## Inputs

- AFW-DLV-0018.2 Dispute Eligibility & Classification snapshot (`Eligible` or `ManualReviewRequired` claims only).

## Lifecycle

`Open` → `Assigned` → `WaitingForEvidence` → `UnderReview` → `Completed` → `Closed`.

## Capabilities

Analyst assignment, evidence requests, evidence submission with integrity metadata (SHA-256, size, content type), duplicate-evidence rejection, automatic fulfillment of matching evidence requests, investigation completion with an explicit outcome, and closure.

## Outputs

Investigation status, outcome, evidence count, open evidence request count, timeline length, and an audit record per lifecycle command.

## Critical boundary

This delivery does not approve refunds, execute refunds, initiate chargebacks, recover merchant funds, reverse ledger entries, or move money. It only manages evidence and investigation state.

Successful validation means `READY FOR REVIEW`.
