# Execution Guide

The bank transfer execution platform consumes an already ready transfer intent and an approved routing decision. It validates both before creating a provider-neutral execution record.

## Lifecycle

1. Validate transfer intent exists and is ready for routing.
2. Validate routing decision is present and matches the request.
3. Create an execution record with idempotency protection.
4. Submit to the provider gateway.
5. Capture provider reference.
6. Complete the execution when the submission is accepted.

## Sandbox constraints

All provider interactions remain sandboxed and are not production bank certification claims.
