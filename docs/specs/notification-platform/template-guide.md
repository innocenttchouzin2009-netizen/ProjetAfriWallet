# Template Guide — AFW-DLV-0012.1

## Principles
- Templates are versioned, localized, and parameterized.
- Locales fall back to English when the requested locale is unavailable.
- Tokens use `{{token}}` syntax.

## Initial Template Example
- Key: `PAYMENT_RECEIVED`
- Locales: `fr`, `en`, `de`, `sw`

## Publication
- Create the template first.
- Publish explicitly before runtime use.
