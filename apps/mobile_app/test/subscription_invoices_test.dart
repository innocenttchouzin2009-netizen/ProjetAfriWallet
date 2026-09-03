import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoices_page.dart';
import 'package:mobile_app/services/subscription_repository.dart';

const _subscription = UserSubscription(
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

class _InvoiceRepository implements SubscriptionRepository {
  _InvoiceRepository({this.invoices = const [], this.shouldFail = false});

  final List<SubscriptionInvoice> invoices;
  final bool shouldFail;
  int fetchInvoicesCalls = 0;
  int createCalls = 0;
  int cancelCalls = 0;
  int toggleCalls = 0;

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId) async {
    fetchInvoicesCalls += 1;
    if (shouldFail) throw Exception('invoice failure');
    return invoices;
  }

  @override
  Future<void> cancelSubscription(String subscriptionId) async => cancelCalls += 1;

  @override
  Future<void> createSubscription(String offerId) async => createCalls += 1;

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async => null;

  @override
  Future<List<SubscriptionOffer>> fetchOffers({String? country, String? currency, String? query}) async => const [];

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async => const [];

  @override
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async => toggleCalls += 1;
}

Widget _app(_InvoiceRepository repository, {VoidCallback? onReturn}) => MaterialApp(
      locale: const Locale('fr'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoicesPage(
        subscription: _subscription,
        repository: repository,
        onReturnToSubscription: onReturn,
      ),
    );

void main() {
  testWidgets('invoice history displays read-only billing data', (tester) async {
    final repository = _InvoiceRepository(
      invoices: const [
        SubscriptionInvoice(
          id: 'invoice-beta17-1',
          subscriptionId: 'subscription-beta17',
          amount: 19.99,
          currency: 'EUR',
          status: 'PAID',
          issueDate: '2026-09-03',
        ),
      ],
    );

    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-invoices-page')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-invoice-beta17-1')), findsOneWidget);
    expect(find.textContaining('invoice-beta17-1'), findsOneWidget);
    expect(find.textContaining('PAID'), findsOneWidget);
    expect(find.textContaining('2026-09-03'), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    expect(repository.createCalls, 0);
    expect(repository.cancelCalls, 0);
    expect(repository.toggleCalls, 0);
  });

  testWidgets('invoice history exposes an empty state', (tester) async {
    final repository = _InvoiceRepository();
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-invoices-empty')), findsOneWidget);
  });

  testWidgets('invoice history exposes retry on fetch error', (tester) async {
    final repository = _InvoiceRepository(shouldFail: true);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-invoices-error')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoices-retry')), findsOneWidget);
  });

  testWidgets('invoice history returns explicitly to subscription detail', (tester) async {
    var returns = 0;
    final repository = _InvoiceRepository();
    await tester.pumpWidget(_app(repository, onReturn: () => returns += 1));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('subscription-invoices-return-detail')));
    expect(returns, 1);
  });
}
