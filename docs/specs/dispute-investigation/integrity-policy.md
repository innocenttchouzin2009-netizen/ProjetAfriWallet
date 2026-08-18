# Evidence Integrity Policy - Sandbox

Every evidence item records:

- `Sha256` - normalized to lower-case, trimmed hash of the evidence content;
- `SizeBytes` - size of the evidence payload in bytes;
- `ContentType` - declared MIME type of the evidence payload.

Duplicate evidence is detected by a case-insensitive comparison of `Sha256` against all evidence already recorded on the same investigation, and is rejected with `Duplicate evidence hash rejected.`

This delivery does not perform any external storage, virus scanning, or content verification beyond the recorded integrity metadata.
