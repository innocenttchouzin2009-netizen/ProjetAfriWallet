import 'package:flutter/material.dart';

import 'l10n/app_localizations.dart';
import 'localization/language_manager.dart';
import 'localization/locale_controller.dart';
import 'pages/beta_welcome_page.dart';
import 'pages/language_settings_page.dart';
import 'pages/onboarding_auth_page.dart';
import 'pages/subscriptions_page.dart';
import 'services/subscription_repository.dart';
import 'theme/afwal_theme.dart';

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
  bool _hasEnteredBeta = false;
  bool _hasCompletedOnboarding = false;
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

  Widget _buildCurrentExperience() {
    if (!_isLocaleLoaded) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    if (!_hasEnteredBeta) {
      return BetaWelcomePage(onContinue: () => setState(() => _hasEnteredBeta = true));
    }

    if (!_hasCompletedOnboarding) {
      return OnboardingAuthPage(
        onContinueToBeta: () => setState(() => _hasCompletedOnboarding = true),
      );
    }

    return SubscriptionsPage(
      repository: widget.repository,
      locale: _locale,
      onOpenSettings: () {
        Navigator.of(context).push(MaterialPageRoute<void>(
          builder: (context) => LanguageSettingsPage(
            onLocaleChanged: (locale) async {
              await _handleLocaleChanged(locale);
              if (!context.mounted) return;
              Navigator.of(context).pop();
            },
          ),
        ));
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AfWal',
      debugShowCheckedModeBanner: false,
      locale: _locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      theme: AfWalTheme.light(),
      home: Builder(builder: (_) => _buildCurrentExperience()),
    );
  }
}
