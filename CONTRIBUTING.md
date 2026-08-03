# Contributing to AfriWallet

## Branches

- `main`: releasable code only
- `develop`: integration branch when enabled
- `feature/<scope>`: new work
- `fix/<scope>`: defect correction
- `docs/<scope>`: documentation only

## Commit convention

Use Conventional Commits:

```text
feat(onboarding): add language selection
fix(router): prevent duplicate navigation
docs(adr): record localization strategy
test(theme): cover dark theme tokens
```

## Pull request definition of done

A pull request must:

1. Build successfully.
2. Pass `flutter analyze`.
3. Pass automated tests.
4. Include or update documentation.
5. Avoid secrets and personal data.
6. Use AfriWallet design tokens.
7. Provide screenshots for visible UI changes.
