# Payment Platform CI/CD Guide

## Pull-request gate

The repository CI workflow must:

1. check out the pull-request SHA
2. install .NET SDK 10
3. run `validate-payment-platform.ps1 -Configuration Release`
4. fail on any nonzero validation result

The validation script builds and tests the integrated payment platform. It does
not require external provider connectivity or production secrets.

## Merge policy

- CI must be complete and successful on the current head SHA.
- The pull request must target `main` and be mergeable.
- Use Squash and Merge.
- Retrieve `mergeCommit.oid` from GitHub after merge.
- Create `sprint14-dlv-0014.7` only on that exact SHA.
- Verify local and remote peeled tag parity before freezing the delivery.

## Supply-chain controls

The gate scans all six payment APIs and the readiness project for known vulnerable
NuGet packages. Package sources and runner images remain governed by repository
and organization policy.