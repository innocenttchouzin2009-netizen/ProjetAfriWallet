import 'package:flutter/foundation.dart';

import '../models/transaction_history.dart';
import '../services/transaction_history_repository.dart';

class TransactionHistoryPaginationController extends ChangeNotifier {
  factory TransactionHistoryPaginationController({
    required PaginatedTransactionHistoryRepository repository,
    int pageSize = 25,
  }) {
    assert(pageSize > 0);
    return TransactionHistoryPaginationController._(repository, pageSize);
  }

  TransactionHistoryPaginationController._(this._repository, this.pageSize);

  final PaginatedTransactionHistoryRepository _repository;
  final int pageSize;

  List<TransactionHistoryItem> _items = const <TransactionHistoryItem>[];
  String? _nextCursor;
  bool _hasLoadedInitial = false;
  bool _isInitialLoading = false;
  bool _isLoadingMore = false;
  Object? _initialError;
  Object? _paginationError;

  List<TransactionHistoryItem> get items =>
      List<TransactionHistoryItem>.unmodifiable(_items);
  bool get isInitialLoading => _isInitialLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasLoadedInitial && _nextCursor != null;
  Object? get initialError => _initialError;
  Object? get paginationError => _paginationError;

  Future<void> loadInitial() async {
    if (_isInitialLoading || _isLoadingMore) {
      return;
    }
    if (_hasLoadedInitial && _initialError == null) {
      return;
    }

    _isInitialLoading = true;
    _initialError = null;
    _paginationError = null;
    notifyListeners();

    try {
      final page = await _repository.listTransactionPage(limit: pageSize);
      _items = List<TransactionHistoryItem>.unmodifiable(page.items);
      _nextCursor = page.nextCursor;
      _hasLoadedInitial = true;
    } catch (error) {
      _initialError = error;
      if (!_hasLoadedInitial) {
        _items = const <TransactionHistoryItem>[];
        _nextCursor = null;
      }
    } finally {
      _isInitialLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMore() async {
    if (!_hasLoadedInitial ||
        _isInitialLoading ||
        _isLoadingMore ||
        !hasMore) {
      return;
    }

    final cursor = _nextCursor;
    if (cursor == null) {
      return;
    }

    _isLoadingMore = true;
    _paginationError = null;
    notifyListeners();

    try {
      final page = await _repository.listTransactionPage(
        limit: pageSize,
        cursor: cursor,
      );
      _items = List<TransactionHistoryItem>.unmodifiable(
        <TransactionHistoryItem>[..._items, ...page.items],
      );
      _nextCursor = page.nextCursor;
    } catch (error) {
      _paginationError = error;
    } finally {
      _isLoadingMore = false;
      notifyListeners();
    }
  }
}
