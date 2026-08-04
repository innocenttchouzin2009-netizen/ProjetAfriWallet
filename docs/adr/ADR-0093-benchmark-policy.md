# ADR-0093: Benchmark Policy

## Status
Accepted

## Context
The platform needs repeatable benchmarks for Wallet, Ledger, Payment, and Fraud flows.

## Decision
Benchmarks are executed as automated scenario runs with explicit requests, success counts, latency percentiles, and throughput values to track regressions.

## Consequences
The release process can compare benchmark results over time rather than relying on ad-hoc measurements.
