# Payment Platform Security Guide

## Repository controls

- never commit provider credentials, API keys, OAuth secrets, passwords, tokens,
  private keys, or webhook signing material
- keep generated `bin` and `obj` directories excluded
- run the secret scanner and dependency scan on every readiness pull request
- verify package manifests and checksums before promotion

## Runtime controls

- resolve production secrets from an approved secret store
- isolate credentials by environment and provider
- require TLS for production provider endpoints
- verify provider-specific webhook signatures, timestamps, and replay controls
- redact payment identifiers and secret material from logs
- rotate credentials using an audited procedure

## Certification boundary

Generic sandbox adapters and the HMAC foundation do not constitute operator
certification. Each production connector requires provider-specific security
review, contractual approval, and acceptance evidence.