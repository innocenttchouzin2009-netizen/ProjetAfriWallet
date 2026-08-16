# Provider boundary

This delivery keeps identity verification strictly sandboxed.

## Rules
- No production provider credentials are stored in the repository.
- No raw document blobs are persisted in the domain.
- Only provider references and normalized result codes may survive in the orchestration session.
- Any live provider integration requires a separate controlled release.
