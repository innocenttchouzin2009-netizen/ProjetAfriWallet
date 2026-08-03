# Mobile Architecture Overview

## Scope

This lot establishes the mobile client foundation only. Financial services,
ledger, payment orchestration, KYC, and partner integrations are not implemented.

## Structure

The app uses:

- feature-first source organization;
- clear presentation, domain, and data boundaries as features grow;
- GoRouter for declarative navigation;
- Riverpod as the state and dependency boundary;
- `gen_l10n` for FR / EN / DE localization;
- a local package for design-system reuse.

## Dependency direction

```text
Presentation → Application/Domain interfaces → Data implementations
```

UI code must not directly call partner APIs or persist financial data.
