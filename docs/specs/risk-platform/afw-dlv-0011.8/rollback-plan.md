# Rollback Plan — AFW-DLV-0011.8

## Trigger Conditions
- Any failed release candidate check.
- Packaging mismatch or missing checksum.
- Secret detection in RC evidence.

## Steps
1. Stop promotion.
2. Revert to the previous stable main commit.
3. Rebuild and rerun the RC gate.
4. Publish corrected evidence only after all checks pass.
