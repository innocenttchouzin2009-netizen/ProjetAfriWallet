import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/pages/transaction_history_page.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';

typedef _PageLoader = Future<TransactionHistoryPageResult> Function({
  int? limit,
  String? cursor,
});

TransactionHistoryItem _item({
  required String id,
  String? counterpartyLabel,
  int amountMinor = 1250,
}) =>
    TransactionHistoryItem(
      transactionId: id,
      amountMinor: amountMinor,
      currencyCode: 'EUR',
      direction: TransactionDirection.outgoing,
      status: TransactionHistoryStatus.completed,
      occurredAt: DateTime.utc(2026, 9, 1, 8),
      reference: 'PI-$id',
      counterpartyLabel: counterpartyLabel,
    );

class _PaginatedRepository implements PaginatedTransactionHistoryRepository {
  _PaginatedRepository(this._loader);

  final _PageLoader _loader;

  @override
  Future<TransactionHistoryPageResult> listTransactionPage({
    int? limit,
    String? cursor,
  }) =>
      _loader(limit: limit, cursor: cursor);

  @override
  Future<List<TransactionHistoryItem>> listTransactions() async =>
      (await listTransactionPage()).items;
}

class FakeTransactionHistoryRepository extends _PaginatedRepository {
  FakeTransactionHistoryRepository()
      : super(
          ({int? limit, String? cursor}) async =>
              TransactionHistoryPageResult(
            items: <TransactionHistoryItem>[
              _item(id: 'TX-001', counterpartyLabel: '@receiver'),
            ],
            nextCursor: null,
          ),
        );
}

class EmptyTransactionHistoryRepository extends _PaginatedRepository {
  EmptyTransactionHistoryRepository()
      : super(
          ({int? limit, String? cursor}) async =>
              const TransactionHistoryPageResult(
            items: <TransactionHistoryItem>[],
            nextCursor: null,
          ),
        );
}

