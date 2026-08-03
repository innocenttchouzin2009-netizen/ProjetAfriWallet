from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
required = [
    "README.md",
    "apps/mobile_app/pubspec.yaml",
    "apps/mobile_app/lib/main.dart",
    "packages/afw_design_system/pubspec.yaml",
    ".github/workflows/flutter-quality.yml",
]

missing = [item for item in required if not (root / item).exists()]
if missing:
    print("Missing required files:")
    for item in missing:
        print(f" - {item}")
    sys.exit(1)

print("AfriWallet Lot 1 structure validation passed.")
