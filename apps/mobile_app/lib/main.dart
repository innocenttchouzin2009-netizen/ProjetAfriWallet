import 'dart:async';

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
import 'services/auth_production_wiring.dart';
import 'services/authenticated_production_wiring.dart';
import 'services/identity_read_repository.dart';
import 'services/mobile_money_beneficiary_lookup_repository.dart';
import 'services/mobile_money_payout_funding_repository.dart';
import 'services/mobile_money_payout_quote_repository.dart';
import 'services/qr_payment_repository.dart';
import 'services/secure_session_store.dart';
import 'services/subscription_repository.dart';
import 'services/transaction_history_repository.dart';
import 'services/transfer_repository.dart';
import 'services/wallet_production_wiring.dart';
import 'services/wallet_repository.dart';
import 'theme/afwal_theme.dart';
import 'widgets/auth_session_gate.dart';

void main() {
  runApp(const AfriWalletApp());
}

class AfriWalletApp extends StatefulWidget {
  const AfriWalletApp({
    super.key,
    this.repository,
    this.identityReadRepository,
    this.walletRepository,
    this.authSessionStore,
    this.authProductionWiring,
    this.transferRepository,
    this.transactionHistoryRepository,
    this.qrPaymentRepository,
    this.mobileMoneyBeneficiaryLookupRepository,
    this.mobileMoneyPayoutQuoteRepository,
    this.mobileMoneyPayoutFundingRepository,
  });

  final SubscriptionRepository? repository;
  final IdentityReadRepository? identityReadRepository;
  final WalletRepository? walletRepository;
  final AuthSessionStore? authSessionStore;
  final AuthProductionWiring? authProductionWiring;
  final TransferRepository? transferRepository;
  final TransactionHistoryRepository? transactionHistoryRepository;
  final QrPaymentRepository? qrPaymentRepository;
  final MobileMoneyBeneficiaryLookupRepository?
      mobileMoneyBeneficiaryLookupRepository;
  final MobileMoneyPayoutQuoteRepository? mobileMoneyPayoutQuoteRepository;
  final MobileMoneyPayoutFundingRepository? mobileMoneyPayoutFundingRepository;

  @override
  State<AfriWalletApp> createState() => _AfriWalletAppState();
}

class _AfriWalletAppState extends State<AfriWalletApp> {
  Locale _locale = const Locale('en');
  bool _isLocaleLoaded = false;
  bool _hasEnteredBeta = false;
  bool _hasVisitedIdentity = false;
  String? _currentAfWalId;
  bool _hasVisitedWalletHome = false;
  bool _hasVisitedSendReceive = false;
  bool _hasVisitedTransactions = false;
  bool _hasVisitedQrPayments = false;
  SendReceiveMode _sendReceiveInitialMode = SendReceiveMode.send;
  LocaleController? _localeController;
  AuthenticatedProductionWiring? _ownedAuthenticatedProductionWiring;
  AuthProductionWiring? _ownedAuthProductionWiring;
  late final AuthProductionWiring _authProductionWiring;
  WalletProductionWiring? _walletProductionWiring;
  late final IdentityReadRepository _identityReadRepository;
  late final WalletRepository _walletRepository;
  late final TransferRepository _transferRepository;
  late final TransactionHistoryRepository _transactionHistoryRepository;
  late final QrPaymentRepository _qrPaymentRepository;
  late final MobileMoneyBeneficiaryLookupRepository?
      _mobileMoneyBeneficiaryLookupRepository;
  late final MobileMoneyPayoutQuoteRepository?
      _mobileMoneyPayoutQuoteRepository;
  late final MobileMoneyPayoutFundingRepository?
      _mobileMoneyPayoutFundingRepository;

  @override
  void initState() {
    super.initState();
    _wireProductionDependencies();
    _loadSavedLocale();
  }

  void _wireProductionDependencies() {
    if (widget.authProductionWiring == null && widget.authSessionStore == null) {
      final wiring = AuthenticatedProductionWiring();
      _ownedAuthenticatedProductionWiring = wiring;
      _authProductionWiring = wiring.auth;
      _identityReadRepository =
          widget.identityReadRepository ?? wiring.identityReadRepository;
      _walletRepository = widget.walletRepository ?? wiring.walletRepository;
      _transferRepository =
          widget.transferRepository ?? wiring.transferRepository;
      _transactionHistoryRepository = widget.transactionHistoryRepository ??
          wiring.transactionHistoryRepository;
      _qrPaymentRepository =
          widget.qrPaymentRepository ?? wiring.qrPaymentRepository;
      _mobileMoneyBeneficiaryLookupRepository =
          widget.mobileMoneyBeneficiaryLookupRepository ??
              wiring.mobileMoneyBeneficiaryLookupRepository;
      _mobileMoneyPayoutQuoteRepository =
          widget.mobileMoneyPayoutQuoteRepository ??
              wiring.mobileMoneyPayoutQuoteRepository;
      _mobileMoneyPayoutFundingRepository =
          widget.mobileMoneyPayoutFundingRepository ??
              wiring.mobileMoneyPayoutFundingRepository;
      return;
    }

    _wireAuthProduction();
    _identityReadRepository = widget.identityReadRepository ??
        const UnavailableIdentityReadRepository();
    _wireWalletRepository();
    _transferRepository =
        widget.transferRepository ?? const UnavailableTransferRepository();
    _transactionHistoryRepository = widget.transactionHistoryRepository ??
        const UnavailableTransactionHistoryRepository();
    _qrPaymentRepository =
        widget.qrPaymentRepository ?? const UnavailableQrPaymentRepository();
    _mobileMoneyBeneficiaryLookupRepository =
        widget.mobileMoneyBeneficiaryLookupRepository;
    _mobileMoneyPayoutQuoteRepository =
        widget.mobileMoneyPayoutQuoteRepository;
    _mobileMoneyPayoutFundingRepository =
        widget.mobileMoneyPayoutFundingRepository;
  }

