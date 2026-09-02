import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/pages/subscriptions_page.dart';
import 'package:mobile_app/pages/wallet_home_page.dart';
import 'package:mobile_app/services/subscription_repository.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';
import 'package:mobile_app/services/wallet_repository.dart';

class _ReadyWalletRepository implements WalletRepository {
  @override
  Future<List<WalletBalance>> loadWalletBalances() async => const [
        WalletBalance(walletId: 'WALLET-BETA16', currency: 'EUR', availableMinor: 12345, status: 'ACTIVE', countryCode: 'DE'),
      ];
}

class _ReadyTimelineRepository implements TransactionHistoryRepository {
  @override
  Future<List<TransactionHistoryItem>> listTransactions() async => const [];
}

class _SubscriptionRepository implements SubscriptionRepository {
  int createSubscriptionCalls = 0;
  int cancelSubscriptionCalls = 0;
  int toggleAutoRenewCalls = 0;

  static const offer = SubscriptionOffer(
    id: 'offer-beta15',
    providerId: 'provider-beta15',
    name: 'Beta15 Premium',
    description: 'Beta1.15 offer',
    price: 14.99,
    currency: 'EUR',
    country: 'DE',
    category: 'Entertainment',
    features: ['Feature A'],
    longDescription: 'Beta1.15 detailed offer description',
  );

  static const subscription = UserSubscription(
    id: 'subscription-beta16',
    offerId: 'offer-beta16',
    providerId: 'provider-beta16',
    name: 'Beta16 Premium',
    status: 'ACTIVE',
    autoRenew: true,
    nextBillingDate: '2026-10-02',
    currentCycle: 'Cycle 2',
    price: 14.99,
    currency: 'EUR',
  );

  @override
  Future<void> cancelSubscription(String subscriptionId) async => cancelSubscriptionCalls += 1;

  @override
  Future<void> createSubscription(String offerId) async => createSubscriptionCalls += 1;

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async => offer;

  @override
  Future<List<SubscriptionOffer>> fetchOffers({String? country, String? currency, String? query}) async => const [offer];

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId) async => const [];

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async => const [subscription];

  @override
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async => toggleAutoRenewCalls += 1;
}

Finder _subscribeButton() {
  final offerCard = find.ancestor(of: find.text('Beta15 Premium'), matching: find.byType(Card));
  return find.descendant(of: offerCard, matching: find.byType(FilledButton));
}

Future<void> _confirmOffer(WidgetTester tester) async {
  await tester.tap(_subscribeButton());
  await tester.pumpAndSettle();
  await tester.tap(find.byKey(const Key('subscription-offer-detail-continue')));
  await tester.pumpAndSettle();
  await tester.tap(find.byKey(const Key('subscription-offer-confirmation-confirm')));
  await tester.pumpAndSettle();
}

Widget _subscriptionsApp(_SubscriptionRepository repository) => MaterialApp(
      locale: const Locale('fr'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionsPage(repository: repository),
    );

void main() {
  testWidgets('Wallet Home exposes the subscriptions action', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(MaterialApp(home: WalletHomePage(repository: _ReadyWalletRepository(), transactionHistoryRepository: _ReadyTimelineRepository(), onSubscriptions: () {})));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('wallet-subscriptions-action')), findsOneWidget);
    expect(find.text('Abonnements'), findsOneWidget);
  });

  testWidgets('Wallet Home opens subscriptions and returns to Wallet Home', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final repository = _SubscriptionRepository();
    await tester.pumpWidget(MaterialApp(locale: const Locale('fr'), supportedLocales: AppLocalizations.supportedLocales, localizationsDelegates: AppLocalizations.localizationsDelegates, home: WalletHomePage(repository: _ReadyWalletRepository(), transactionHistoryRepository: _ReadyTimelineRepository(), subscriptionRepository: repository)));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('wallet-subscriptions-action')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('subscriptions-return-to-wallet')));
    await tester.pumpAndSettle();
    expect(find.text('Wallet Home'), findsOneWidget);
  });

  testWidgets('my subscription opens detail and returns without lifecycle mutations', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final repository = _SubscriptionRepository();
    await tester.pumpWidget(_subscriptionsApp(repository));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('subscription-card-open-subscription-beta16')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-detail-page')), findsOneWidget);
    expect(find.text('ACTIVE'), findsOneWidget);
    await tester.tap(find.byKey(const Key('subscription-detail-return-subscriptions')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-detail-page')), findsNothing);
    expect(repository.cancelSubscriptionCalls, 0);
    expect(repository.toggleAutoRenewCalls, 0);
    expect(repository.createSubscriptionCalls, 0);
  });

  testWidgets('confirmation still opens activation result without creating subscription', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final repository = _SubscriptionRepository();
    await tester.pumpWidget(_subscriptionsApp(repository));
    await tester.pumpAndSettle();
    await _confirmOffer(tester);
    expect(find.byKey(const Key('subscription-activation-result-page')), findsOneWidget);
    expect(repository.createSubscriptionCalls, 0);
  });

  testWidgets('activation result returns through subscription flow to Wallet Home', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final repository = _SubscriptionRepository();
    await tester.pumpWidget(MaterialApp(locale: const Locale('fr'), supportedLocales: AppLocalizations.supportedLocales, localizationsDelegates: AppLocalizations.localizationsDelegates, home: WalletHomePage(repository: _ReadyWalletRepository(), transactionHistoryRepository: _ReadyTimelineRepository(), subscriptionRepository: repository)));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('wallet-subscriptions-action')));
    await tester.pumpAndSettle();
    await _confirmOffer(tester);
    await tester.tap(find.byKey(const Key('subscription-activation-result-return-wallet')));
    await tester.pumpAndSettle();
    expect(find.text('Wallet Home'), findsOneWidget);
    expect(repository.createSubscriptionCalls, 0);
  });
}
