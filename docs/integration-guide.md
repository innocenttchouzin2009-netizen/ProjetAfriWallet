# Merchant Registry Integration Guide

## Overview
This guide describes how downstream services and clients can integrate with the Merchant Registry.

## API entry points
- Merchant collection and detail endpoints under /api/v1/merchants
- Lifecycle transitions under /api/v1/merchants/{merchantId}/activate, /suspend, and /close
- QR payment and settlement scaffolding endpoints under /api/v1/qr-payments and /api/v1/settlements

## Integration notes
- Use the merchantId returned from creation to correlate future operations.
- Preserve the merchant status and version fields when updating regulatory or operational data.
- Treat QR and settlement endpoints as scaffolded integration points for the next implementation wave.
