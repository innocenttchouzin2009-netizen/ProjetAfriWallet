import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/transaction_history_contracts.dart';
import 'package:mobile_app/data/remote/transaction_history_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('TransactionHistoryRemoteDataSource', () {
    test('reads the exact authenticated transaction history backend contract',
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
                  'transactionId':
                      'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
                  'walletId': '11111111-1111-1111-1111-111111111111',
                  'amountMinor': 2500,
                  'currencyCode': 'EUR',
                  'direction': 'Incoming',
                  'status': 'Completed',
                  'occurredAtUtc': '2026-10-02T10:00:00+00:00',
                  'reference': 'TX-001',
                  'counterpartyLabel': 'Counterparty',
                },
              ],
              'nextCursor': 'opaque-cursor-value',
            }),
            200,
          );
        }),
      );
      final source = TransactionHistoryRemoteDataSource(apiClient);

      final page = await source.listTransactions(
        'access-secret',
        limit: 25,
        cursor: 'cursor-input',
      );

      expect(captured.method, 'GET');
      expect(captured.url.path, '/api/v1/transactions');
      expect(captured.url.queryParameters, <String, String>{
        'limit': '25',
        'cursor': 'cursor-input',
      });
      expect(captured.headers['Authorization'], 'Bearer access-secret');

      expect(page.items, hasLength(1));
      expect(
        page.items.single.transactionId,
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      );
      expect(
        page.items.single.walletId,
        '11111111-1111-1111-1111-111111111111',
      );
      expect(page.items.single.amountMinor, 2500);
      expect(page.items.single.currencyCode, 'EUR');
      expect(
        page.items.single.direction,
        TransactionHistoryRemoteDirection.incoming,
      );
      expect(
        page.items.single.status,
        TransactionHistoryRemoteStatus.completed,
      );
      expect(
        page.items.single.occurredAtUtc,
        DateTime.parse('2026-10-02T10:00:00Z'),
      );
      expect(page.items.single.reference, 'TX-001');
      expect(page.items.single.counterpartyLabel, 'Counterparty');
      expect(page.nextCursor, 'opaque-cursor-value');

      apiClient.close();
    });

    test('accepts the final page with a null next cursor', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'items': <Object?>[],
              'nextCursor': null,
            }),
            200,
          ),
        ),
      );
      final source = TransactionHistoryRemoteDataSource(apiClient);

      final page = await source.listTransactions('access-secret');

      expect(page.items, isEmpty);
      expect(page.nextCursor, isNull);

      apiClient.close();
    });

    test('maps outgoing and reversed wire values exactly', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'items': <Object?>[
                <String, Object?>{
                  'transactionId':
                      'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
                  'walletId': '22222222-2222-2222-2222-222222222222',
                  'amountMinor': 10,
                  'currencyCode': 'XAF',
                  'direction': 'Outgoing',
                  'status': 'Reversed',
                  'occurredAtUtc': '2026-10-02T09:00:00Z',
                  'reference': 'TX-002',
                  'counterpartyLabel': null,
                },
              ],
              'nextCursor': null,
            }),
            200,
          ),
        ),
      );
      final source = TransactionHistoryRemoteDataSource(apiClient);

      final page = await source.listTransactions('access-secret');

      expect(
        page.items.single.direction,
        TransactionHistoryRemoteDirection.outgoing,
      );
      expect(
        page.items.single.status,
        TransactionHistoryRemoteStatus.reversed,
      );
      expect(page.items.single.counterpartyLabel, isNull);

      apiClient.close();
    });

    test('rejects malformed successful transaction history responses',
        () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'items': <Object?>[
                <String, Object?>{
                  'transactionId':
                      'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
                  'walletId': '11111111-1111-1111-1111-111111111111',
                  'amountMinor': 2500,
                  'currencyCode': 'EUR',
                  'direction': 'UnsupportedDirection',
                  'status': 'Completed',
                  'occurredAtUtc': '2026-10-02T10:00:00Z',
                  'reference': 'TX-001',
                  'counterpartyLabel': null,
                },
              ],
              'nextCursor': null,
            }),
            200,
          ),
        ),
      );
      final source = TransactionHistoryRemoteDataSource(apiClient);

      await expectLater(
        source.listTransactions('access-secret'),
        throwsA(isA<ApiMalformedResponseException>()),
      );

      apiClient.close();
    });

    test('propagates unauthorized responses without masking them', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final source = TransactionHistoryRemoteDataSource(apiClient);

      await expectLater(
        source.listTransactions('expired-token'),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      apiClient.close();
    });
  });
}
