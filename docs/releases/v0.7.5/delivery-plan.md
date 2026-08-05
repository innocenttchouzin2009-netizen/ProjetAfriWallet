# Delivery Plan — AFW-DLV-0003.8

## Phase 1 — Foundations
1. Introduire la structure Flutter i18n/l10n.
2. Ajouter les fichiers ARB de base pour les langues core.
3. Configurer l’AppLocalizations et les widgets localisés.

## Phase 2 — UX and settings
4. Ajouter l’écran Paramètres → Langue.
5. Persister la langue préférée côté application.
6. Préparer le mapping vers le backend.

## Phase 3 — Backend integration
7. Ajouter les champs PreferredLanguage, Timezone, Currency et Country au profil utilisateur.
8. Exposer les préférences via l’API utilisateur.
9. Définir la logique de devise automatique selon le pays.

## Phase 4 — Enterprise hardening
10. Ajouter des tests de localisation.
11. Valider l’expérience sur les principales langues.
12. Packaging de la livraison avec documentation et validation.
