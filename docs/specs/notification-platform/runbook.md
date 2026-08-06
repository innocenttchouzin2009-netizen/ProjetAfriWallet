# Runbook — AFW-DLV-0012.1

## Validation Commands
- dotnet build backend/src/NotificationPlatform/Notification.Api/Notification.Api.csproj -c Release
- dotnet run --project backend/tests/Notification.Scenarios/Notification.Scenarios.csproj

## Failure Triage
1. If template rendering fails, inspect locale fallback and token replacement.
2. If channel delivery fails, inspect delivery attempts and retry classification.
3. If preferences block expected delivery, review channel enablement and marketing opt-in.
