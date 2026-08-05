# ADR-0161 — Merchant KYC Workflow

## Status
Accepted

## Context
Merchant onboarding requires a structured KYC workflow that can manage requirements, capture review outcomes, and support manual review for exceptions.

## Decision
We will represent KYC as a case-based workflow with a status model and requirement list. The workflow supports submission, approval, rejection, and future manual review without hardcoding a specific provider.

## Consequences
- KYC decisions are traceable and reviewable.
- Future provider integrations can be plugged into the same workflow contract.
- The system can support manual review alongside automated decisioning.
