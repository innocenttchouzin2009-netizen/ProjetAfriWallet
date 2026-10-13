import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/transaction_history_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/authenticated_request_executor.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';

void main() {
  group('AuthenticatedTransactionHistoryRepository', () {
    test(
      'propagates pagination and maps the remote page with the current access token',
      () async {
        late http.Request captured;
        final apiClient = ApiClient(
          baseUrl: 'https://api.afwal.test',
          httpClient: MockClient((request) async {
            captured = request;
            return http.Response(
              jsonEncode(<String, Object?>{
                'items': <Object?>[
                  <String, Object?>{
                    'transactionId': 'txn-1',
                    'walletId': 'wallet-1',
                    'amountMinor': 125000,
                    'currencyCode': 'XAF',
                    'direction': 'Outgoing',
                    'status': 'Reversed',
                    'occurredAtUtc': '2026-10-02T12:30:00Z',
                    'reference': 'AFW-REF-1',
                    'counterpartyLabel': 'Merchant A',
                  },
                ],
                'nextCursor': 'cursor-next',
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }),
        );
        final lifecycle = _FakeAuthSessionLifecycle(
          restoredSession: _storedSession('access-secret'),
        );
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          AuthenticatedRequestExecutor(lifecycle),
        );

        final page = await repository.listTransactionPage(
          limit: 25,
          cursor: 'cursor-input',
        );

        expect(captured.headers['Authorization'], 'Bearer access-secret');
        expect(captured.url.queryParameters, <String, String>{
          'limit': '25',
          'cursor': 'cursor-input',
        });
        expect(page.nextCursor, 'cursor-next');
        expect(page.items, hasLength(1));
        final transaction = page.items.single;
        expect(transaction.transactionId, 'txn-1');
        expect(transaction.amountMinor, 125000);
        expect(transaction.currencyCode, 'XAF');
        expect(transaction.direction, TransactionDirection.outgoing);
        expect(transaction.status, TransactionHistoryStatus.reversed);
        expect(transaction.occurredAt, DateTime.utc(2026, 10, 2, 12, 30));
        expect(transaction.reference, 'AFW-REF-1');
        expect(transaction.counterpartyLabel, 'Merchant A');
        expect(lifecycle.restoreCalls, 1);
        expect(lifecycle.refreshCalls, 0);
        apiClient.close();
      },
    );

    test(
      'keeps the legacy non-paginated list contract compatible',
      () async {
        final apiClient = ApiClient(
          baseUrl: 'https://api.afwal.test',
          httpClient: MockClient(
            (_) async => http.Response(
              jsonEncode(<String, Object?>{
                'items': <Object?>[
                  <String, Object?>{
                    'transactionId': 'txn-legacy',
                    'walletId': 'wallet-1',
                    'amountMinor': 100,
                    'currencyCode': 'EUR',
                    'direction': 'Incoming',
                    'status': 'Completed',
                    'occurredAtUtc': '2026-10-02T12:00:00Z',
                    'reference': 'AFW-LEGACY',
                    'counterpartyLabel': null,
                  },
                ],
                'nextCursor': 'ignored-by-legacy-contract',
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            ),
          ),
        );
        final lifecycle = _FakeAuthSessionLifecycle(
          restoredSession: _storedSession('access-secret'),
        );
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          AuthenticatedRequestExecutor(lifecycle),
        );

        final transactions = await repository.listTransactions();

        expect(transactions, hasLength(1));
        expect(transactions.single.transactionId, 'txn-legacy');
        apiClient.close();
      },
    );

    test(
      'rejects transaction history reads when no authenticated session exists',
      () async {
        var remoteCalled = false;
        final apiClient = ApiClient(
          baseUrl: 'https://api.afwal.test',
          httpClient: MockClient((_) async {
            remoteCalled = true;
            return http.Response('{"items":[],"nextCursor":null}', 200);
          }),
        );
        final lifecycle = _FakeAuthSessionLifecycle();
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          AuthenticatedRequestExecutor(lifecycle),
        );

        await expectLater(
          repository.listTransactions(),
          throwsA(isA<TransactionHistoryUnavailableException>()),
        );
        expect(remoteCalled, isFalse);
        expect(lifecycle.restoreCalls, 1);
        expect(lifecycle.refreshCalls, 0);
        apiClient.close();
      },
    );

    test(
      'refreshes once after 401 and retries transaction history read',
      () async {
        final authorizationHeaders = <String?>[];
        final apiClient = ApiClient(
          baseUrl: 'https://api.afwal.test',
          httpClient: MockClient((request) async {
            final authorization = request.headers['Authorization'];
            authorizationHeaders.add(authorization);

            if (authorization == 'Bearer access-secret') {
              return http.Response('', 401);
            }

            return http.Response(
              '{"items":[],"nextCursor":null}',
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }),
        );
        final lifecycle = _FakeAuthSessionLifecycle(
          restoredSession: _storedSession('access-secret'),
          refreshedSession: _storedSession('refreshed-access-secret'),
        );
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          AuthenticatedRequestExecutor(lifecycle),
        );

        final page = await repository.listTransactionPage(limit: 25);

        expect(page.items, isEmpty);
        expect(
          authorizationHeaders,
          <String?>[
            'Bearer access-secret',
            'Bearer refreshed-access-secret',
          ],
        );
        expect(lifecycle.restoreCalls, 1);
        expect(lifecycle.refreshCalls, 1);
        apiClient.close();
      },
    );

    test(
      'maps unrecoverable unauthorized responses to transaction history unavailable',
      () async {
        final apiClient = ApiClient(
          baseUrl: 'https://api.afwal.test',
          httpClient: MockClient((_) async => http.Response('', 401)),
        );
        final lifecycle = _FakeAuthSessionLifecycle(
          restoredSession: _storedSession('access-secret'),
        );
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          AuthenticatedRequestExecutor(lifecycle),
        );

        await expectLater(
          repository.listTransactionPage(limit: 25),
          throwsA(isA<TransactionHistoryUnavailableException>()),
        );
        expect(lifecycle.restoreCalls, 1);
        expect(lifecycle.refreshCalls, 1);
        apiClient.close();
      },
    );
  });
}

StoredAuthSession _storedSession(String accessToken) => StoredAuthSession(
      accessToken: accessToken,
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 10, 13, 18),
    );

class _FakeAuthSessionLifecycle implements AuthSessionLifecycle {
  _FakeAuthSessionLifecycle({
    this.restoredSession,
    this.refreshedSession,
  });

  StoredAuthSession? restoredSession;
  StoredAuthSession? refreshedSession;
  int restoreCalls = 0;
  int refreshCalls = 0;
  int clearCalls = 0;

  @override
  Future<StoredAuthSession?> restoreValidSession() async {
    restoreCalls += 1;
    return restoredSession;
  }

  @override
  Future<StoredAuthSession?> refreshSession() async {
    refreshCalls += 1;
    return refreshedSession;
  }

  @override
  Future<void> clearLocalSession() async {
    clearCalls += 1;
  }
}