  void _wireAuthProduction() {
    final injectedWiring = widget.authProductionWiring;
    if (injectedWiring != null) {
      _authProductionWiring = injectedWiring;
      return;
    }

    final wiring = AuthProductionWiring();
    _ownedAuthProductionWiring = wiring;
    _authProductionWiring = wiring;
  }

  void _wireWalletRepository() {
    final injectedRepository = widget.walletRepository;
    if (injectedRepository != null) {
      _walletRepository = injectedRepository;
      return;
    }

    final wiring = WalletProductionWiring(
      requestExecutor: _authProductionWiring.requestExecutor,
    );
    _walletProductionWiring = wiring;
    _walletRepository = wiring.repository;
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

  void _handleIdentityContinue(String? afWalId) {
    final normalized = afWalId?.trim();
    setState(() {
      _currentAfWalId =
          normalized != null && normalized.isNotEmpty ? normalized : null;
      _hasVisitedIdentity = true;
    });
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

  Widget _buildAuthenticatedExperience() {
    if (!_hasVisitedIdentity) {
      return IdentityAwidPage(
        identityRepository: _identityReadRepository,
        transferRepository: _transferRepository,
        onContinue: _handleIdentityContinue,
      );
    }
    if (!_hasVisitedWalletHome) {
      return WalletHomePage(
        repository: _walletRepository,
        transactionHistoryRepository: _transactionHistoryRepository,
        qrPaymentRepository: _qrPaymentRepository,
        mobileMoneyBeneficiaryLookupRepository:
            _mobileMoneyBeneficiaryLookupRepository,
        mobileMoneyPayoutQuoteRepository:
            _mobileMoneyPayoutQuoteRepository,
        mobileMoneyPayoutFundingRepository:
            _mobileMoneyPayoutFundingRepository,
        subscriptionRepository: widget.repository,
        afWalId: _currentAfWalId,
        onSend: () => _openSendReceive(SendReceiveMode.send),
        onReceive: () => _openSendReceive(SendReceiveMode.receive),
        onContinue: () => _openSendReceive(SendReceiveMode.send),
      );
    }
    if (!_hasVisitedSendReceive) {
      return SendReceivePage(
        repository: _transferRepository,
        initialMode: _sendReceiveInitialMode,
        onReturnToWallet: _returnToWalletHome,
        onContinue: () => setState(() => _hasVisitedSendReceive = true),
      );
    }
    if (!_hasVisitedTransactions) {
      return TransactionHistoryPage(
        repository: _transactionHistoryRepository,
        onReturnToWallet: _returnToWalletHome,
        onContinue: () => setState(() => _hasVisitedTransactions = true),
      );
    }
    if (!_hasVisitedQrPayments) {
      return QrPaymentPage(
        repository: _qrPaymentRepository,
        onContinue: () => setState(() => _hasVisitedQrPayments = true),
      );
    }
    return SubscriptionsPage(
      repository: widget.repository,
      locale: _locale,
      onOpenSettings: () {
        Navigator.of(context).push(
          MaterialPageRoute<void>(
            builder: (context) => LanguageSettingsPage(
              onLocaleChanged: (locale) async {
                await _handleLocaleChanged(locale);
                if (!context.mounted) return;
                Navigator.of(context).pop();
              },
            ),
          ),
        );
      },
    );
  }

  Widget _buildCurrentExperience() {
    if (!_isLocaleLoaded) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (!_hasEnteredBeta) {
      return BetaWelcomePage(
        onContinue: () => setState(() => _hasEnteredBeta = true),
      );
    }

    return AuthSessionGate(
      controller: _authProductionWiring.stateController,
      restoringBuilder: (_) => const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      ),
      unauthenticatedBuilder: (_) => OnboardingAuthPage(
        onContinueToBeta: () {
          unawaited(_authProductionWiring.stateController.restore());
        },
      ),
      authenticatedBuilder: (_, _) => _buildAuthenticatedExperience(),
      restorationFailedBuilder: (_, retry) => Scaffold(
        body: SafeArea(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.lock_reset_rounded, size: 56),
                  const SizedBox(height: 16),
                  const Text(
                    'Impossible de restaurer la session',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 12),
                  const Text(
                    'Votre session reste fermée tant que sa restauration sécurisée n’a pas réussi.',
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 20),
                  FilledButton(
                    onPressed: () => unawaited(retry()),
                    child: const Text('Réessayer'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _walletProductionWiring?.dispose();
    _ownedAuthProductionWiring?.dispose();
    _ownedAuthenticatedProductionWiring?.dispose();
    super.dispose();
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