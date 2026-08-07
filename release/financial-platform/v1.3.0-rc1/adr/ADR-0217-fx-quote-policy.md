# ADR-0217 - Sandbox FX Quote Policy

The engine uses IFxQuoteProvider to resolve conversion rates.
For development and tests, SandboxFxQuoteProvider returns deterministic technical rates.

Important: sandbox FX rates are fictitious and for technical validation only. They are not market rates and must never be used for production pricing.
