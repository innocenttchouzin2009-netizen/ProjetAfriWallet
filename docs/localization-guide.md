# Localization Guide

## Supported locales
- English (`en`)
- French (`fr`)
- German (`de`)
- Kiswahili (`sw`)

## Current implementation notes
- The Flutter app persists a selected language through the locale repository and controller.
- Unsupported values are normalized to English before being stored.
- The backend profile language endpoint returns a language, culture, and timezone tuple.

## Adding a new language
1. Create a new ARB file in the Flutter localization folder.
2. Add the locale to the supported locales list.
3. Provide translations for the required keys.
4. Update the language switcher and tests.

## Validation checklist
- Run `flutter test` for the mobile app.
- Verify the profile language endpoint returns a supported culture for each language.
