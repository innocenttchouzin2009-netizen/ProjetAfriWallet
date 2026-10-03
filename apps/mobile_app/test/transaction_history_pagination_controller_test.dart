import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/presentation/transaction_history_pagination_controller.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';

typedef _PageLoader = Future<TransactionHistoryPageResult> Function({
  int? limit,
  String? cursor,
});

class _FakePaginatedRepository
    implements PaginatedTransactionHistoryRepository {
  _FakePaginatedRepository(this._loader);

  final _PageLoader _loader;
  final List<int?> limits = <int?>[];
  final List<String?> cursors = <String?>[];

  int get callCount => cursors.length;

  @override
  Future<TransactionHistoryPageResult> listTransactionPage({
    int? limit,
    String? cursor,
  }) {
    limits.add(limit);
    cursors.add(cursor);
    return _loader(limit: limit, cursor: cursor);
  }

  @override
  Future<List<TransactionHistoryItem>> listTransactions() async =>
      (await listTransactionPage()).items;
}

TransactionHistoryItem _item(String id) => TransactionHistoryItem(
      transactionId: id,
      amountMinor: 100,
      currencyCode: 'EUR',
      direction: TransactionDirection.incoming,
      status: TransactionHistoryStatus.completed,
      occurredAt: DateTime.utc(2026, 10, 2, 20),
      reference: 'REF-$id',
      counterpartyLabel: null,
    );

void main() {
  test('initial load exposes items and hasMore from the first page', () async {
    final repository = _FakePaginatedRepository(
      ({int? limit, String? cursor}) async => TransactionHistoryPageResult(
        items: <TransactionHistoryItem>[_item('TX-1')],
        nextCursor: 'cursor-1',
      ),
    );
    final controller = TransactionHistoryPaginationController(
      repository: repository,
    );

    await controller.loadInitial();

    expect(controller.items.map((item) => item.transactionId), <String>['TX-1']);
    expect(controller.isInitialLoading, isFalse);
    expect(controller.isLoadingMore, isFalse);
    expect(controller.hasMore, isTrue);
    expect(controller.initialError, isNull);
    expect(controller.paginationError, isNull);
    expect(repository.limits, <int?>[25]);
    expect(repository.cursors, <String?>[null]);
  });

  test('load more appends items and stops when nextCursor is null', () async {
    final repository = _FakePaginatedRepository(
      ({int? limit, String? cursor}) async {
        if (cursor == null) {
          return TransactionHistoryPageResult(
            items: <TransactionHistoryItem>[_item('TX-1')],
            nextCursor: 'cursor-1',
          );
        }
        return TransactionHistoryPageResult(
          items: <TransactionHistoryItem>[_item('TX-2')],
          nextCursor: null,
        );
      },
    );
    final controller = TransactionHistoryPaginationController(
      repository: repository,
    );

    await controller.loadInitial();
    await controller.loadMore();

    expect(
      controller.items.map((item) => item.transactionId),
      <String>['TX-1', 'TX-2'],
    );
    expect(controller.hasMore, isFalse);
    expect(repository.cursors, <String?>[null, 'cursor-1']);

    await controller.loadMore();
    expect(repository.callCount, 2);
  });

  test('load more ignores duplicate concurrent requests', () async {
    final loadMoreCompleter = Completer<TransactionHistoryPageResult>();
    final repository = _FakePaginatedRepository(
      ({int? limit, String? cursor}) {
        if (cursor == null) {
          return Future<TransactionHistoryPageResult>.value(
            TransactionHistoryPageResult(
              items: <TransactionHistoryItem>[_item('TX-1')],
              nextCursor: 'cursor-1',
            ),
          );
        }
        return loadMoreCompleter.future;
      },
    );
    final controller = TransactionHistoryPaginationController(
      repository: repository,
    );

    await controller.loadInitial();
    final firstLoadMore = controller.loadMore();
    final duplicateLoadMore = controller.loadMore();

    expect(controller.isLoadingMore, isTrue);
    expect(repository.callCount, 2);

    loadMoreCompleter.complete(
      TransactionHistoryPageResult(
        items: <TransactionHistoryItem>[_item('TX-2')],
        nextCursor: null,
      ),
    );
    await Future.wait(<Future<void>>[firstLoadMore, duplicateLoadMore]);

    expect(repository.callCount, 2);
    expect(
      controller.items.map((item) => item.transactionId),
      <String>['TX-1', 'TX-2'],
    );
  });

  test('pagination failure preserves loaded items and remains retryable', () async {
    var loadMoreAttempts = 0;
    final repository = _FakePaginatedRepository(
      ({int? limit, String? cursor}) async {
        if (cursor == null) {
          return TransactionHistoryPageResult(
            items: <TransactionHistoryItem>[_item('TX-1')],
            nextCursor: 'cursor-1',
          );
        }
        loadMoreAttempts++;
        if (loadMoreAttempts == 1) {
          throw StateError('page unavailable');
        }
        return TransactionHistoryPageResult(
          items: <TransactionHistoryItem>[_item('TX-2')],
          nextCursor: null,
        );
      },
    );
    final controller = TransactionHistoryPaginationController(
      repository: repository,
    );

    await controller.loadInitial();
    await controller.loadMore();

    expect(
      controller.items.map((item) => item.transactionId),
      <String>['TX-1'],
    );
    expect(controller.initialError, isNull);
    expect(controller.paginationError, isA<StateError>());
    expect(controller.hasMore, isTrue);

    await controller.loadMore();

    expect(controller.paginationError, isNull);
    expect(
      controller.items.map((item) => item.transactionId),
      <String>['TX-1', 'TX-2'],
    );
    expect(controller.hasMore, isFalse);
  });

  test('initial failure is isolated from pagination failure state', () async {
    final repository = _FakePaginatedRepository(
      ({int? limit, String? cursor}) async {
        throw StateError('initial unavailable');
      },
    );
    final controller = TransactionHistoryPaginationController(
      repository: repository,
    );

    await controller.loadInitial();

    expect(controller.items, isEmpty);
    expect(controller.hasMore, isFalse);
    expect(controller.initialError, isA<StateError>());
    expect(controller.paginationError, isNull);
    expect(controller.isInitialLoading, isFalse);
  });
}
