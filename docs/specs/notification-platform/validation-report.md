# Validation Report — AFW-DLV-0012.1

Build command:
- dotnet build backend/src/NotificationPlatform/Notification.Api/Notification.Api.csproj -c Release

Scenario command:
- dotnet run --project backend/tests/Notification.Scenarios/Notification.Scenarios.csproj

Expected result:
- email notification PASS
- sms notification PASS
- push notification PASS
- in-app notification PASS
- template rendering PASS
- localized template PASS
- user preferences PASS
- retry delivery PASS
- delivery tracking PASS
- audit generation PASS
- telemetry generation PASS
