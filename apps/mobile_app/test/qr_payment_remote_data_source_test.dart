import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/qr_payment_remote_data_source.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('QrPaymentRemoteDataSource', () {
    test('posts the exact certified decode contract and maps the response',
        () async {
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
              'expiresAt': '2026-10-03T13:00:00Z',
            }),
            200,
          );
        }),
      );
      final dataSource = QrPaymentRemoteDataSource(apiClient);

      final decoded = await dataSource.decode('backend-issued-code');
      final payload = decoded.toPayload();

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/qr-payments/decode');
      expect(captured.headers.containsKey('Authorization'), isFalse);
      expect(
        jsonDecode(captured.body),
        <String, Object?>{'code': 'backend-issued-code'},
      );
      expect(decoded.qrId, 'qr-001');
      expect(decoded.type, QrPaymentType.static);
      expect(decoded.status, QrPaymentStatus.active);
      expect(decoded.expiresAt, DateTime.parse('2026-10-03T13:00:00Z'));
      expect(payload.qrId, 'qr-001');
      expect(payload.merchantId, 'merchant-001');
      expect(payload.amountMinor, 1550);
      expect(payload.currencyCode, 'XAF');
      expect(payload.merchantName, 'AfWal Market');
      expect(payload.description, 'Order 42');

      apiClient.close();
    });

    test('posts the exact authenticated idempotent initiate contract',
        () async {
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
      final dataSource = QrPaymentRemoteDataSource(apiClient);

      final response = await dataSource.initiate(
        'access-secret',
        const QrPaymentInitiateRequest(
          qrId: 'qr-001',
          payerWalletId: '11111111-1111-1111-1111-111111111111',
          amountMinor: 1550,
          currency: 'XAF',
          idempotencyKey: 'idem-001',
        ),
      );

      expect(captured.method, 'POST');
      expect(captured.url.path, '/api/v1/qr-payments/initiate');
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(jsonDecode(captured.body), <String, Object?>{
        'qrId': 'qr-001',
        'payerWalletId': '11111111-1111-1111-1111-111111111111',
        'amountMinor': 1550,
        'currency': 'XAF',
        'idempotencyKey': 'idem-001',
      });
      expect(response.paymentId, 'payment-001');
      expect(response.transferIntentId, 'transfer-001');
      expect(response.status, QrPaymentStatus.initiated);
      expect(response.amountMinor, 1550);
      expect(response.currency, 'XAF');
      expect(response.updatedAt, DateTime.parse('2026-10-03T12:30:00Z'));
      expect(response.toResult().status, QrPaymentStatus.initiated);
      expect(response.toResult().transferIntentId, 'transfer-001');

      apiClient.close();
    });

    test('gets authoritative status with bearer auth and maps paid receipt',
        () async {
      late http.Request captured;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'paymentId': 'payment-001',
              'qrId': 'qr-001',
              'transferIntentId': 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
              'status': 'Paid',
              'amountMinor': 1550,
              'currency': 'XAF',
              'receiptId': 'receipt-001',
              'receiptCode': 'AFW-R-001',
              'updatedAt': '2026-10-03T12:31:00+00:00',
            }),
            200,
          );
        }),
      );
      final dataSource = QrPaymentRemoteDataSource(apiClient);

      final response = await dataSource.getStatus(
        'access-secret',
        'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
      );
      final result = response.toResult();

      expect(captured.method, 'GET');
      expect(
        captured.url.path,
        '/api/v1/qr-payments/transfers/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/status',
      );
      expect(captured.headers['Authorization'], 'Bearer access-secret');
      expect(response.status, QrPaymentStatus.paid);
      expect(result.isFinanciallyConfirmed, isTrue);
      expect(result.receiptId, 'receipt-001');
      expect(result.receiptCode, 'AFW-R-001');

      apiClient.close();
    });

    test('maps Dynamic and Expired certified wire values', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'qrId': 'qr-dynamic',
              'type': 'Dynamic',
              'merchantId': 'merchant-002',
              'amountMinor': 0,
              'currency': 'EUR',
              'merchantName': '',
              'description': '',
              'status': 'Expired',
              'expiresAt': null,
            }),
            200,
          ),
        ),
      );
      final dataSource = QrPaymentRemoteDataSource(apiClient);

      final decoded = await dataSource.decode('dynamic-code');

      expect(decoded.type, QrPaymentType.dynamic);
      expect(decoded.status, QrPaymentStatus.expired);
      expect(decoded.expiresAt, isNull);
      expect(decoded.toPayload().amountMinor, 0);

      apiClient.close();
    });

    test('rejects unknown certified enum values as malformed responses',
        () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'qrId': 'qr-001',
              'type': 'Unsupported',
              'merchantId': 'merchant-001',
              'amountMinor': 100,
              'currency': 'XAF',
              'merchantName': 'Merchant',
              'description': '',
              'status': 'Active',
              'expiresAt': null,
            }),
            200,
          ),
        ),
      );
      final dataSource = QrPaymentRemoteDataSource(apiClient);

      await expectLater(
        dataSource.decode('code'),
        throwsA(isA<ApiMalformedResponseException>()),
      );

      apiClient.close();
    });

    test('propagates authenticated API failures without masking them',
        () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final dataSource = QrPaymentRemoteDataSource(apiClient);

      await expectLater(
        dataSource.getStatus('expired-token', 'transfer-001'),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      apiClient.close();
    });
  });
}
