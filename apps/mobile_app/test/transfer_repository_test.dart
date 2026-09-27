import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/p2p_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/payment_transfer.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/transfer_repository.dart';

void main() {
  group('AuthenticatedTransferRepository', () {
    test('sends an AfWal ID transfer with the explicit source wallet', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'transferId': '11111111-1111-1111-1111-111111111111',
              'sourceWalletId': '22222222-2222-2222-2222-222222222222',
              'targetWalletId': '33333333-3333-3333-3333-333333333333',
              'currencyCode': 'XAF',
              'amountMinor': 2500,
              'correlationId': '44444444-4444-4444-4444-444444444444',
              'createdAtUtc': '2026-09-27T13:30:00Z',
              'recipientKind': 'afwal-id',
            }),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );
      final repository = AuthenticatedTransferRepository(
        P2PRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );
      const request = SendTransferRequest(
        sourceWalletId: '22222222-2222-2222-2222-222222222222',
        payeeId: '@recipient',
        amountMinor: 2500,
        currencyCode: 'XAF',
        idempotencyKey: '44444444-4444-4444-4444-444444444444',
      );

      final receipt = await repository.send(request);
      final requestBody =
          jsonDecode(captured.body) as Map<String, dynamic>;

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/p2p/transfers');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(requestBody['sourceWalletId'], request.sourceWalletId);
      expect(requestBody['recipientKind'], 'afwal-id');
      expect(requestBody['recipientValue'], request.payeeId);
      expect(requestBody['currencyCode'], request.currencyCode);
      expect(requestBody['amountMinor'], request.amountMinor);
      expect(requestBody['correlationId'], request.idempotencyKey);

      expect(
        receipt.paymentIntentId,
        '11111111-1111-1111-1111-111111111111',
      );
      expect(receipt.status, TransferStatus.completed);
      expect(receipt.amountMinor, 2500);
      expect(receipt.currencyCode, 'XAF');
      expect(receipt.payeeId, '@recipient');
      apiClient.close();
    });

    test('sends a QR recipient transfer when recipient kind is explicit', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'transferId': '55555555-5555-5555-5555-555555555555',
              'sourceWalletId': '22222222-2222-2222-2222-222222222222',
              'targetWalletId': '33333333-3333-3333-3333-333333333333',
              'currencyCode': 'XAF',
              'amountMinor': 2500,
              'correlationId': '66666666-6666-6666-6666-666666666666',
              'createdAtUtc': '2026-09-27T20:00:00Z',
              'recipientKind': 'qr',
            }),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );
      final repository = AuthenticatedTransferRepository(
        P2PRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );
      const request = SendTransferRequest(
        sourceWalletId: '22222222-2222-2222-2222-222222222222',
        recipientKind: TransferRecipientKind.qr,
        payeeId: 'qr-token-abc',
        amountMinor: 2500,
        currencyCode: 'XAF',
        idempotencyKey: '66666666-6666-6666-6666-666666666666',
      );

      await repository.send(request);
      final requestBody =
          jsonDecode(captured.body) as Map<String, dynamic>;

      expect(requestBody['sourceWalletId'], request.sourceWalletId);
      expect(requestBody['recipientKind'], 'qr');
      expect(requestBody['recipientValue'], 'qr-token-abc');
      expect(requestBody['correlationId'], request.idempotencyKey);
      apiClient.close();
    });

    test('rejects transfers when no authenticated session exists', () async {
      var remoteCalled = false;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async {
          remoteCalled = true;
          return http.Response('{}', 200);
        }),
      );
      final repository = AuthenticatedTransferRepository(
        P2PRemoteDataSource(apiClient),
        _FakeAuthSessionStore(null),
      );

      await expectLater(
        repository.send(
          const SendTransferRequest(
            sourceWalletId: '22222222-2222-2222-2222-222222222222',
            payeeId: '@recipient',
            amountMinor: 2500,
            currencyCode: 'XAF',
            idempotencyKey: '44444444-4444-4444-4444-444444444444',
          ),
        ),
        throwsA(isA<TransferUnavailableException>()),
      );
      expect(remoteCalled, isFalse);
      apiClient.close();
    });

    test('loads the backend receive identity with the stored token', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'publicLabel': '@innocent',
              'qrToken': 'qr-token',
            }),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );
      final repository = AuthenticatedTransferRepository(
        P2PRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );

      final identity = await repository.loadReceiveIdentity();

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/p2p/receive-identity');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(identity.publicLabel, '@innocent');
      expect(identity.qrToken, 'qr-token');
      expect(identity.hasBackendQr, isTrue);
      apiClient.close();
    });

    test('preserves unauthorized responses from the P2P endpoint', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final repository = AuthenticatedTransferRepository(
        P2PRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );

      await expectLater(
        repository.loadReceiveIdentity(),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      apiClient.close();
    });
  });
}

StoredAuthSession _storedSession() => StoredAuthSession(
      accessToken: 'access-secret',
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 27, 14),
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
