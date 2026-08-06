# PRD — AFW-DLV-0012.1

## Summary
Build a centralized Notification & Messaging Platform for AfriWallet with multi-channel delivery, user preferences, localized templates, retry support, auditability, and safe telemetry.

## Goals
- Support EMAIL, SMS, PUSH, IN_APP, and WEBHOOK channels.
- Evaluate user preferences before each non-mandatory delivery.
- Provide versioned and localized templates with parameter rendering.
- Record delivery attempts, retries, audit events, and aggregate telemetry.

## Scope
- Notification platform core domain and orchestration services.
- Minimal API for notifications, preferences, and templates.
- Scenario validation for channel delivery, templates, preferences, retry, audit, and telemetry.

## Validation Targets
- dotnet build backend/src/NotificationPlatform/Notification.Api/Notification.Api.csproj -c Release
- dotnet run --project backend/tests/Notification.Scenarios/Notification.Scenarios.csproj
