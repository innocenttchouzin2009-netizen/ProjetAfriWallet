# AFW-DLV-0007.3.3 — Release Notes

**Version:** v0.7.3.3  
**Sprint:** Sprint 7 — Mobile Money, Banking & Card Network

## Nouveautés

### Flutter MTN MoMo Experience

Cette livraison transforme les briques techniques MTN MoMo en une expérience utilisateur premium et cohérente avec la vision d'AfriWallet.

## Écrans inclus

- écran d’accueil MTN MoMo
- dépôt
- retrait
- validation du numéro et du montant
- écran de confirmation
- suivi du statut
- actualisation d’une transaction
- historique
- détail
- reçu

## Architecture Flutter

Le module introduit :

- repository API
- repository de démonstration
- client HTTP avec AFW_API_BASE_URL
- controllers Flutter
- routage
- badges de statut
- états chargement, erreur et historique vide

## Tests

- 3 tests widget
- couverture de la logique de repository et de navigation

## Documentation

- ADR-0116 et ADR-0117
- PRD
- Release Notes
- guide d’intégration

## Validation

Depuis apps/mobile_app :

```bash
flutter pub get
flutter analyze
flutter test
```

## Architecture Flutter

```text
features/
  mobile_money/
    mtn_momo/
      data/
      domain/
      presentation/
      widgets/
      repository/
      controllers/
      models/
      services/
      routes/
```

## Widgets premium

- Carte MTN MoMo
- Sélecteur de numéro favori
- Clavier numérique personnalisé
- Indicateur de progression
- Timeline de transaction
- Badge de statut
- Animation de succès
- Animation d'échec
- Skeleton loaders
- Pull-to-refresh

## Backend

Connexion aux endpoints sandbox déjà développés :

```http
POST /api/v1/mobile-money/mtn-momo/api/deposit
POST /api/v1/mobile-money/mtn-momo/api/withdraw
GET  /api/v1/mobile-money/mtn-momo/api/status/{reference}
```

## Tests

- Widget tests
- Repository tests
- Controller tests
- Navigation tests
- Flutter Analyze

## Documentation

- Release Notes
- PRD
- ADR
- Guide utilisateur
- Guide développeur

## Validation

```text
Flutter Analyze .......... PASS
Flutter Tests ............ PASS
```

## Impact

Cette livraison apporte une expérience MTN MoMo plus immersive, plus claire et plus alignée avec le positionnement premium d'AfriWallet.
