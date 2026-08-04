// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for French (`fr`).
class AppLocalizationsFr extends AppLocalizations {
  AppLocalizationsFr([String locale = 'fr']) : super(locale);

  @override
  String get wallets => 'Portefeuilles';

  @override
  String get createWallet => 'Créer un portefeuille';

  @override
  String get fxQuote => 'Citation FX';

  @override
  String get sourceAmount => 'Montant source';

  @override
  String get amountReceived => 'Montant reçu';

  @override
  String get marketRate => 'Taux de marché';

  @override
  String get appliedRate => 'Taux appliqué';

  @override
  String get spread => 'Spread';

  @override
  String get fees => 'Frais';

  @override
  String get countdown => 'Compte à rebours';

  @override
  String get expiresAt => 'Expire à';

  @override
  String get fromCurrency => 'Devise d\'origine';

  @override
  String get toCurrency => 'Devise de destination';

  @override
  String get getQuote => 'Obtenir une offre';

  @override
  String get quoteSummary => 'Résumé de l\'offre';

  @override
  String get requiredField => 'Ce champ est requis';
}
