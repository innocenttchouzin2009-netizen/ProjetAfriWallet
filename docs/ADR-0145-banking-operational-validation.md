# ADR-0145 — Banking Operational Validation

## Status
Accepted

## Context
Operational validation must be repeatable and scriptable so banking releases can be assessed without manual intervention.

## Decision
A PowerShell validation script will generate a release package and produce a validation report that captures the readiness checks for banking.

## Consequences
Release validation becomes deterministic and suitable for CI/CD or pre-release review.
