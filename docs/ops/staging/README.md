# Staging environment

## Prerequisites
- Docker Desktop
- Docker Compose

## Start the stack
```bash
docker compose up --build -d
```

## Demo flow
1. Register an identity via the identity-service endpoint.
2. Create a wallet via the universal-wallet wallet endpoint.
3. Create a payment intent and execute the payment flow.
4. Confirm settlement and inspect the wallet timeline or receipt payload.

## Ports
- Universal Wallet API: http://localhost:5000
- Identity Service: http://localhost:5001
- Grafana: http://localhost:3000
- Prometheus: http://localhost:9090
