# ADR-0016.1 — Compliance Profile & KYC Registry

## Status
Accepted

## Context
The compliance platform needs a first, sandbox-safe representation of customer KYC state before any production provider contracts are introduced. We need a registry that can hold profile data, document records, and review decisions while remaining provider-agnostic.

## Decision
We will model the compliance profile as a first-class domain entity with a lifecycle:
- draft
- pending review
- active
- rejected
- suspended

The system stores profile metadata, supporting documents, audit trail entries, and a simple review flow. All provider interactions remain abstract and are represented as sandbox metadata until a later integration phase validates real providers.

## Consequences
- The registry can support downstream KYC orchestration without committing to a specific provider.
- Auditability is preserved through a durable trail of actions and decisions.
- The model remains extensible for future sanctions, AML, and document verification adapters.
