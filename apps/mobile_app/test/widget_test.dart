import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/main.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/current_identity_profile.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_lookup.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/models/mobile_money_payout_quote_intent.dart';
import 'package:mobile_app/models/payment_transfer.dart';
import 'package:mobile_app/models/public_afwal_identity.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/pages/mobile_money_payout_funding_selection_page.dart';
import 'package:mobile_app/presentation/mobile_money_payout_funding_selection_controller.dart';
import 'package:mobile_app/services/auth_production_wiring.dart';
import 'package:mobile_app/services/identity_read_repository.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';
import 'package:mobile_app/services/mobile_money_payout_quote_repository.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';
import 'package:mobile_app/services/subscription_repository.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';
import 'package:mobile_app/services/transfer_repository.dart';
import 'package:mobile_app/services/wallet_repository.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'app renders Mobile Beta welcome experience before auth restoration',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      final wiring = _testAuthWiring();
      addTearDown(wiring.dispose);

      await tester.pumpWidget(
        AfriWalletApp(
          repository: _FakeSubscriptionRepository(),
          authProductionWiring: wiring,
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('MOBILE BETA 1'), findsOneWidget);
      expect(
        find.text('Une identité.\nUn wallet.\nUne Afrique connectée.'),
        findsOneWidget,
      );
      expect(find.text('Découvrir AfWal'), findsOneWidget);
      expect(
        find.text('Connecting Africa. Empowering People.'),
        findsOneWidget,
      );
    },
  );

  testWidgets(
    'auth bootstrap fails closed to onboarding when no session is stored',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      var networkCalls = 0;
      final wiring = _testAuthWiring(
        onRequest: (_) async {
          networkCalls += 1;
          return http.Response('', 500);
        },
      );
      addTearDown(wiring.dispose);

      await tester.pumpWidget(
        AfriWalletApp(
          repository: _FakeSubscriptionRepository(),
          authProductionWiring: wiring,
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Découvrir AfWal'));
      await tester.pumpAndSettle();

      expect(
        find.text('Une identité pour une Afrique connectée'),
        findsOneWidget,
      );
      expect(find.text('AfWal ID indisponible'), findsNothing);
      expect(networkCalls, 0);
    },
  );

  testWidgets(
    'auth bootstrap restores stored session before entering app experience',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      var networkCalls = 0;
      final wiring = _testAuthWiring(
        onRequest: (_) async {
          networkCalls += 1;
          return http.Response('', 500);
        },
      );
      addTearDown(wiring.dispose);

      await wiring.sessionStore.save(
        StoredAuthSession(
          accessToken: 'stored-access',
          refreshToken: 'stored-refresh',
          tokenType: 'Bearer',
          sessionId: 'session-1',
          userId: 'user-1',
          accessTokenExpiresAtUtc:
              DateTime.now().toUtc().add(const Duration(hours: 1)),
        ),
      );

      await tester.pumpWidget(
        AfriWalletApp(
          repository: _FakeSubscriptionRepository(),
          authProductionWiring: wiring,
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Découvrir AfWal'));
      await tester.pumpAndSettle();

      expect(find.text('Mon AfWal ID'), findsOneWidget);
      expect(find.text('AfWal ID indisponible'), findsOneWidget);
      expect(
        find.text('Une identité pour une Afrique connectée'),
        findsNothing,
      );
      expect(networkCalls, 0);
    },
  );

  testWidgets(
    'premium wallet home receives authoritative AfWal ID from identity handoff',
    (WidgetTester tester) async {
      await tester.binding.setSurfaceSize(const Size(900, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      SharedPreferences.setMockInitialValues({});
      final wiring = _testAuthWiring();
      addTearDown(wiring.dispose);

      await wiring.sessionStore.save(
        StoredAuthSession(
          accessToken: 'stored-access',
          refreshToken: 'stored-refresh',
          tokenType: 'Bearer',
          sessionId: 'session-1',
          userId: 'user-1',
          accessTokenExpiresAtUtc:
              DateTime.now().toUtc().add(const Duration(hours: 1)),
        ),
      );

      await tester.pumpWidget(
        AfriWalletApp(
          repository: _FakeSubscriptionRepository(),
          authProductionWiring: wiring,
          identityReadRepository: _ReadyIdentityReadRepository(),
          walletRepository: _ReadyWalletRepository(),
          transferRepository: _ReadyTransferRepository(),
          transactionHistoryRepository: _ReadyTimelineRepository(),
          mobileMoneyBeneficiaryLookupRepository:
              _ReadyMobileMoneyBeneficiaryLookupRepository(),
          mobileMoneyPayoutQuoteRepository:
              _ReadyMobileMoneyPayoutQuoteRepository(),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Découvrir AfWal'));
      await tester.pumpAndSettle();

      expect(find.text('@premium-user'), findsOneWidget);
      await tester.tap(find.text('Continuer'));
      await tester.pumpAndSettle();

      expect(find.text('Wallet Home'), findsOneWidget);
      expect(
        find.byKey(const Key('wallet-home-primary-balance')),
        findsOneWidget,
      );
      expect(find.byKey(const Key('wallet-home-afwal-id')), findsNothing);

      await tester.tap(
        find.byKey(const Key('wallet-home-primary-balance')),
      );
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('wallet-home-afwal-id')), findsOneWidget);
      expect(find.text('@premium-user'), findsOneWidget);

      final mobileMoneyAction =
          find.byKey(const Key('wallet-quick-action-mobile-money'));
      await tester.ensureVisible(mobileMoneyAction);
      await tester.tap(mobileMoneyAction);
      await tester.pumpAndSettle();

      expect(find.text('Ajouter un bénéficiaire'), findsOneWidget);
      expect(find.text('Rechercher un bénéficiaire Mobile Money'), findsOneWidget);

      await tester.enterText(
        find.byKey(const Key('momo-beneficiary-search-input')),
        '+237670123456',
      );
      await tester.pump();
      await tester.tap(
        find.byKey(const Key('momo-beneficiary-search-submit')),
      );
      await tester.pumpAndSettle();
      await tester.tap(
        find.byKey(const Key('momo-beneficiary-search-continue')),
      );
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('momo-quote-intent-entry-page')),
        findsOneWidget,
      );
      await tester.enterText(
        find.byKey(const Key('momo-quote-intent-amount')),
        '10.00',
      );
      await tester.tap(
        find.byKey(const Key('momo-quote-intent-continue')),
      );
      await tester.pumpAndSettle();

      expect(find.text('Votre estimation'), findsOneWidget);
      expect(find.text('Ada N.'), findsOneWidget);

      await tester.drag(
        find.byKey(const Key('momo-quote-ready')),
        const Offset(0, -600),
      );
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('momo-quote-continue')), findsOneWidget);
      await tester.tap(find.byKey(const Key('momo-quote-continue')));
      await tester.pumpAndSettle();

      expect(
        find.byType(MobileMoneyPayoutFundingSelectionPage),
        findsOneWidget,
      );
      final fundingPage = tester.widget<MobileMoneyPayoutFundingSelectionPage>(
        find.byType(MobileMoneyPayoutFundingSelectionPage),
      );
      final fundingController = fundingPage.controller;
      final fundingWallet = fundingController.fundingSources.single;
      expect(fundingWallet.id, 'wallet-premium-eur');
      expect(fundingWallet.type, FundingSourceType.wallet);
      expect(fundingWallet.currencyCode, 'EUR');
      expect(fundingWallet.availableMinor, 12345);
      expect(fundingController.availableExternalSources, isEmpty);
      expect(fundingController.canUseSplit, isFalse);
      expect(fundingController.canUseWalletOnly, isTrue);

      await tester.tap(find.byKey(const Key('momo-funding-wallet')));
      await tester.pump();
      expect(
        fundingController.selectedMode,
        MobileMoneyPayoutFundingSelectionMode.walletOnly,
      );
      expect(
        find.byKey(const Key('momo-funding-continue')),
        findsNothing,
      );

      await tester.tap(find.byKey(const Key('momo-funding-back')));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('momo-quote-ready')), findsOneWidget);

      await tester.tap(find.byKey(const Key('momo-quote-back')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('momo-quote-intent-back')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('momo-beneficiary-search-back')));
      await tester.pumpAndSettle();

      expect(find.text('Wallet Home'), findsOneWidget);
      expect(find.byKey(const Key('wallet-home-primary-balance')), findsOneWidget);
    },
  );

}

