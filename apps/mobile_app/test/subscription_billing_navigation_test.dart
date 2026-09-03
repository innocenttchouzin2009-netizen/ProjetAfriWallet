import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscriptions_page.dart';
import 'package:mobile_app/services/subscription_repository.dart';

class _BillingNavigationRepository implements SubscriptionRepository {
  int fetchInvoicesCalls = 0;
  int createCalls = 0;
  int cancelCalls = 0;
  int toggleCalls = 0;

  static const subscription = UserSubscription(
    id: 'subscription-beta17',
    offerId: 'offer-beta17',
    providerId: 'provider-beta17',
    name: 'Beta17 Premium',
    status: 'ACTIVE',
    autoRenew: true,
    nextBillingDate: '2026-10-03',
    currentCycle: 'Cycle 3',
    price: 19.99,
    currency: 'EUR',
  );

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId) async {
    fetchInvoicesCalls += 1;
    return const [
      SubscriptionInvoice(
        id: 'invoice-beta17-route',
        subscriptionId: 'subscription-beta17',
        amount: 19.99,
        currency: 'EUR',
        status: 'PAID',
        issueDate: '2026-09-03',
      ),
    ];
  }

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async => const [subscription];

  @override
  Future<List<SubscriptionOffer>> fetchOffers({String? country, String? currency, String? query}) async => const [];

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async => null;

  @override
  Future<void> createSubscription(String offerId) async => createCalls += 1;

  @override
  Future<void> cancelSubscription(String subscriptionId) async => cancelCalls += 1;

  @override
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async => toggleCalls += 1;
}

Widget _app(_BillingNavigationRepository repository) => MaterialApp(
      locale: const Locale('fr'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionsPage(repository: repository),
    );

void main() {
  testWidgets('My Subscriptions opens billing history and returns to detail', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    final repository = _BillingNavigationRepository();
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-card-open-subscription-beta17')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-detail-page')), findsOneWidget);

    await tester.tap(find.byKey(const Key('subscription-detail-billing-history')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-invoices-page')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-invoice-beta17-route')), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);

    await tester.tap(find.byKey(const Key('subscription-invoices-return-detail')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-detail-page')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoices-page')), findsNothing);

    expect(repository.createCalls, 0);
    expect(repository.cancelCalls, 0);
    expect(repository.toggleCalls, 0);
  });
}
