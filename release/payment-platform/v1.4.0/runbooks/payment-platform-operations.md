# Payment Platform Operations

Run the committed validation script before promotion. Verify all API health
endpoints, provider health, audit and telemetry flow, settlement observations,
package checksums, and rollback readiness. Use immutable artifact digests and
never inject secret values into source or release files.

See `docs/specs/payment-production-readiness/operations-runbook.md` for the full
procedure.