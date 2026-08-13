# Provider Resilience Guide

## Retry policy

The retry policy performs one initial attempt plus at most `maxRetries` retries.
It retries only when the normalized result is unsuccessful and retryable.
Executor exceptions are converted to a retryable generic result; cancellation is
never converted or retried.

Backoff delays are bounded at two seconds:

```text
retry 1: 200 ms
retry 2: 400 ms
retry 3: 800 ms
retry 4+: capped at 2000 ms
```

Production connectors must still honor provider rate-limit headers and published
retry guidance.

## Circuit breaker

Each provider code has an independent, thread-safe circuit state. The default
circuit opens after five failed integration requests and remains open for 30
seconds. A successful request resets the failure count.

The current breaker is process-local. Multi-instance deployments require an
operational strategy that tolerates independent instance state or introduces an
approved distributed implementation.

## Provider health

Health records completed integration requests and reports success rate and
average end-to-end execution latency. A provider is available when no calls have
been observed or its observed success rate is at least 50 percent.

Health data is an operational signal, not a financial source of truth.