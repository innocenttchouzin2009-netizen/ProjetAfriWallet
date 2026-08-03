# AfriWallet

**Connecting Africa. Empowering People.**

Official engineering repository foundation for AfriWallet.

## Delivery

- Delivery ID: `AFW-DLV-0002`
- Lot: `Lot 1 — Flutter Enterprise Foundation`
- Version: `0.1.0-foundation`
- Status: Initial engineering baseline

## Included

- Monorepo-ready directory structure
- Flutter mobile application source overlay
- AfriWallet design-system package
- FR / EN / DE localization files
- Light and dark themes
- GoRouter navigation baseline
- Splash, Welcome, and Financial Desk placeholder screens
- GitHub Actions quality workflow
- Architecture Decision Records
- Contribution and security policies

## Bootstrap locally

Flutter is not bundled in this delivery.

```bash
git clone https://github.com/innocenttchouzin2009-netizen/ProjetAfriWallet.git
cd ProjetAfriWallet

# Copy the contents of this delivery into the repository, then:
bash tools/bootstrap_flutter.sh
cd apps/mobile_app
flutter pub get
flutter gen-l10n
flutter analyze
flutter test
flutter run
```

On Windows PowerShell:

```powershell
./tools/bootstrap_flutter.ps1
cd apps/mobile_app
flutter pub get
flutter gen-l10n
flutter analyze
flutter test
flutter run
```

## Repository model

```text
apps/mobile_app/                 Flutter client
packages/afw_design_system/      Shared UI foundations and components
docs/architecture/              Architecture documentation
docs/adr/                       Architecture Decision Records
docs/ux/                        UX source material
tools/                          Bootstrap and validation scripts
.github/                        CI and collaboration templates
```

## Branch and release

- Branch: `feature/flutter-foundation`
- Suggested tag after merge: `v0.1.0-foundation`
