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

String _dateString(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

SubscriptionInvoice _relativeInvoice({
  required String id,
  required int daysAgo,
  required String status,
  required double amount,
}) {
  final date = DateTime.now().subtract(Duration(days: daysAgo));
  return SubscriptionInvoice(
    id: id,
    subscriptionId: 'subscription-beta17',
    amount: amount,
    currency: 'EUR',
    status: status,
    issueDate: _dateString(date),
  );
}

void _expectZeroMutations(_InvoiceRepository repository) {
  expect(repository.createCalls, 0);
  expect(repository.cancelCalls, 0);
  expect(repository.toggleCalls, 0);
}

Future<void> _ensureVisible(WidgetTester tester, Finder finder) async {
  if (finder.evaluate().isEmpty) {
    await tester.scrollUntilVisible(
      finder,
      240,
      scrollable: find.byType(Scrollable).last,
    );
  }
  if (finder.evaluate().isNotEmpty) {
    await tester.ensureVisible(finder);
  }
  await tester.pumpAndSettle();
}

Future<void> _expectInvoiceVisible(WidgetTester tester, String id) async {
  final finder = find.byKey(Key('subscription-invoice-$id'));
  await _ensureVisible(tester, finder);
  expect(finder, findsOneWidget);
}

Future<void> _expectInvoiceBefore(WidgetTester tester, String firstId, String secondId) async {
  final firstFinder = find.byKey(Key('subscription-invoice-$firstId'));
  final secondFinder = find.byKey(Key('subscription-invoice-$secondId'));
  await _ensureVisible(tester, secondFinder);
  await _ensureVisible(tester, firstFinder);
  final first = tester.getTopLeft(firstFinder).dy;
  final second = tester.getTopLeft(secondFinder).dy;
  expect(first, lessThan(second));
}

Future<void> _selectSort(WidgetTester tester, String label) async {
  final finder = find.byKey(const Key('subscription-invoice-sort'));
  await _ensureVisible(tester, finder);
  await tester.tap(finder);
  await tester.pumpAndSettle();
  await tester.tap(find.text(label).last);
  await tester.pumpAndSettle();
}

Future<void> _selectFromDay(WidgetTester tester, String day) async {
  final finder = find.byKey(const Key('subscription-invoice-date-from'));
  await _ensureVisible(tester, finder);
  await tester.tap(finder);
  await tester.pumpAndSettle();
  expect(find.byType(DatePickerDialog), findsOneWidget);
  await tester.tap(find.text(day).last);
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
}

Future<void> _selectQuickPeriod(WidgetTester tester, String period) async {
  final finder = find.byKey(Key('subscription-invoice-period-$period'));
  await _ensureVisible(tester, finder);
  await tester.tap(finder);
  await tester.pumpAndSettle();
}

Future<void> _selectStatus(WidgetTester tester, String label) async {
  final finder = find.byKey(const Key('subscription-invoice-status-filter'));
  await _ensureVisible(tester, finder);
  await tester.tap(finder);
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
    expect(find.byKey(const Key('subscription-invoice-date-from')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-date-to')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-period-shortcuts')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-results-count')), findsOneWidget);
    expect(find.text('Plus récentes d’abord'), findsOneWidget);
    expect(find.textContaining('Date de début'), findsOneWidget);
    expect(find.textContaining('Date de fin'), findsOneWidget);
    expect(find.text('1 facture trouvée'), findsOneWidget);
    await _expectInvoiceVisible(tester, 'INV-PAID-001');
    expect(find.text('Payée'), findsOneWidget);
    expect(find.text('PAID'), findsNothing);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('advanced filters expose active chips and reset all locally', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), 'inv');
    await _selectStatus(tester, 'En attente');

    final activeFilters = find.byKey(const Key('subscription-invoice-active-filters'));
    await _ensureVisible(tester, activeFilters);
    expect(activeFilters, findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-active-search')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-active-status')), findsOneWidget);
    expect(find.byKey(const Key('subscription-invoice-reset-filters')), findsOneWidget);
    expect(find.text('1 facture trouvée'), findsOneWidget);

    final reset = find.byKey(const Key('subscription-invoice-reset-filters'));
    await tester.tap(reset);
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-invoice-active-filters')), findsNothing);
    await _expectInvoiceVisible(tester, 'INV-PAID-001');
    await _expectInvoiceVisible(tester, 'INV-PENDING-002');
    expect(find.text('2 factures trouvées'), findsOneWidget);
    expect(find.text('inv'), findsNothing);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('quick periods filter locally and all dates restores invoices', (tester) async {
    final recent = _relativeInvoice(id: 'INV-RECENT', daysAgo: 10, status: 'PAID', amount: 10);
    final middle = _relativeInvoice(id: 'INV-MIDDLE', daysAgo: 60, status: 'PENDING', amount: 20);
    final old = _relativeInvoice(id: 'INV-OLD', daysAgo: 120, status: 'PAID', amount: 30);
    final repository = _InvoiceRepository(invoices: [recent, middle, old]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await _selectQuickPeriod(tester, 'last-30-days');
    await _expectInvoiceVisible(tester, 'INV-RECENT');
    expect(find.byKey(const Key('subscription-invoice-INV-MIDDLE')), findsNothing);
    expect(find.byKey(const Key('subscription-invoice-INV-OLD')), findsNothing);
    expect(find.byKey(const Key('subscription-invoice-active-date')), findsOneWidget);
    expect(find.text('1 facture trouvée'), findsOneWidget);

    await _selectQuickPeriod(tester, 'last-90-days');
    await _expectInvoiceVisible(tester, 'INV-RECENT');
    await _expectInvoiceVisible(tester, 'INV-MIDDLE');
    expect(find.byKey(const Key('subscription-invoice-INV-OLD')), findsNothing);
    expect(find.text('2 factures trouvées'), findsOneWidget);

    await _selectQuickPeriod(tester, 'all');
    await _expectInvoiceVisible(tester, 'INV-RECENT');
    await _expectInvoiceVisible(tester, 'INV-MIDDLE');
    await _expectInvoiceVisible(tester, 'INV-OLD');
    expect(find.byKey(const Key('subscription-invoice-active-date')), findsNothing);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('search status quick period and sorting compose without refetch', (tester) async {
    final recentPaid = _relativeInvoice(id: 'INV-MATCH-A', daysAgo: 5, status: 'PAID', amount: 40);
    final recentPaidLower = _relativeInvoice(id: 'INV-MATCH-B', daysAgo: 8, status: 'PAID', amount: 10);
    final recentPending = _relativeInvoice(id: 'INV-MATCH-C', daysAgo: 4, status: 'PENDING', amount: 5);
    final oldPaid = _relativeInvoice(id: 'INV-MATCH-D', daysAgo: 100, status: 'PAID', amount: 1);
    final repository = _InvoiceRepository(invoices: [recentPaid, recentPaidLower, recentPending, oldPaid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), 'match');
    await _selectStatus(tester, 'Payée');
    await _selectQuickPeriod(tester, 'last-30-days');
    await _selectSort(tester, 'Montant : croissant');

    await _expectInvoiceVisible(tester, 'INV-MATCH-A');
    await _expectInvoiceVisible(tester, 'INV-MATCH-B');
    expect(find.byKey(const Key('subscription-invoice-INV-MATCH-C')), findsNothing);
    expect(find.byKey(const Key('subscription-invoice-INV-MATCH-D')), findsNothing);
    await _expectInvoiceBefore(tester, 'INV-MATCH-B', 'INV-MATCH-A');
    expect(find.text('2 factures trouvées'), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice date filter is local inclusive and clears without refetch', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await _selectFromDay(tester, '4');
    expect(find.byKey(const Key('subscription-invoice-INV-PAID-001')), findsNothing);
    await _expectInvoiceVisible(tester, 'INV-PENDING-002');
    final clear = find.byKey(const Key('subscription-invoice-date-clear'));
    await _ensureVisible(tester, clear);
    expect(clear, findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);

    await tester.tap(clear);
    await tester.pumpAndSettle();
    await _expectInvoiceVisible(tester, 'INV-PAID-001');
    await _expectInvoiceVisible(tester, 'INV-PENDING-002');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice date filter combines with status search and sorting', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await _selectFromDay(tester, '4');
    final search = find.byKey(const Key('subscription-invoice-search'));
    await _ensureVisible(tester, search);
    await tester.enterText(search, 'inv');
    await _selectStatus(tester, 'En attente');
    await _selectSort(tester, 'Montant : décroissant');

    expect(find.byKey(const Key('subscription-invoice-INV-PAID-001')), findsNothing);
    await _expectInvoiceVisible(tester, 'INV-PENDING-002');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice date filter exposes localized date-range empty state', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await _selectFromDay(tester, '4');
    await _selectStatus(tester, 'Payée');

    final empty = find.byKey(const Key('subscription-invoices-date-empty'));
    await _ensureVisible(tester, empty);
    expect(empty, findsOneWidget);
    expect(find.text('Aucune facture dans cette période'), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice sorting defaults to newest and supports oldest locally', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await _expectInvoiceBefore(tester, 'INV-PENDING-002', 'INV-PAID-001');

    await _selectSort(tester, 'Plus anciennes d’abord');
    await _expectInvoiceBefore(tester, 'INV-PAID-001', 'INV-PENDING-002');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice sorting supports amount ascending and descending locally', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_pending, _paid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();

    await _selectSort(tester, 'Montant : croissant');
    await _expectInvoiceBefore(tester, 'INV-PAID-001', 'INV-PENDING-002');

    await _selectSort(tester, 'Montant : décroissant');
    await _expectInvoiceBefore(tester, 'INV-PENDING-002', 'INV-PAID-001');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice sorting date and advanced filter labels are localized in English', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid]);
    await tester.pumpWidget(_app(repository, locale: const Locale('en')));
    await tester.pumpAndSettle();
    expect(find.text('Sort invoices'), findsOneWidget);
    expect(find.text('Newest first'), findsOneWidget);
    expect(find.text('Filter by issue date'), findsOneWidget);
    expect(find.textContaining('From date'), findsOneWidget);
    expect(find.textContaining('To date'), findsOneWidget);
    expect(find.text('Quick period:'), findsOneWidget);
    expect(find.text('Last 30 days'), findsOneWidget);
    expect(find.text('Last 90 days'), findsOneWidget);
    expect(find.text('This year'), findsOneWidget);
    expect(find.text('All dates'), findsOneWidget);
    expect(find.text('1 invoice found'), findsOneWidget);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice search is local case insensitive and trims spaces', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), '  paid-001  ');
    await tester.pumpAndSettle();
    await _expectInvoiceVisible(tester, 'INV-PAID-001');
    expect(find.byKey(const Key('subscription-invoice-INV-PENDING-002')), findsNothing);
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice search combines with localized status filter and sorting', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), 'inv');
    await _selectStatus(tester, 'En attente');
    await _selectSort(tester, 'Montant : croissant');
    expect(find.byKey(const Key('subscription-invoice-INV-PAID-001')), findsNothing);
    await _expectInvoiceVisible(tester, 'INV-PENDING-002');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('clearing invoice search restores status-filtered results', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid, _pending]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    final search = find.byKey(const Key('subscription-invoice-search'));
    await tester.enterText(search, 'missing');
    await tester.pumpAndSettle();
    final empty = find.byKey(const Key('subscription-invoices-search-empty'));
    await _ensureVisible(tester, empty);
    expect(empty, findsOneWidget);
    await _ensureVisible(tester, search);
    await tester.enterText(search, '');
    await tester.pumpAndSettle();
    await _expectInvoiceVisible(tester, 'INV-PAID-001');
    await _expectInvoiceVisible(tester, 'INV-PENDING-002');
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice search exposes localized no-result state', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('subscription-invoice-search')), 'unknown');
    await tester.pumpAndSettle();
    final empty = find.byKey(const Key('subscription-invoices-search-empty'));
    await _ensureVisible(tester, empty);
    expect(empty, findsOneWidget);
    expect(find.text('Aucune facture trouvée'), findsAtLeastNWidgets(1));
    expect(repository.fetchInvoicesCalls, 1);
    _expectZeroMutations(repository);
  });

  testWidgets('invoice history opens invoice detail and returns without mutations', (tester) async {
    final repository = _InvoiceRepository(invoices: const [_paid]);
    await tester.pumpWidget(_app(repository));
    await tester.pumpAndSettle();
    final invoice = find.byKey(const Key('subscription-invoice-open-INV-PAID-001'));
    await _ensureVisible(tester, invoice);
    await tester.tap(invoice);
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
