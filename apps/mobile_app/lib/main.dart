import 'package:flutter/material.dart';

import 'l10n/app_localizations.dart';
import 'localization/language_manager.dart';
import 'localization/locale_controller.dart';
import 'pages/beta_welcome_page.dart';
import 'pages/identity_awid_page.dart';
import 'pages/language_settings_page.dart';
import 'pages/onboarding_auth_page.dart';
import 'pages/qr_payment_page.dart';
import 'pages/send_receive_page.dart';
import 'pages/subscriptions_page.dart';
import 'pages/transaction_history_page.dart';
import 'pages/wallet_home_page.dart';
import 'services/identity_repository.dart';
import 'services/qr_payment_repository.dart';
import 'services/subscription_repository.dart';
import 'services/transaction_history_repository.dart';
import 'services/transfer_repository.dart';
import 'services/wallet_repository.dart';
import 'theme/afwal_theme.dart';

void main() {
  runApp(const AfriWalletApp());
}

class AfriWalletApp extends StatefulWidget {
  const AfriWalletApp({
    super.key,
    this.repository,
    this.identityRepository,
    this.walletRepository,
    this.transferRepository,
    this.transactionHistoryRepository,
    this.qrPaymentRepository,
  });

  final SubscriptionRepository? repository;
  final IdentityRepository? identityRepository;
  final WalletRepository? walletRepository;
  final TransferRepository? transferRepository;
  final TransactionHistoryRepository? transactionHistoryRepository;
  final QrPaymentRepository? qrPaymentRepository;

  @override
  State<AfriWalletApp> createState() => _AfriWalletAppState();
}

class _AfriWalletAppState extends State<AfriWalletApp> {
  Locale _locale = const Locale('en');
  bool _isLocaleLoaded = false;
  bool _hasEnteredBeta = false;
  bool _hasCompletedOnboarding = false;
  bool _hasVisitedIdentity = false;
  bool _hasVisitedWalletHome = false;
  bool _hasVisitedSendReceive = false;
  bool _hasVisitedTransactions = false;
  bool _hasVisitedQrPayments = false;
  SendReceiveMode _sendReceiveInitialMode = SendReceiveMode.send;
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

  void _openSendReceive(SendReceiveMode mode) {
    setState(() {
      _sendReceiveInitialMode = mode;
      _hasVisitedWalletHome = true;
    });
  }

  void _returnToWalletHome() {
    setState(() {
      _sendReceiveInitialMode = SendReceiveMode.send;
      _hasVisitedWalletHome = false;
    });
  }

  Widget _buildCurrentExperience() {
    if (!_isLocaleLoaded) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (!_hasEnteredBeta) {
      return BetaWelcomePage(onContinue: () => setState(() => _hasEnteredBeta = true));
    }
    if (!_hasCompletedOnboarding) {
      return OnboardingAuthPage(onContinueToBeta: () => setState(() => _hasCompletedOnboarding = true));
    }
    if (!_hasVisitedIdentity) {
      return IdentityAwidPage(
        repository: widget.identityRepository ?? const UnavailableIdentityRepository(),
        onContinue: () => setState(() => _hasVisitedIdentity = true),
      );
    }
    if (!_hasVisitedWalletHome) {
      return WalletHomePage(
        repository: widget.walletRepository ?? const UnavailableWalletRepository(),
        transactionHistoryRepository:
            widget.transactionHistoryRepository ?? const UnavailableTransactionHistoryRepository(),
        onSend: () => _openSendReceive(SendReceiveMode.send),
        onReceive: () => _openSendReceive(SendReceiveMode.receive),
        onContinue: () => _openSendReceive(SendReceiveMode.send),
      );
    }
    if (!_hasVisitedSendReceive) {
      return SendReceivePage(
        repository: widget.transferRepository ?? const UnavailableTransferRepository(),
        initialMode: _sendReceiveInitialMode,
        onReturnToWallet: _returnToWalletHome,
      );
    }
    if (!_hasVisitedTransactions) {
      return TransactionHistoryPage(
        repository: widget.transactionHistoryRepository ?? const UnavailableTransactionHistoryRepository(),
        onContinue: () => setState(() => _hasVisitedTransactions = true),
      );
    }
    if (!_hasVisitedQrPayments) {
      return QrPaymentPage(
        repository: widget.qrPaymentRepository ?? const UnavailableQrPaymentRepository(),
        onContinue: () => setState(() => _hasVisitedQrPayments = true),
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
      title: 'AfriWallet',
      debugShowCheckedModeBanner: false,
      locale: _locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      theme: AfWalTheme.light(),
      home: Builder(builder: (_) => _buildCurrentExperience()),
    );
  }
}
