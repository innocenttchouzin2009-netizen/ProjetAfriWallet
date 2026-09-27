import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/p2p_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('P2PRemoteDataSource', () {
    test('posts the exact AfWal ID P2P transfer backend contract', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'transferId': 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
              'sourceWalletId': '11111111-1111-1111-1111-111111111111',
              'targetWalletId': '22222222-2222-2222-2222-222222222222',
              'currencyCode': 'XAF',
              'amountMinor': 2500,
              'correlationId': '33333333-3333-3333-3333-333333333333',
              'createdAtUtc': '2026-09-27T12:00:00Z',
              'recipientKind': 'afwal-id',
            }),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      final response = await dataSource.executeTransfer(
        'access-secret',
        const P2PTransferRequest(
          sourceWalletId: '11111111-1111-1111-1111-111111111111',
          recipientKind: P2PRecipientKind.afWalId,
          recipientValue: '@receiver',
          currencyCode: 'XAF',
          amountMinor: 2500,
          correlationId: '33333333-3333-3333-3333-333333333333',
        ),
      );

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/p2p/transfers');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(jsonDecode(captured.body), <String, Object?>{
        'sourceWalletId': '11111111-1111-1111-1111-111111111111',
        'recipientKind': 'afwal-id',
        'recipientValue': '@receiver',
        'currencyCode': 'XAF',
        'amountMinor': 2500,
        'correlationId': '33333333-3333-3333-3333-333333333333',
      });
      expect(response.transferId, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
      expect(response.targetWalletId, '22222222-2222-2222-2222-222222222222');
      expect(response.currencyCode, 'XAF');
      expect(response.amountMinor, 2500);
      expect(response.recipientKind, P2PRecipientKind.afWalId);
      expect(response.createdAtUtc, DateTime.parse('2026-09-27T12:00:00Z'));

      apiClient.close();
    });

    test('uses the exact qr recipient kind wire value', () async {
      late Map<String, dynamic> posted;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          posted = jsonDecode(request.body) as Map<String, dynamic>;
          return http.Response(
            jsonEncode(<String, Object?>{
              'transferId': 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
              'sourceWalletId': '11111111-1111-1111-1111-111111111111',
              'targetWalletId': '22222222-2222-2222-2222-222222222222',
              'currencyCode': 'EUR',
              'amountMinor': 990,
              'correlationId': '33333333-3333-3333-3333-333333333333',
              'createdAtUtc': '2026-09-27T12:00:00+00:00',
              'recipientKind': 'qr',
            }),
            200,
          );
        }),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      final response = await dataSource.executeTransfer(
        'access-secret',
        const P2PTransferRequest(
          sourceWalletId: '11111111-1111-1111-1111-111111111111',
          recipientKind: P2PRecipientKind.qr,
          recipientValue: 'qr-token-value',
          currencyCode: 'EUR',
          amountMinor: 990,
          correlationId: '33333333-3333-3333-3333-333333333333',
        ),
      );

      expect(posted['recipientKind'], 'qr');
      expect(posted['recipientValue'], 'qr-token-value');
      expect(response.recipientKind, P2PRecipientKind.qr);

      apiClient.close();
    });

    test('posts receive identity contract with bearer token and no body', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'publicLabel': '@afwal-owner',
              'qrToken': 'backend-issued-qr-token',
            }),
            200,
          );
        }),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      final identity = await dataSource.issueReceiveIdentity('access-secret');

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/p2p/receive-identity');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(captured.body, isEmpty);
      expect(identity.publicLabel, '@afwal-owner');
      expect(identity.qrToken, 'backend-issued-qr-token');

      apiClient.close();
    });

    test('rejects malformed successful transfer responses', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            '{"transferId":"only-one-field"}',
            200,
          ),
        ),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      await expectLater(
        dataSource.executeTransfer(
          'access-secret',
          const P2PTransferRequest(
            sourceWalletId: '11111111-1111-1111-1111-111111111111',
            recipientKind: P2PRecipientKind.afWalId,
            recipientValue: '@receiver',
            currencyCode: 'XAF',
            amountMinor: 1,
            correlationId: '33333333-3333-3333-3333-333333333333',
          ),
        ),
        throwsA(isA<ApiMalformedResponseException>()),
      );

      apiClient.close();
    });

    test('rejects malformed receive identity responses', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response('{"publicLabel":"@owner"}', 200),
        ),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      await expectLater(
        dataSource.issueReceiveIdentity('access-secret'),
        throwsA(isA<ApiMalformedResponseException>()),
      );

      apiClient.close();
    });

    test('propagates unauthorized P2P responses without masking them', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      await expectLater(
        dataSource.issueReceiveIdentity('expired-token'),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      apiClient.close();
    });

    test('propagates recipient not found responses without masking them', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'code': 'P2P_RECIPIENT_NOT_FOUND',
              'message': 'Recipient was not found for the requested currency.',
              'traceId': 'trace-1',
            }),
            404,
          ),
        ),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      await expectLater(
        dataSource.executeTransfer(
          'access-secret',
          const P2PTransferRequest(
            sourceWalletId: '11111111-1111-1111-1111-111111111111',
            recipientKind: P2PRecipientKind.afWalId,
            recipientValue: '@missing',
            currencyCode: 'XAF',
            amountMinor: 100,
            correlationId: '33333333-3333-3333-3333-333333333333',
          ),
        ),
        throwsA(isA<ApiNotFoundException>()),
      );

      apiClient.close();
    });

    test('propagates unavailable recipient providers as server errors', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 503)),
      );
      final dataSource = P2PRemoteDataSource(apiClient);

      await expectLater(
        dataSource.issueReceiveIdentity('access-secret'),
        throwsA(
          isA<ApiServerException>().having(
            (error) => error.statusCode,
            'statusCode',
            503,
          ),
        ),
      );

      apiClient.close();
    });
  });
}
