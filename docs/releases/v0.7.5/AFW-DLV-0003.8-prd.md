# PRD — AFW-DLV-0003.8

## Titre
Internationalization (i18n) for AfriWallet

## Objectif
Créer un module de localisation complet et évolutif pour supporter une expérience panafricaine dès la version 1.0.

## Scope initial
- Supporter officiellement les langues suivantes : English, Français, Deutsch, Español, Português, Italiano, Nederlands, العربية, 中文, 日本語, 한국어, Русский, Kiswahili.
- Introduire le Kiswahili comme langue core dès cette livraison.
- Ajouter une structure Flutter compatible avec l18n et l10n.
- Préparer la persistance backend de la langue préférée, du fuseau horaire, du pays et de la devise.

## Scope étendu recommandé
- Ajouter progressivement Amharique, Haoussa, Yoruba, Igbo, Zoulou, Xhosa.

## Exigences fonctionnelles
- Sélection de la langue depuis Paramètres → Langue.
- Mise à jour automatique de la devise selon le pays.
- Stockage des préférences utilisateur côté backend.
- Support des chaînes d’interface et des messages d’erreur localisés.

## Architecture Flutter
- lib/l10n/app_en.arb
- lib/l10n/app_fr.arb
- lib/l10n/app_de.arb
- lib/l10n/app_es.arb
- lib/l10n/app_pt.arb
- lib/l10n/app_it.arb
- lib/l10n/app_nl.arb
- lib/l10n/app_ar.arb
- lib/l10n/app_zh.arb
- lib/l10n/app_ja.arb
- lib/l10n/app_ko.arb
- lib/l10n/app_ru.arb
- lib/l10n/app_sw.arb
- lib/generated/

## Backend
- UserProfile.PreferredLanguage
- UserProfile.Timezone
- UserProfile.Currency
- UserProfile.Country

## Exemple de payload
{
  "preferredLanguage": "sw",
  "country": "TZ",
  "currency": "TZS"
}
