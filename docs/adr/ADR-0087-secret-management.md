# ADR-0087: Secret Management

## Status
Accepted

## Context
Production deployments require secrets to be sourced from a centralized and auditable mechanism without embedding them in source code.

## Decision
The platform uses a secret provider abstraction with environment variables as the default implementation and placeholders for Azure Key Vault, AWS Secrets Manager, and HashiCorp Vault.

## Consequences
The application can validate and retrieve secrets consistently while preserving a path toward managed secret backends.