AuthProductionWiring _testAuthWiring({
  Future<http.Response> Function(http.Request request)? onRequest,
}) {
  final storage = _MemorySecureStorageAdapter();
  final apiClient = ApiClient(
    baseUrl: 'https://api.afwal.test',
    httpClient: MockClient(
      onRequest ?? (_) async => http.Response('', 500),
    ),
  );

  return AuthProductionWiring(
    apiClient: apiClient,
    secureStorageAdapter: storage,
  );
}


class _ReadyIdentityReadRepository implements IdentityReadRepository {
  @override
  Future<CurrentIdentityProfile> loadCurrentProfile() async {
    return CurrentIdentityProfile(
      userId: 'user-1',
      identifier: 'user@example.com',
      createdAtUtc: DateTime.utc(2026, 10, 15, 10),
    );
  }

  @override
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) async {
    return PublicAfWalIdentity(afWalId: afWalId);
  }
}

class _ReadyTransferRepository implements TransferRepository {
  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async {
    return const ReceiveIdentity(
      publicLabel: '@premium-user',
      qrToken: 'premium-qr-token',
    );
  }

  @override
  Future<TransferReceipt> send(SendTransferRequest request) {
    throw UnimplementedError();
  }
}


class _ReadyMobileMoneyBeneficiaryLookupRepository
    implements MobileMoneyBeneficiaryLookupRepository {
  @override
  Future<MobileMoneyBeneficiarySearchResult> search({
    required String phoneNumber,
  }) async {
    return const MobileMoneyBeneficiarySearchResult(
      lookup: MobileMoneyBeneficiaryLookup(
        normalizedPhoneNumber: '+237670123456',
        countryCode: 'CM',
        operator: 'MTN',
        operatorResolved: true,
        accountHolderName: 'Ada N.',
        beneficiaryResolved: true,
      ),
      operator: MobileMoneyBeneficiaryOperator.mtnCameroon,
    );
  }
}

