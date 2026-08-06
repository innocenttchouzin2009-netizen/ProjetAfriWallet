# Provider Integration Guide — AFW-DLV-0012.1

## Current Channels
- EMAIL
- SMS
- PUSH
- IN_APP
- WEBHOOK

## Extension Model
Additional providers and channels should plug into dispatching logic without changing external API contracts. Candidate future channels include WhatsApp Business, RCS, Apple Business Chat, Google Business Messages, Telegram, and Signal.

## Safety Rules
- Do not expose email addresses, phone numbers, or personal identifiers in metrics labels.
- Retries should be applied only to transient delivery failures.
