# Evidence

This directory documents where release evidence originates. All evidence is reproducible from the repository:

- Frozen delivery tag parity: `tools/release/verify-dispute-rc-frozen-deliveries.ps1`
- AFW-DLV-0018.7 readiness gate: `tools/release/validate-dispute-readiness.ps1`
- RC checks and package generation: `backend/tests/DisputeReleaseCandidate.Scenarios`

`validation-report.json` and `delivery-tags.txt` in the parent release directory are generated directly by the RC scenario runner and are not hand-authored.
