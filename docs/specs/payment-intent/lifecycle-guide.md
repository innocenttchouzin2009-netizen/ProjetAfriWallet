# Lifecycle Guide

## États
- Created
- Authorized
- Processing
- Completed
- Failed
- Cancelled
- Expired

## Règles
- La transition Created → Authorized est obligatoire avant le traitement.
- La transition Authorized → Processing suit l’autorisation.
- La transition Processing → Completed clôture la demande.
- Les états finaux ne peuvent plus être modifiés.
