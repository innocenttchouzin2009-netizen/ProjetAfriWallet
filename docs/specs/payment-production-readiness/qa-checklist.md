# AFW-DLV-0014.7 QA Checklist

## Integrated deliveries

- [x] AFW-DLV-0014.1 is present on main.
- [x] AFW-DLV-0014.2 is present on main with dependency remediation.
- [x] AFW-DLV-0014.3 is present on main.
- [x] AFW-DLV-0014.4 is present on main.
- [x] AFW-DLV-0014.5 is present on main.
- [x] AFW-DLV-0014.6 is present on main.

## Automated gate

- [x] All six scenario executables pass in Release configuration.
- [x] All six APIs and readiness executable build with zero errors.
- [x] Exactly 22 readiness checks pass.
- [x] No readiness check is skipped.
- [x] Secret scan passes.
- [x] All seven dependency graphs are free of known vulnerabilities.
- [x] Release package manifest and checksums verify.
- [ ] Pull-request CI is green on the merge candidate SHA.

## Release governance

- [ ] Squash merge SHA is retrieved from GitHub.
- [ ] `sprint14-dlv-0014.7` peels to the exact squash SHA.
- [ ] Remote tag parity is verified before delivery freeze.