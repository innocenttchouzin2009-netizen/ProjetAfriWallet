# Webhook Security Guide

## AFW-DLV-0014.6 foundation

`HmacWebhookVerifier`:

- resolves the provider webhook secret at runtime
- computes HMAC-SHA256 over the exact received payload
- accepts a hexadecimal signature
- rejects malformed hexadecimal signatures
- compares binary digests using a timing-safe primitive
- rejects path and body provider mismatches at the API boundary

## Production connector requirements

Before a production connector is certified, it must also implement the exact
operator contract for:

- signature header extraction
- payload canonicalization
- timestamp validation and maximum clock skew
- replay prevention and event idempotency
- key identifiers and rotation
- certificate or asymmetric verification where required
- safe storage of accepted event identifiers
- redaction of payload fields in logs

The generic verification endpoint is a foundation. It is not evidence of
provider certification by itself.

## Failure handling

- Missing runtime secret: verification unavailable, HTTP 503.
- Invalid request shape: HTTP 400.
- Signature mismatch: HTTP 401.
- Provider mismatch between path and body: HTTP 400.

Do not reveal expected signatures, secret values, or raw credentials in any
failure response.