# ADR-0100: Deployment Strategy

- Status: Accepted
- Date: 2026-08-04

## Context
AfriWallet needs safe deployment patterns for development, integration, staging, and production environments.

## Decision
We will support progressive deployment workflows with a rolling, blue/green, or canary strategy selected by environment and infrastructure. The initial scaffold provides a deployment entry point and environment selector for staging and production use cases.

## Consequences
The platform can evolve toward safer releases while keeping the initial automation simple and auditable.
