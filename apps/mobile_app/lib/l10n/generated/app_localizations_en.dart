// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'AfriWallet';

  @override
  String get tagline => 'Connecting Africa. Empowering People.';

  @override
  String get welcomeTitle => 'Welcome to AfriWallet';

  @override
  String get welcomeSubtitle =>
      'Your digital financial identity, wallet and payments in one secure experience.';

  @override
  String get createAccount => 'Create an account';

  @override
  String get signIn => 'Sign in';

  @override
  String get chooseLanguage => 'Choose language';

  @override
  String get financialDesk => 'Financial Desk';

  @override
  String get availableBalance => 'Available balance';

  @override
  String get send => 'Send';

  @override
  String get receive => 'Receive';

  @override
  String get request => 'Request';

  @override
  String get scan => 'Scan';
}
