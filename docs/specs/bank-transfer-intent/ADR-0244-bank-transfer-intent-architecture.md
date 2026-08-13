# ADR-0244 — Bank Transfer Intent Architecture

## Status

Accepted

## Context

The banking platform must describe a transfer intent before any rail choice or execution occurs. This arrangement allows the system to validate identity, eligibility, amount, and lifecycle safely before routing.

## Decision

We introduce a dedicated BankTransferIntent aggregate that owns the lifecycle and validation of a transfer request. The engine remains a declaration layer only and depends on a beneficiary registry gateway for eligibility checks.

## Consequences

- The domain owns intent creation, confirmation, and cancellation rules.
- Routing and execution remain separate deliveries.
- The architecture preserves clear separation of concerns and reduces wrong rail binding too early.
