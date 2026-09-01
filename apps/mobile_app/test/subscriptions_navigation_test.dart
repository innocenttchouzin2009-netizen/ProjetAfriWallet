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
  Future<List<WalletBalance>> loadWalletBalances() async {
    return const [
      WalletBalance(
        walletId: 'WALLET-BETA13',
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
  Future<List<TransactionHistoryItem>> listTransactions() async => const [];
}

class _ReadySubscriptionRepository implements SubscriptionRepository {
  @override
  Future<void> cancelSubscription(String subscriptionId) async {}

  @override
  Future<void> createSubscription(String offerId) async {}

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async => null;

  @override
  Future<List<SubscriptionOffer>> fetchOffers({
    String? country,
    String? currency,
    String? query,
  }) async => const [];

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId) async => const [];

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async => const [];

  @override
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async {}
}

class _OfferSubscriptionRepository implements SubscriptionRepository {
  int createSubscriptionCalls = 0;

  static const offer = SubscriptionOffer(
    id: 'offer-beta14',
    providerId: 'provider-beta14',
    name: 'Beta14 Premium',
    description: 'Beta1.14 offer',
    price: 12.99,
    currency: 'EUR',
    country: 'DE',
    category: 'Entertainment',
    features: ['Feature A'],
    longDescription: 'Beta1.14 detailed offer description',
  );

  @override
  Future<void> cancelSubscription(String subscriptionId) async {}

  @override
  Future<void> createSubscription(String offerId) async {
    createSubscriptionCalls += 1;
  }

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async => offer;

  @override
  Future<List<SubscriptionOffer>> fetchOffers({
    String? country,
    String? currency,
    String? query,
  }) async => const [offer];

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId) async => const [];

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async => const [];

  @override
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async {}
}

void main() {
  testWidgets('Wallet Home exposes the subscriptions action', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      MaterialApp(
        home: WalletHomePage(
          repository: _ReadyWalletRepository(),
          transactionHistoryRepository: _ReadyTimelineRepository(),
          onSubscriptions: () {},
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('wallet-subscriptions-action')), findsOneWidget);
    expect(find.text('Abonnements'), findsOneWidget);
  });

  testWidgets('subscriptions callback is distinct from existing quick actions', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    var sendCount = 0;
    var receiveCount = 0;
    var qrCount = 0;
    var subscriptionsCount = 0;

    await tester.pumpWidget(
      MaterialApp(
        home: WalletHomePage(
          repository: _ReadyWalletRepository(),
          transactionHistoryRepository: _ReadyTimelineRepository(),
          onSend: () => sendCount += 1,
          onReceive: () => receiveCount += 1,
          onQr: () => qrCount += 1,
          onSubscriptions: () => subscriptionsCount += 1,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('wallet-subscriptions-action')));

    expect(subscriptionsCount, 1);
    expect(sendCount, 0);
    expect(receiveCount, 0);
    expect(qrCount, 0);
  });

  testWidgets('Wallet Home opens subscriptions and returns to Wallet Home', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(
      MaterialApp(
        locale: const Locale('fr'),
        supportedLocales: AppLocalizations.supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: WalletHomePage(
          repository: _ReadyWalletRepository(),
          transactionHistoryRepository: _ReadyTimelineRepository(),
          subscriptionRepository: _ReadySubscriptionRepository(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Wallet Home'), findsOneWidget);

    await tester.tap(find.byKey(const Key('wallet-subscriptions-action')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscriptions-return-to-wallet')), findsOneWidget);
    expect(find.text('Wallet Home'), findsNothing);

    await tester.tap(find.byKey(const Key('subscriptions-return-to-wallet')));
    await tester.pumpAndSettle();

    expect(find.text('Wallet Home'), findsOneWidget);
    expect(find.byKey(const Key('subscriptions-return-to-wallet')), findsNothing);
  });

  testWidgets('Subscriptions opens offer details and returns without creating subscription', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    final repository = _OfferSubscriptionRepository();

    await tester.pumpWidget(
      MaterialApp(
        locale: const Locale('fr'),
        supportedLocales: AppLocalizations.supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: SubscriptionsPage(repository: repository),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Beta14 Premium'), findsOneWidget);
    expect(find.text('Détails'), findsOneWidget);

    await tester.tap(find.text('Détails'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-detail-page')), findsOneWidget);
    expect(find.text('Beta1.14 detailed offer description'), findsOneWidget);
    expect(repository.createSubscriptionCalls, 0);

    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-detail-page')), findsNothing);
    expect(find.text('Beta14 Premium'), findsOneWidget);
    expect(find.text('Détails'), findsOneWidget);
    expect(repository.createSubscriptionCalls, 0);
  });

  testWidgets('Subscribe opens detail confirmation without creating subscription', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    final repository = _OfferSubscriptionRepository();

    await tester.pumpWidget(
      MaterialApp(
        locale: const Locale('fr'),
        supportedLocales: AppLocalizations.supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: SubscriptionsPage(repository: repository),
      ),
    );
    await tester.pumpAndSettle();

    expect(repository.createSubscriptionCalls, 0);
    expect(find.text('S’abonner'), findsOneWidget);

    await tester.tap(find.text('S’abonner'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-detail-page')), findsOneWidget);
    expect(find.byKey(const Key('subscription-offer-detail-continue')), findsOneWidget);
    expect(repository.createSubscriptionCalls, 0);

    await tester.tap(find.byKey(const Key('subscription-offer-detail-continue')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-confirmation-dialog')), findsOneWidget);
    expect(repository.createSubscriptionCalls, 0);

    await tester.tap(find.byKey(const Key('subscription-offer-confirmation-confirm')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-confirmation-dialog')), findsNothing);
    expect(find.byKey(const Key('subscription-offer-detail-page')), findsOneWidget);
    expect(repository.createSubscriptionCalls, 0);
  });
}
