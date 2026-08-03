#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP="$ROOT/apps/mobile_app"

if ! command -v flutter >/dev/null 2>&1; then
  echo "Flutter is not installed or not available in PATH."
  exit 1
fi

if [[ ! -d "$APP/android" || ! -d "$APP/ios" ]]; then
  TMP="$(mktemp -d)"
  flutter create \
    --project-name afriwallet_mobile \
    --org com.afriwallet \
    --platforms android,ios,web \
    "$TMP/mobile_app"

  cp -R "$TMP/mobile_app/android" "$APP/"
  cp -R "$TMP/mobile_app/ios" "$APP/"
  cp -R "$TMP/mobile_app/web" "$APP/"
  rm -rf "$TMP"
fi

cd "$APP"
flutter pub get
flutter gen-l10n
dart format lib test
flutter analyze
flutter test

echo "AfriWallet Flutter foundation is ready."
