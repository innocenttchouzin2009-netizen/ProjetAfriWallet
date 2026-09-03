import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoices_page.dart';
import 'package:mobile_app/services/subscription_repository.dart';

const _subscription = UserSubscription(
  id: 'subscription-beta17', offerId: 'offer-beta17', providerId: 'provider-beta17',
  name: 'Beta17 Premium', status: 'ACTIVE', autoRenew: true,
  nextBillingDate: '2026-10-03', currentCycle: 'Cycle 3', price: 19.99, currency: 'EUR',
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
  @override Future<void> cancelSubscription(String subscriptionId) async => cancelCalls += 1;
  @override Future<void> createSubscription(String offerId) async => createCalls += 1;
  @override Future<SubscriptionOffer?> fetchOffer(String offerId) async => null;
  @override Future<List<SubscriptionOffer>> fetchOffers({String? country, String? currency, String? query}) async => const [];
  @override Future<List<UserSubscription>> fetchUserSubscriptions() async => const [];
  @override Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async => toggleCalls += 1;
}

Widget _app(
  _InvoiceRepository repository, {
  VoidCallback? onReturn,
  Locale locale = const Locale('fr'),
}) => MaterialApp(
  locale: locale,
  supportedLocales: AppLocalizations.supportedLocales,
  localizationsDelegates: AppLocalizations.localizationsDelegates,
  home: SubscriptionInvoicesPage(subscription: _subscription, repository: repository, onReturnToSubscription: onReturn),
);

const _paid = SubscriptionInvoice(
  id: 'INV-PAID-001', subscriptionId: 'subscription-beta17', amount: 19.99,
  currency: 'EUR', status: 'PAID', issueDate: '2026-09-03',
);
const _pending = SubscriptionInvoice(
  id: 'INV-PENDING-002', subscriptionId: 'subscription-beta17', amount: 29.99,
  currency: 'EUR', status: 'PENDING', issueDate: '2026-10-03',
);

void _expectZeroMutations(_InvoiceRepository repository) {
  expect(repository.createCalls, 0);
  expect(repository.cancelCalls, 0);
  expect(repository.toggleCalls, 0);
}

void _expectInvoiceBefore(WidgetTester tester, String firstId, String secondId) {
  final first = tester.getTopLeft(find.byKey(Key('subscription-invoice-$firstId'))).dy;
  final second = tester.getTopLeft(find.byKey(Key('subscription-invoice-$secondId'))).dy;
  expect(first, lessThan(second));
}

Future<void> _selectSort(WidgetTester tester, String label) async {
  await tester.tap(find.byKey(const Key('subscription-invoice-sort')));
  await tester.pumpAndSettle();
  await tester.tap(find.text(label).last);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('invoice history displays localized read-only billing data', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-invoice-search')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-status-filter')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-sort')), findsOneWidget);
    expect(find.text('Plus récentes d’abord'), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-INV-PAID-001')), findsOneWidget);
    expect(find.text('Payée'), findsOneWidget);
    expect(find.text('PAID'), findsNothing);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice sorting defaults to newest and supports oldest locally', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    _expectInvoiceBefore(tester, 'INV-PENDING-002', 'INV-PAID-001');

    await _selectSort(tester, 'Plus anciennes d’abord');
    _expectInvoiceBefore(tester, 'INV-PAID-001', 'INV-PENDING-002');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice sorting supports amount ascending and descending locally', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_pending, _paid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await _selectSort(tester, 'Montant : croissant');
    _expectInvoiceBefore(tester, 'INV-PAID-001', 'INV-PENDING-002');

    await _selectSort(tester, 'Montant : décroissant');
    _expectInvoiceBefore(tester, 'INV-PENDING-002', 'INV-PAID-001');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice sorting labels are localized in English', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid]);
    await tester.pumpWidget(_app(repository, locale: const Locale('en')));
    await tester.pumpAndSettle();
    expect(find.text('Sort invoices'), findsOneWidget);
    expect(find.text('Newest first'), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice search is local case insensitive and trims spaces', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), '  paid-001  ');
    await tester.pump();
    expect(find.byKey(const Key('subscription-invoice-INV-PAID-001')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-INV-PENDING-002')), findsNothing);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice search combines with localized status filter and sorting', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), 'inv');
    await tester.tap(find.byKey(const Key('subscription-invoice-status-filter')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('En attente').last);
    await tester.pumpAndSettle();
    await _selectSort(tester, 'Montant : croissant');
    expect(find.byKey(const Key('subscription-invoice-INV-PAID-001')), findsNothing);
    expect(find.byKey(const Key('subscription-invoice-INV-PENDING-002')), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('clearing invoice search restores status-filtered results', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), 'missing');
    await tester.pump();
    expect(find.byKey(const Key('subscription-invoices-search-empty')), findsOneWidget);
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), '');
    await tester.pump();
    expect(find.byKey(const Key('subscription-invoice-INV-PAID-001')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-INV-PENDING-002')), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice search exposes localized no-result state', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), 'unknown');
    await tester.pump();
    expect(find.byKey(const Key('subscription-invoices-search-empty')), findsOneWidget);
    expect(find.text('Aucune facture trouvée'), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice history opens invoice detail and returns without mutations', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('subscription-invoice-open-INV-PAID-001')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-invoice-detail-page')), findsOneWidget);
    expect(find.text('Payée'), findsOneWidget);
    await tester.tap(find.byKey(const Key('subscription-invoice-detail-return-history')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('subscription-invoices-page')), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
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
