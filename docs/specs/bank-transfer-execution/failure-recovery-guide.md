# Failure Recovery Guide

Failures are recorded as execution-level `FailureCode` values and transition the execution into the failed state without mutating a completed execution.

Recovery is operational and should be handled by an explicit retry workflow rather than implicit re-execution.
