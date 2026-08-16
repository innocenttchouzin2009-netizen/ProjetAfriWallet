# Provider Boundary

This delivery uses only synthetic sandbox sanctions and PEP lists.

## Rules

- The application depends on provider-neutral interfaces.
- Every active source must declare `Sandbox: true`.
- No licensed or production regulatory dataset is included.
- No production provider credential is stored in the repository.
- Live provider integration requires a separate controlled release.