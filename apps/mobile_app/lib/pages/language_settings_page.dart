import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';

/// Page that lets the user choose the application language.
class LanguageSettingsPage extends StatelessWidget {
  const LanguageSettingsPage({super.key, required this.onLocaleChanged});

  final Future<void> Function(Locale) onLocaleChanged;

  static const _supportedLocales = [
    Locale('en'),
    Locale('fr'),
  ];

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;
    final labels = <String, String>{
      'en': localizations.english,
      'fr': localizations.french,
    };
    return Scaffold(
      appBar: AppBar(title: Text(localizations.selectLanguage)),
      body: ListView(
        children: _supportedLocales.map((locale) {
          final label = labels[locale.languageCode] ?? locale.languageCode;
          return ListTile(
            title: Text(label),
            onTap: () => onLocaleChanged(locale),
          );
        }).toList(),
      ),
    );
  }
}
