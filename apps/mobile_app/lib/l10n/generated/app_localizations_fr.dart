// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for French (`fr`).
class AppLocalizationsFr extends AppLocalizations {
  AppLocalizationsFr([String locale = 'fr']) : super(locale);

  @override
  String get appName => 'AfriWallet';

  @override
  String get tagline => 'Connecting Africa. Empowering People.';

  @override
  String get welcomeTitle => 'Bienvenue dans AfriWallet';

  @override
  String get welcomeSubtitle =>
      'Votre identité financière numérique, votre Wallet et vos paiements dans une expérience sécurisée.';

  @override
  String get createAccount => 'Créer un compte';

  @override
  String get signIn => 'Se connecter';

  @override
  String get chooseLanguage => 'Choisir la langue';

  @override
  String get financialDesk => 'Financial Desk';

  @override
  String get availableBalance => 'Solde disponible';

  @override
  String get send => 'Envoyer';

  @override
  String get receive => 'Recevoir';

  @override
  String get request => 'Demander';

  @override
  String get scan => 'Scanner';
}
