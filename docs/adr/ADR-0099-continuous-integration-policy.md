# ADR-0099: Continuous Integration Policy

- Status: Accepted
- Date: 2026-08-04

## Context
AfriWallet needs a repeatable gate for every change so build, tests, security, and release checks are executed automatically before merge or release.

## Decision
We will use GitHub Actions workflows for build, tests, security, performance, packaging, Docker, release, and deployment. The workflows will be triggered for pull requests and selected branches and will provide a shared baseline for quality and delivery.

## Consequences
The repository gains a consistent pipeline entry point, though the current implementation uses script-based placeholders to keep the initial rollout lightweight and adaptable.
