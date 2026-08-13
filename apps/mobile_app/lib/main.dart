import 'package:flutter/material.dart';

import 'l10n/app_localizations.dart';
import 'localization/language_manager.dart';
import 'localization/locale_controller.dart';
import 'pages/language_settings_page.dart';
import 'pages/subscriptions_page.dart';
import 'services/subscription_repository.dart';

void main() {
  runApp(const AfriWalletApp());
}

class AfriWalletApp extends StatefulWidget {
  const AfriWalletApp({super.key, this.repository});

  final SubscriptionRepository? repository;

  @override
  State<AfriWalletApp> createState() => _AfriWalletAppState();
}

class _AfriWalletAppState extends State<AfriWalletApp> {
  Locale _locale = const Locale('en');
  bool _isLocaleLoaded = false;
  LocaleController? _localeController;

  @override
  void initState() {
    super.initState();
    _loadSavedLocale();
  }

  Future<void> _loadSavedLocale() async {
    final controller = await LanguageManager.create();
    if (!mounted) return;
    setState(() {
      _localeController = controller;
      _locale = controller.currentLocale;
      _isLocaleLoaded = true;
    });
  }

  Future<void> _handleLocaleChanged(Locale locale) async {
    await _localeController?.save(locale.languageCode);
    if (!mounted) return;
    setState(() => _locale = locale);
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AfriWallet',
      locale: _locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.indigo),
      home: _isLocaleLoaded
          ? SubscriptionsPage(repository: widget.repository, locale: _locale, onOpenSettings: () {
              Navigator.of(context).push(MaterialPageRoute<void>(
                builder: (context) => LanguageSettingsPage(onLocaleChanged: (locale) async {
                  await _handleLocaleChanged(locale);
                  if (!context.mounted) return;
                  Navigator.of(context).pop();
                }),
              ));
            })
          : const Scaffold(body: Center(child: CircularProgressIndicator())),
    );
  }
}
