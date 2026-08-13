# Payment Platform Release Evidence

Ephemeral command logs are generated under the ignored
`build/payment-readiness-evidence` directory. The committed release evidence is
the deterministic validation summary, package manifest, checksums, release notes,
configuration matrix, dashboard specification, runbook, and rollback plan.

The package is valid only when the independent verifier confirms every listed
digest and the readiness summary reports 22 of 22 checks passed.