# Provider Configuration Guide

## Model

`ProviderConfiguration` describes:

- provider code
- sandbox or production environment
- provider base URI
- credential lookup key
- webhook secret lookup key
- request timeout
- maximum retries
- enabled state

AFW-DLV-0014.6 defines this model but does not bind production provider
configuration from repository files. A production composition root must load and
validate configurations from approved environment-specific configuration.

## Rules

- Provider codes must be stable and case-insensitive at orchestration boundaries.
- Production base URIs must use HTTPS.
- Credential and webhook fields contain lookup keys, never secret values.
- Retry counts and timeouts must be bounded per provider.
- Disabled providers must not receive execution traffic.
- Sandbox and production configurations must not share credentials.

## Sandbox composition

The included API registers `SandboxCredentialService` and
`SandboxProviderExecutor`. The `/health` response reports `providerMode` as
`sandbox` so this composition cannot be mistaken for a certified connector.