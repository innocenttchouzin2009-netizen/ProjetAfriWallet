# Provider Secrets Guide

## Policy

Provider credentials and webhook signing values must never be committed. Store
them in an approved secret manager and inject them into the runtime identity or
environment through the deployment platform.

## Webhook secret naming

The environment adapter resolves webhook secrets with this pattern:

```text
AFW_PROVIDER_<PROVIDER_CODE>_WEBHOOK_SECRET
```

Provider codes accepted by the HMAC verifier are alphanumeric. Logs and error
responses must never include the resolved value.

## Credential adapters

`SandboxCredentialService` returns a synthetic, short-lived sandbox value. It is
not suitable for production.

A production credential adapter must:

- authenticate to an approved secret or identity service
- return short-lived credentials where the provider supports them
- cache only within the approved security policy
- refresh before expiration
- avoid logging tokens or secret payloads
- surface availability without disclosing secret values

## Rotation

Secret rotation procedures must support overlapping keys where required by the
provider. Rotation must be tested in the target environment and recorded in the
operations audit trail.