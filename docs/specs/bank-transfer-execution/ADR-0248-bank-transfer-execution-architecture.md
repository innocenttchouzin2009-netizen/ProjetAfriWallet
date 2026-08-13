# ADR-0248 — Bank Transfer Execution Architecture

## Status

Accepted.

## Context

AFW-DLV-0015.4 executes a previously routed transfer using a provider-neutral execution boundary. The intent and routing engines remain upstream responsibilities.

## Decision

The execution platform owns: execution record creation, idempotency checks, consistency validation, provider submission, provider reference capture, completion lifecycle, and failure handling foundations.

It does not own:

- transfer intent creation
- routing decision generation
- real connector commissioning

## Consequences

This keeps the system boundary explicit and ensures the execution layer remains a controlled, testable step between routing and provider integration.
