import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/transaction_history_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';

void main() {
  group('AuthenticatedTransactionHistoryRepository', () {
    test(
      'loads and maps transaction history with the stored access token',
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
                'nextCursor': null,
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }),
        );
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          _FakeAuthSessionStore(_storedSession()),
        );

        final transactions = await repository.listTransactions();

        expect(captured.headers['Authorization'], 'Bearer access-secret');
        expect(transactions, hasLength(1));
        final transaction = transactions.single;
        expect(transaction.transactionId, 'txn-1');
        expect(transaction.amountMinor, 125000);
        expect(transaction.currencyCode, 'XAF');
        expect(transaction.direction, TransactionDirection.outgoing);
        expect(transaction.status, TransactionHistoryStatus.reversed);
        expect(transaction.occurredAt, DateTime.utc(2026, 10, 2, 12, 30));
        expect(transaction.reference, 'AFW-REF-1');
        expect(transaction.counterpartyLabel, 'Merchant A');
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
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          _FakeAuthSessionStore(null),
        );

        await expectLater(
          repository.listTransactions(),
          throwsA(isA<TransactionHistoryUnavailableException>()),
        );
        expect(remoteCalled, isFalse);
        apiClient.close();
      },
    );

    test(
      'preserves unauthorized responses from the transaction history endpoint',
      () async {
        final apiClient = ApiClient(
          baseUrl: 'https://api.afwal.test',
          httpClient: MockClient((_) async => http.Response('', 401)),
        );
        final repository = AuthenticatedTransactionHistoryRepository(
          TransactionHistoryRemoteDataSource(apiClient),
          _FakeAuthSessionStore(_storedSession()),
        );

        await expectLater(
          repository.listTransactions(),
          throwsA(isA<ApiUnauthorizedException>()),
        );
        apiClient.close();
      },
    );
  });
}

StoredAuthSession _storedSession() => StoredAuthSession(
      accessToken: 'access-secret',
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 10, 2, 13),
    );

class _FakeAuthSessionStore implements AuthSessionStore {
  _FakeAuthSessionStore(this.session);

  StoredAuthSession? session;

  @override
  Future<StoredAuthSession?> read() async => session;

  @override
  Future<void> save(StoredAuthSession session) async {
    this.session = session;
  }

  @override
  Future<void> clear() async {
    session = null;
  }
}
