# Document Policy

## Supported document types

- BusinessRegistration
- TaxRegistration
- ProofOfAddress
- OwnerIdentity
- BankAccountProof
- ArticlesOfAssociation
- Other

## Integrity

Every document records its SHA-256 hash, size, and content type. Duplicate hashes within the same verification case are rejected with `Duplicate verification document hash rejected.`

## Status

Documents start `Submitted`. This delivery does not perform automated per-document acceptance/rejection; document-level status transitions are reserved for a future delivery.
