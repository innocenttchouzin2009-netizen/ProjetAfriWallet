# AFW-DLV-0018.8

## Dispute Platform Release Candidate v1.8.0-rc1

### Objective

Package and validate the complete Sprint 18 Dispute Platform.

### Covered deliveries

AFW-DLV-0018.1 through AFW-DLV-0018.7.

### Required gates

- seven frozen delivery tags verified
- local/remote SHA parity
- tagged commits contained in origin/main
- AFW-DLV-0018.7 readiness PASS
- 18/18 RC checks PASS
- RC package generated
- SHA-256 manifest generated
- package verification PASS
- git diff --check PASS

### No new business engine

AFW-DLV-0018.8 introduces no new dispute business logic.

### Financial boundary

The release candidate does not:

- execute real refunds
- submit real chargebacks
- move customer funds
- move merchant funds
- execute real settlement
- directly mutate Universal Ledger

### Final tag

`dispute-platform-v1.8.0-rc1`

The tag must only be created after:

1. local RC validation PASS
2. all required PR checks SUCCESS
3. squash merge
4. authoritative mergeCommit.oid retrieval
5. verification that the SHA belongs to origin/main
6. tag creation on that exact SHA
7. local peeled SHA parity
8. single tag push
9. remote peeled SHA parity