void main() {
  testWidgets('shows initial loading before the first page resolves', (
    tester,
  ) async {
    final firstPage = Completer<TransactionHistoryPageResult>();
    final repository = _PaginatedRepository(
      ({int? limit, String? cursor}) => firstPage.future,
    );

    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryPage(
          repository: repository,
          onReturnToWallet: () {},
        ),
      ),
    );
    await tester.pump();

    expect(
      find.byKey(const Key('transaction-history-initial-loading')),
      findsOneWidget,
    );

    firstPage.complete(
      TransactionHistoryPageResult(
        items: <TransactionHistoryItem>[_item(id: 'TX-INITIAL')],
        nextCursor: null,
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('PI-TX-INITIAL'), findsOneWidget);
  });

  testWidgets('renders repository transaction and details', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryPage(
          repository: FakeTransactionHistoryRepository(),
          onReturnToWallet: () {},
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Financial Timeline'), findsOneWidget);
    expect(find.text('@receiver'), findsOneWidget);
    expect(find.text('-12.50 EUR'), findsOneWidget);
    expect(find.text('Terminée'), findsOneWidget);
    expect(
      find.byKey(const Key('transaction-history-end')),
      findsOneWidget,
    );

    await tester.tap(find.text('@receiver'));
    await tester.pumpAndSettle();
    expect(find.text('Détail de la transaction'), findsOneWidget);
    expect(find.text('Référence : PI-TX-001'), findsOneWidget);
    expect(find.text('ID : TX-001'), findsOneWidget);
  });

  testWidgets('loads more and exposes the end of the list', (tester) async {
    final nextPage = Completer<TransactionHistoryPageResult>();
    final repository = _PaginatedRepository(
      ({int? limit, String? cursor}) {
        if (cursor == null) {
          return Future<TransactionHistoryPageResult>.value(
            TransactionHistoryPageResult(
              items: <TransactionHistoryItem>[_item(id: 'TX-1')],
              nextCursor: 'cursor-1',
            ),
          );
        }
        return nextPage.future;
      },
    );

    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryPage(
          repository: repository,
          onReturnToWallet: () {},
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('transaction-history-load-more')),
      findsOneWidget,
    );

    await tester.tap(
      find.byKey(const Key('transaction-history-load-more')),
    );
    await tester.pump();

    expect(
      find.byKey(const Key('transaction-history-load-more-loading')),
      findsOneWidget,
    );
    expect(find.text('PI-TX-1'), findsOneWidget);

    nextPage.complete(
      TransactionHistoryPageResult(
        items: <TransactionHistoryItem>[_item(id: 'TX-2')],
        nextCursor: null,
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('PI-TX-1'), findsOneWidget);
    expect(find.text('PI-TX-2'), findsOneWidget);
    expect(
      find.byKey(const Key('transaction-history-end')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('transaction-history-load-more')),
      findsNothing,
    );
  });

  testWidgets('pagination error preserves items and can retry', (tester) async {
    var attempts = 0;
    final repository = _PaginatedRepository(
      ({int? limit, String? cursor}) async {
        if (cursor == null) {
          return TransactionHistoryPageResult(
            items: <TransactionHistoryItem>[_item(id: 'TX-1')],
            nextCursor: 'cursor-1',
          );
        }
        attempts++;
        if (attempts == 1) {
          throw StateError('page unavailable');
        }
        return TransactionHistoryPageResult(
          items: <TransactionHistoryItem>[_item(id: 'TX-2')],
          nextCursor: null,
        );
      },
    );

    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryPage(
          repository: repository,
          onReturnToWallet: () {},
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(
      find.byKey(const Key('transaction-history-load-more')),
    );
    await tester.pumpAndSettle();

    expect(find.text('PI-TX-1'), findsOneWidget);
    expect(
      find.byKey(const Key('transaction-history-pagination-error')),
      findsOneWidget,
    );

    await tester.tap(
      find.byKey(const Key('transaction-history-pagination-retry')),
    );
    await tester.pumpAndSettle();

    expect(find.text('PI-TX-1'), findsOneWidget);
    expect(find.text('PI-TX-2'), findsOneWidget);
    expect(
      find.byKey(const Key('transaction-history-end')),
      findsOneWidget,
    );
  });

  testWidgets('returns to wallet from populated Financial Timeline', (
    tester,
  ) async {
    var returnCount = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryPage(
          repository: FakeTransactionHistoryRepository(),
          onReturnToWallet: () => returnCount++,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(
      find.byKey(const Key('return-to-wallet-history-list')),
    );
    await tester.pump();

    expect(returnCount, 1);
  });

  testWidgets('returns to wallet from empty Financial Timeline', (
    tester,
  ) async {
    var returnCount = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryPage(
          repository: EmptyTransactionHistoryRepository(),
          onReturnToWallet: () => returnCount++,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Aucune transaction'), findsOneWidget);
    await tester.tap(
      find.byKey(const Key('return-to-wallet-history-empty')),
    );
    await tester.pump();

    expect(returnCount, 1);
  });

  testWidgets(
    'unavailable repository never fabricates history and can return to wallet',
    (tester) async {
      var returnCount = 0;
      await tester.pumpWidget(
        MaterialApp(
          home: TransactionHistoryPage(
            repository: const UnavailableTransactionHistoryRepository(),
            onReturnToWallet: () => returnCount++,
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Historique indisponible'), findsOneWidget);
      expect(
        find.textContaining('Aucune transaction n’est simulée'),
        findsOneWidget,
      );

      await tester.tap(
        find.byKey(const Key('return-to-wallet-history-error')),
      );
      await tester.pump();

      expect(returnCount, 1);
    },
  );

  testWidgets('preserves optional legacy continuation', (tester) async {
    var continueCount = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: TransactionHistoryPage(
          repository: FakeTransactionHistoryRepository(),
          onReturnToWallet: () {},
          onContinue: () => continueCount++,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Continuer'));
    await tester.pump();

    expect(continueCount, 1);
  });
}
