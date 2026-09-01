# AFW-MOB-BETA1.2 Review Checklist

Before merge:
1. Required GitHub Actions checks are SUCCESS.
2. Flutter analyze passes.
3. Flutter tests pass.
4. `git diff --check` passes.
5. Scope contains no production financial execution.
6. Squash merge only.
7. Retrieve actual squash SHA after merge.
8. Verify SHA is in `origin/main`.
9. Create annotated tag only on that SHA.
10. Verify local and remote peeled SHA parity.

Until all ten conditions hold: `DELIVERY FROZEN: NO`.
