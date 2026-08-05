# PRD — AFW-DLV-0007.4

## Titre
Sprint 7.4 — Bank Transfer Engine

## Objectif
Construire un moteur de transferts bancaires enterprise, avec une architecture de Payment Gateway Layer commune capable de supporter Mobile Money, Banking et Cards sur un socle technique réutilisable.

## Objectifs fonctionnels
- Introduire un Payment Gateway Layer abstrait.
- Implémenter la registry des banques et le routage.
- Supporter les comptes IBAN et comptes locaux.
- Gérer les intents de transfert et la validation d'autorisation.
- Ajouter des connecteurs domestiques et SEPA.
- Produire un timeline de transfert et des reçus.
- Appliquer les standards enterprise de production readiness.

## Livraisons proposées
- AFW-DLV-0007.4.1 — Bank Registry & Routing
- AFW-DLV-0007.4.2 — Bank Account Engine (IBAN / comptes locaux)
- AFW-DLV-0007.4.3 — Transfer Intent Engine
- AFW-DLV-0007.4.4 — Transfer Validation & Authorization
- AFW-DLV-0007.4.5 — Domestic Transfer Connector
- AFW-DLV-0007.4.6 — SEPA Connector
- AFW-DLV-0007.4.7 — Transfer Timeline & Receipts
- AFW-DLV-0007.4.8 — Production Readiness
- AFW-DLV-0007.4.9 — Release Candidate

## Architecture cible
- Payment Gateway
  - Mobile Money: MTN, Orange, Moov, Airtel
  - Banking: SEPA, Local Banks, SWIFT
  - Cards: Visa, Mastercard, Virtual Cards

## Exigences enterprise
- Audit trail
- Resilience et retry
- Observabilité et traces
- Feature flags
- Secrets management
- Validation au démarrage
- Packaging et validation RC
