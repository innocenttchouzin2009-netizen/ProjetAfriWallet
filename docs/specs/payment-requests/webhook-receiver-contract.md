# Payment Request Webhook Receiver Contract

## Scope

This contract secures receiver-side verification for payment-request lifecycle webhooks.
It does not add a persistence model for webhook delivery attempts. Existing transactional
outbox delivery state remains the delivery source of truth.

## Required HTTP headers

| Header | Meaning |
| --- | --- |
| `X-AfWal-Webhook-Id` | Stable event UUID. A receiver must reject a duplicate id inside the replay window. |
| `X-AfWal-Webhook-Timestamp` | Unix timestamp in whole seconds, UTC. |
| `X-AfWal-Webhook-Key-Id` | Identifier of the signing key used for this delivery. |
| `X-AfWal-Webhook-Signature` | Lowercase or uppercase hexadecimal HMAC-SHA256 digest prefixed with `v1=`. |

The request body is signed as the exact received bytes. Receivers must not parse,
re-serialize, trim, normalize whitespace, or change character encoding before verification.

## Canonical signing input

Version 1 signs the following byte sequence:

```text
UTF8("v1." + keyId + "." + timestampUnixSeconds + "." + eventIdLowerD + ".")
+ exactRequestBodyBytes
```

The HMAC algorithm is SHA-256. Binary digests are compared with a constant-time primitive.
Expected signatures, secrets, and candidate signatures must never be returned in an error
response or written to logs.

## Timestamp rule

Default maximum accepted clock skew is five minutes in either direction. A receiver rejects
a message before replay registration when the timestamp is older or further in the future
than the configured skew.

The replay retention window must be at least twice the maximum clock skew. The default is
ten minutes.

## Replay rule

After timestamp, key, and signature verification succeed, the receiver atomically registers
`X-AfWal-Webhook-Id` in its replay guard. A second use of the same event id inside the
retention window is rejected.

Commit 7 provides a bounded process-local replay guard and an abstraction for replacement by
a distributed implementation. Process-local replay state is not sufficient for a horizontally
scaled production receiver; production deployment must supply a shared atomic replay store.

Replay state is security state and is distinct from payment-request outbox delivery-attempt
persistence.

## Secret rotation

Each secret has:

- a unique `keyId`;
- a UTC `NotBeforeUtc`;
- an optional UTC `NotAfterUtc`.

Only the current active key may sign new deliveries. During a rotation overlap, a previous
key may remain verification-only until its `NotAfterUtc`. The receiver performs exact
`keyId` lookup and never tries every configured secret for an unknown key id.

Recommended rotation sequence:

1. provision the new secret and key id to receivers;
2. mark the new key current for signing;
3. keep the previous key verification-only for at least the maximum delivery latency plus
   clock-skew/replay allowance;
4. stop accepting the previous key at its explicit `NotAfterUtc`;
5. remove the previous secret after the overlap and incident rollback window.

Secret values must come from an approved runtime secret source and must never be committed.

## Receiver result classes

- `WEBHOOK_VERIFIED`
- `WEBHOOK_INVALID_REQUEST`
- `WEBHOOK_TIMESTAMP_OUTSIDE_TOLERANCE`
- `WEBHOOK_KEY_UNAVAILABLE`
- `WEBHOOK_INVALID_SIGNATURE`
- `WEBHOOK_REPLAY_DETECTED`

These codes intentionally do not reveal cryptographic material.

## Commit 7 boundary

Included:

- canonical receiver contract;
- HMAC-SHA256 signer/verifier;
- constant-time digest comparison;
- timestamp validation;
- bounded replay protection abstraction and process-local implementation;
- explicit key-id rotation rules and overlap verification.

Not included:

- a new database table for delivery attempts;
- webhook business endpoint processing;
- provider-specific canonicalization;
- distributed replay persistence;
- delivery-attempt persistence beyond the existing request-event outbox.
