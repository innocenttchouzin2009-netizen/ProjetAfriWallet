# CI Gate

Dedicated workflow: `.github/workflows/mobile-beta-onboarding-auth.yml`

Required validation:
- Flutter stable setup
- `flutter pub get`
- `flutter analyze`
- `flutter test`
- `git diff --check`

A green workflow is necessary but not sufficient for freeze. Freeze additionally requires squash merge, authoritative main SHA verification and annotated-tag peeled parity.
