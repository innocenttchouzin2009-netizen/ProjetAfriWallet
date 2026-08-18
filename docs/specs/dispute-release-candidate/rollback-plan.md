# Dispute Platform v1.8.0-rc1 Rollback Plan

## Objective

Provide a controlled rollback path for the release candidate.

## Important constraint

A release tag is immutable. Rollback does not move or rewrite: `dispute-platform-v1.8.0-rc1`

## Application rollback

Rollback should deploy the previously approved application artifact.

## Database

No destructive schema rollback is performed automatically. Any future schema rollback requires an independently reviewed migration.

## Dispute state

Historical:

- claims
- investigations
- decisions
- orchestration records
- intelligence findings

must not be deleted to simulate rollback.

## Provider boundary

AFW-DLV-0018.5 uses sandbox providers only. No real financial settlement reversal is part of this RC rollback.

## Tag policy

Historical sprint tags must never be:

- moved
- deleted
- recreated
- force-pushed
