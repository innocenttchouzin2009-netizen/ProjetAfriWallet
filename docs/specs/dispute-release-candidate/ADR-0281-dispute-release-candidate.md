# ADR-0281

## Dispute Platform Release Candidate

### Status

Accepted for AFW-DLV-0018.8.

### Decision

Sprint 18 is packaged as: Dispute Platform v1.8.0-rc1

### Required historical tags

- sprint18-dlv-0018.1
- sprint18-dlv-0018.2
- sprint18-dlv-0018.3
- sprint18-dlv-0018.4
- sprint18-dlv-0018.5
- sprint18-dlv-0018.6
- sprint18-dlv-0018.7

Each must:

- exist locally
- exist remotely
- dereference to the same commit
- reference a commit contained in origin/main

### Final tag

`dispute-platform-v1.8.0-rc1` must reference the exact squash SHA produced by the AFW-DLV-0018.8 PR.

### Immutability

No historical tag may be modified to construct the release candidate.
