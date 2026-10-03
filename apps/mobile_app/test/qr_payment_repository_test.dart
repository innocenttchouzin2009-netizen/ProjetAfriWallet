import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/qr_payment_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/authenticated_production_wiring.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  group('AuthenticatedQrPaymentRepository', () {
    test('decodes a backend QR without requiring authentication', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'qrId': 'qr-001',
              'type': 'Static',
              'merchantId': 'merchant-001',
              'amountMinor': 1550,
              'currency': 'XAF',
              'merchantName': 'AfWal Market',
              'description': 'Order 42',
              'status': 'Active',
              'expiresAt': '2099-10-03T13:00:00Z',
            }),
            200,
          );
        }),
      );
      final repository = AuthenticatedQrPaymentRepository(
        QrPaymentRemoteDataSource(apiClient),
        _FakeAuthSessionStore(null),
      );

      final payload = await repository.decodeAndValidate(' backend-code ');

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/qr-payments/decode');
      expect(captured.headers.containsKey('Authorization'), isFalse);
      expect(jsonDecode(captured.body), <String, Object?>{
        'code': 'backend-code',
      });
      expect(payload.qrId, 'qr-001');
      expect(payload.merchantId, 'merchant-001');
      expect(payload.amountMinor, 1550);
      apiClient.close();
    });

    test('initiates payment with the stored token and idempotency key', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'paymentId': 'payment-001',
              'qrId': 'qr-001',
              'transferIntentId': 'transfer-001',
              'status': 'Initiated',
              'amountMinor': 1550,
              'currency': 'XAF',
              'receiptId': null,
              'receiptCode': null,
              'updatedAt': '2026-10-03T12:30:00Z',
            }),
            200,
          );
        }),
      );
      final repository = AuthenticatedQrPaymentRepository(
        QrPaymentRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
        idempotencyKeyFactory: () => 'idem-001',
      );

      final result = await repository.initiatePayment(
        payload: _activePayload(),
        payerWalletId: ' wallet-001 ',
      );
      final body = jsonDecode(captured.body) as Map<String, dynamic>;

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/qr-payments/initiate');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(body, <String, Object?>{
        'qrId': 'qr-001',
        'payerWalletId': 'wallet-001',
        'amountMinor': 1550,
        'currency': 'XAF',
        'idempotencyKey': 'idem-001',
      });
      expect(result.status, QrPaymentStatus.initiated);
      expect(result.transferIntentId, 'transfer-001');
      apiClient.close();
    });

    test('rejects payment initiation when no authenticated session exists',
        () async {
      var remoteCalled = false;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async {
          remoteCalled = true;
          return http.Response('{}', 200);
        }),
      );
      final repository = AuthenticatedQrPaymentRepository(
        QrPaymentRemoteDataSource(apiClient),
        _FakeAuthSessionStore(null),
      );

      await expectLater(
        repository.initiatePayment(
          payload: _activePayload(),
          payerWalletId: 'wallet-001',
        ),
        throwsA(isA<QrPaymentUnavailableException>()),
      );
      expect(remoteCalled, isFalse);
      apiClient.close();
    });

    test('gets authoritative status with the stored bearer token', () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'paymentId': 'payment-001',
              'qrId': 'qr-001',
              'transferIntentId': 'transfer-001',
              'status': 'Paid',
              'amountMinor': 1550,
              'currency': 'XAF',
              'receiptId': 'receipt-001',
              'receiptCode': 'AFW-R-001',
              'updatedAt': '2026-10-03T12:31:00Z',
            }),
            200,
          );
        }),
      );
      final repository = AuthenticatedQrPaymentRepository(
        QrPaymentRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );

      final result =
          await repository.getAuthoritativeStatus(' transfer-001 ');

      expect(captured.method, 'GET');
      expect(
        captured.url.path,
        '/api/v1/qr-payments/transfers/transfer-001/status',
      );
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(result.status, QrPaymentStatus.paid);
      expect(result.receiptId, 'receipt-001');
      expect(result.receiptCode, 'AFW-R-001');
      apiClient.close();
    });

    test('rejects inactive decoded QR before application payment flow', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'qrId': 'qr-expired',
              'type': 'Static',
              'merchantId': 'merchant-001',
              'amountMinor': 1550,
              'currency': 'XAF',
              'merchantName': 'AfWal Market',
              'description': '',
              'status': 'Expired',
              'expiresAt': null,
            }),
            200,
          ),
        ),
      );
      final repository = AuthenticatedQrPaymentRepository(
        QrPaymentRemoteDataSource(apiClient),
        _FakeAuthSessionStore(null),
      );

      await expectLater(
        repository.decodeAndValidate('expired-code'),
        throwsA(isA<InvalidQrPaymentException>()),
      );
      apiClient.close();
    });

    test('preserves unauthorized responses from authenticated QR endpoints',
        () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final repository = AuthenticatedQrPaymentRepository(
        QrPaymentRemoteDataSource(apiClient),
        _FakeAuthSessionStore(_storedSession()),
      );

      await expectLater(
        repository.getAuthoritativeStatus('transfer-001'),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      apiClient.close();
    });
  });

  test('authenticated production wiring exposes the QR repository', () {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afwal.test',
      httpClient: MockClient((_) async => http.Response('', 404)),
    );
    final wiring = AuthenticatedProductionWiring(
      apiClient: apiClient,
      secureStorageAdapter: _MemorySecureStorageAdapter(),
    );

    expect(
      wiring.qrPaymentRepository,
      isA<AuthenticatedQrPaymentRepository>(),
    );

    wiring.dispose();
    apiClient.close();
  });
}

QrPaymentPayload _activePayload() => QrPaymentPayload(
      type: QrPaymentType.static,
      merchantId: 'merchant-001',
      amountMinor: 1550,
      currencyCode: 'XAF',
      merchantName: 'AfWal Market',
      description: 'Order 42',
      qrId: 'qr-001',
      expiresAt: DateTime.utc(2099, 10, 3, 13),
    );

StoredAuthSession _storedSession() => StoredAuthSession(
      accessToken: 'access-secret',
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2099, 10, 3, 14),
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

class _MemorySecureStorageAdapter implements SecureStorageAdapter {
  final Map<String, String> _values = <String, String>{};

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {
    _values[key] = value;
  }

  @override
  Future<String?> read({required String key}) async => _values[key];

  @override
  Future<void> delete({required String key}) async {
    _values.remove(key);
  }
}
