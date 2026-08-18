# Release Policy

## Scope

AFW-DLV-0018.8 packages AFW-DLV-0018.1 through AFW-DLV-0018.7 as `dispute-platform-v1.8.0-rc1`. It introduces no new dispute business capability.

## Gate order

1. Verify 7/7 frozen historical delivery tags (local/remote SHA parity, contained in `origin/main`).
2. Re-run the AFW-DLV-0018.7 readiness gate (8/8 checks).
3. Run the 18 RC checks (7 tag checks + 11 platform presence/boundary checks).
4. Generate the RC package (`release/dispute-platform/v1.8.0-rc1`), including `validation-report.json`, `delivery-tags.txt`, and `manifest.sha256`.
5. Verify the RC package structure and content.
6. Verify `git diff --check`.

## Tag discipline

The final tag `dispute-platform-v1.8.0-rc1` is created only after a real squash-merge SHA is retrieved from the merged PR, verified to be contained in `origin/main`, and after local/remote peeled tag parity is confirmed. No historical `sprint18-dlv-0018.x` tag may be moved, deleted, recreated, or force-pushed as part of this process.
