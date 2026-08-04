# AfriWallet Architecture Overview

## 1. Purpose
AfriWallet is a multi-wallet, ledger-first payment platform designed for wallet-to-wallet transfers, settlement confirmation, and future adapter-based payment rails.

## 2. Core Principles
- Ledger is the source of truth for financial state.
- Balance projections are derived and rebuilt from ledger activity.
- Payment intents represent business intent before execution.
- Transfers are atomic accounting events.
- Settlements confirm an already-executed transfer without creating new ledger entries for internal transfers.

## 3. System Components
### Identity Service
- Lightweight identity and onboarding endpoint.
- Current MVP implementation returns a placeholder registration response.

### Universal Wallet API
- Hosts wallet management, payment intents, validation, authorization, transfers, settlement, and read models.
- Uses in-memory repositories for MVP development.

### Universal Ledger
- Stores ledger transactions and wallet entries.
- Provides projection support for balance reconstruction.

### Balance Engine
- Rebuilds wallet balance projections from ledger data.
- Powers wallet read models and payment validation.

## 4. Runtime Flow
1. Create account / identity placeholder.
2. Create wallet.
3. Create payment intent.
4. Validate the intent.
5. Authorize funds.
6. Execute transfer.
7. Settle the transfer.
8. Query wallet timeline and receipt payload.

## 5. API Surface
- Wallets: /api/v1/wallets
- Payment intents: /api/v1/payments/intents
- Validation: /api/v1/payments/intents/{id}/validate
- Authorization: /api/v1/payments/intents/{id}/authorize
- Execution: /api/v1/payments/intents/{id}/execute
- Transfers: /api/v1/payments/transfers
- Settlements: /api/v1/payments/settlements

## 6. Deployment Model
- The API is containerized with Docker for staging deployment.
- The current implementation uses in-memory state and is intended for demo and validation environments.

## 7. Release Candidate Notes
- Tag: v0.5.0-rc1
- Focus: end-to-end demo readiness, architecture documentation, and staging deployment.
