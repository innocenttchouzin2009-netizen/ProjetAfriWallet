$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$App = Join-Path $Root "apps/mobile_app"

if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) {
  throw "Flutter is not installed or is not available in PATH."
}

if (-not (Test-Path (Join-Path $App "android")) -or
    -not (Test-Path (Join-Path $App "ios"))) {
  $Temp = Join-Path $env:TEMP ("afriwallet-" + [guid]::NewGuid())
  flutter create --project-name afriwallet_mobile --org com.afriwallet --platforms android,ios,web (Join-Path $Temp "mobile_app")

  Copy-Item (Join-Path $Temp "mobile_app/android") $App -Recurse
  Copy-Item (Join-Path $Temp "mobile_app/ios") $App -Recurse
  Copy-Item (Join-Path $Temp "mobile_app/web") $App -Recurse
  Remove-Item $Temp -Recurse -Force
}

Push-Location $App
flutter pub get
flutter gen-l10n
dart format lib test
flutter analyze
flutter test
Pop-Location

Write-Host "AfriWallet Flutter foundation is ready."
