# Payment Platform Readiness Guide

## Prerequisites

- Windows PowerShell 5.1 or PowerShell 7
- .NET SDK 10
- a clean checkout containing AFW-DLV-0014.1 through AFW-DLV-0014.6
- access to configured NuGet sources

No operator credentials are required because all connector implementations remain
sandbox adapters.

## Run

```powershell
.\validate-payment-platform.ps1 -Configuration Release
```

The command clears stale ephemeral evidence, executes every validation step,
generates the release reports, writes the package manifest and checksums, and runs
an independent package verification scenario.

## Expected decision

```text
Checks: 22
Passed: 22
Failed: 0
Skipped: 0

Decision: READY FOR PAYMENT RC
```

Anything else blocks review, merge, tagging, and delivery freeze.