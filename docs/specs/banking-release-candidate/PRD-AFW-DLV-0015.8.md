# AFW-DLV-0015.8 — Banking Platform Release Candidate v1.5.0-rc1

## Objective
Freeze and validate the AfriWallet Banking Platform as a Release Candidate.

## Included platform
AFW-DLV-0015.1 through AFW-DLV-0015.7.

## Responsibilities
- complete Banking Platform gate
- release package generation
- evidence collection
- OpenAPI consolidation
- operational documentation
- manifest generation
- SHA-256 checksums
- rollback package
- RC validation
- immutable release evidence

## No new business functionality
AFW-DLV-0015.8 MUST NOT introduce new banking business capabilities.

## Production boundary
The Banking Platform remains sandbox-only.
This RC does not authorize:
- real SEPA traffic
- real SWIFT traffic
- production bank credentials
- production bank API connectivity
- production settlement with an external bank
- certification claims

## Release decision
The RC may be tagged only after:
- CI passes
- PR is squash-merged
- exact squash SHA is retrieved
- final RC tag points exactly to that SHA
- local and remote SHA parity is verified
