// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get wallets => 'Wallets';

  @override
  String get createWallet => 'Create wallet';

  @override
  String get fxQuote => 'FX Quote';

  @override
  String get sourceAmount => 'Source amount';

  @override
  String get amountReceived => 'Amount received';

  @override
  String get marketRate => 'Market rate';

  @override
  String get appliedRate => 'Applied rate';

  @override
  String get spread => 'Spread';

  @override
  String get fees => 'Fees';

  @override
  String get countdown => 'Countdown';

  @override
  String get expiresAt => 'Expires at';

  @override
  String get fromCurrency => 'From currency';

  @override
  String get toCurrency => 'To currency';

  @override
  String get getQuote => 'Get quote';

  @override
  String get quoteSummary => 'Quote summary';

  @override
  String get requiredField => 'This field is required';
}