class _ReadyMobileMoneyPayoutQuoteRepository
    implements MobileMoneyPayoutQuoteRepository {
  @override
  Future<MobileMoneyPayoutQuote> createQuoteFromIntent(
    MobileMoneyPayoutQuoteIntent intent,
  ) async {
    return MobileMoneyPayoutQuote(
      quoteId: 'quote-widget',
      sourceCurrencyCode: intent.sourceCurrencyCode,
      sourceAmountMinor: intent.sourceAmountMinor,
      fees: const <MobileMoneyPayoutQuoteFee>[],
      totalFeeMinor: 0,
      totalSourceDebitMinor: intent.sourceAmountMinor,
      destinationCurrencyCode: intent.destinationCurrencyCode,
      destinationAmountMinor: 650000,
      fxRate: 650,
      createdAtUtc: DateTime.utc(2026, 10, 16, 10),
      expiresAtUtc: DateTime.utc(2099),
    );
  }

  @override
  Future<MobileMoneyPayoutQuote> createQuote({
    required String sourceCountryCode,
    required String sourceCurrencyCode,
    required String destinationCountryCode,
    required String destinationCurrencyCode,
    required String operatorCode,
    required int sourceAmountMinor,
  }) {
    throw StateError('The integrated handoff must use a quote intent.');
  }
}

class _ReadyWalletRepository implements WalletRepository {
  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    return const <WalletBalance>[
      WalletBalance(
        walletId: 'wallet-premium-eur',
        currency: 'EUR',
        availableMinor: 12345,
        status: 'ACTIVE',
        countryCode: 'DE',
      ),
    ];
  }
}

class _ReadyTimelineRepository implements TransactionHistoryRepository {
  @override
  Future<List<TransactionHistoryItem>> listTransactions() async {
    return const <TransactionHistoryItem>[];
  }
}

class _MemorySecureStorageAdapter implements SecureStorageAdapter {
  final Map<String, String> _values = <String, String>{};

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {
    _values[key] = value;
  }

  @override
  Future<String?> read({required String key}) async => _values[key];

  @override
  Future<void> delete({required String key}) async {
    _values.remove(key);
  }
}

class _FakeSubscriptionRepository implements SubscriptionRepository {
  @override
  Future<List<SubscriptionOffer>> fetchOffers({
    String? country,
    String? currency,
    String? query,
  }) async {
    return [];
  }

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async {
    return null;
  }

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async {
    return [];
  }

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(
    String subscriptionId,
  ) async {
    return [];
  }

  @override
  Future<void> createSubscription(String offerId) async {}

  @override
  Future<void> cancelSubscription(String subscriptionId) async {}

  @override
  Future<void> toggleAutoRenew(
    String subscriptionId,
    bool enabled,
  ) async {}
}