# Security boundary

Identity verification orchestration must not assume production trust.

## Boundaries
- sandbox providers only
- no local biometric training or recognition
- no raw identity material retention
- no production credentials in code
- webhook callbacks must be validated in a future controlled integration
