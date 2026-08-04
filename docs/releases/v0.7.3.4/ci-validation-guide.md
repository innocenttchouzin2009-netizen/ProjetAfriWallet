# CI Validation Guide

Run the validation gate from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File validate-mtn-momo-production.ps1 -Configuration Release
```

The script produces the release evidence bundle under release/mtn-momo/v0.7.3.4 and writes the final summary to validation-report.json and validation-report.md.
