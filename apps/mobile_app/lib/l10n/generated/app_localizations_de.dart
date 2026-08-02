// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for German (`de`).
class AppLocalizationsDe extends AppLocalizations {
  AppLocalizationsDe([String locale = 'de']) : super(locale);

  @override
  String get appName => 'AfriWallet';

  @override
  String get tagline => 'Connecting Africa. Empowering People.';

  @override
  String get welcomeTitle => 'Willkommen bei AfriWallet';

  @override
  String get welcomeSubtitle =>
      'Ihre digitale Finanzidentität, Wallet und Zahlungen in einer sicheren Anwendung.';

  @override
  String get createAccount => 'Konto erstellen';

  @override
  String get signIn => 'Anmelden';

  @override
  String get chooseLanguage => 'Sprache auswählen';

  @override
  String get financialDesk => 'Financial Desk';

  @override
  String get availableBalance => 'Verfügbares Guthaben';

  @override
  String get send => 'Senden';

  @override
  String get receive => 'Empfangen';

  @override
  String get request => 'Anfordern';

  @override
  String get scan => 'Scannen';
}
