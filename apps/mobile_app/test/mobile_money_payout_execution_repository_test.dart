import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_execution_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_handoff.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/mobile_money_payout_execution_repository.dart';
import 'package:mobile_app/services/secure_session_store.dart';

void main() {
  group('RemoteMobileMoneyPayoutExecutionRepository', () {
    test('uses authenticated session and maps remote preparation to domain',
        () async {
      late http.Request capturedRequest;
      final repository = _repository(
        MockClient((request) async {
          capturedRequest = request;
          return http.Response(
            jsonEncode(_validResponse()),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );

      final preparation = await repository.prepareExecution(
        fundingCorrelationId: '22222222-2222-4222-8222-222222222222',
        idempotencyKey: 'exec-42',
        handoff: _handoff(),
      );

      expect(
        capturedRequest.url.path,
        '/api/v1/mobile-money/payouts/execution/prepare',
      );
      expect(
        capturedRequest.headers['Authorization'],
        'Bearer execution-access-token',
      );
      expect(capturedRequest.headers['Idempotency-Key'], 'exec-42');
      expect(
        jsonDecode(capturedRequest.body),
        <String, Object?>{
          'quoteId': '11111111-1111-4111-8111-111111111111',
          'fundingCorrelationId': '22222222-2222-4222-8222-222222222222',
        },
      );

      expect(
        preparation.bindingId,
        '33333333-3333-4333-8333-333333333333',
      );
      expect(
        preparation.quoteId,
        '11111111-1111-4111-8111-111111111111',
      );
      expect(
        preparation.fundingCorrelationId,
        '22222222-2222-4222-8222-222222222222',
      );
      expect(preparation.idempotencyKey, 'exec-42');
      expect(preparation.status, 'Prepared');
      expect(preparation.isReplay, isFalse);
      expect(preparation.preparedAtUtc, DateTime.utc(2026, 10, 12, 6));
    });

    test('requires an authenticated session before the network call', () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
        authenticated: false,
      );

      await expectLater(
        repository.prepareExecution(
          fundingCorrelationId: '22222222-2222-4222-8222-222222222222',
          idempotencyKey: 'exec-42',
          handoff: _handoff(),
        ),
        throwsA(isA<MobileMoneyPayoutExecutionUnavailableException>()),
      );

      expect(called, isFalse);
    });

    test('rejects empty idempotency key before the network call', () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.prepareExecution(
          fundingCorrelationId: '22222222-2222-4222-8222-222222222222',
          idempotencyKey: '   ',
          handoff: _handoff(),
        ),
        throwsArgumentError,
      );

      expect(called, isFalse);
    });

    test('rejects invalid handoff before reading session or calling network',
        () async {
      var called = false;
      final store = _CountingAuthSessionStore(_authenticatedSession());
      final repository = RemoteMobileMoneyPayoutExecutionRepository(
        MobileMoneyPayoutExecutionRemoteDataSource(
          ApiClient(
            baseUrl: 'https://api.afrikawallet.test',
            httpClient: MockClient((_) async {
              called = true;
              return http.Response('{}', 200);
            }),
          ),
        ),
        store,
      );

      await expectLater(
        repository.prepareExecution(
          fundingCorrelationId: '22222222-2222-4222-8222-222222222222',
          idempotencyKey: 'exec-42',
          handoff: MobileMoneyPayoutExecutionHandoff(
            quoteId: '11111111-1111-4111-8111-111111111111',
            sourceCurrencyCode: 'EUR',
            totalSourceDebitMinor: 10250,
            destinationCurrencyCode: 'XAF',
            destinationAmountMinor: 6720000,
            fundingAllocations: const <FundingAllocation>[],
            preparedAtUtc: DateTime.utc(2026, 10, 12, 5, 55),
            quoteExpiresAtUtc: DateTime.utc(2026, 10, 12, 6, 5),
          ),
        ),
        throwsArgumentError,
      );

      expect(store.readCount, 0);
      expect(called, isFalse);
    });
  });
}

RemoteMobileMoneyPayoutExecutionRepository _repository(
  http.Client client, {
  bool authenticated = true,
}) {
  return RemoteMobileMoneyPayoutExecutionRepository(
    MobileMoneyPayoutExecutionRemoteDataSource(
      ApiClient(
        baseUrl: 'https://api.afrikawallet.test',
        httpClient: client,
      ),
    ),
    _CountingAuthSessionStore(
      authenticated ? _authenticatedSession() : null,
    ),
  );
}

StoredAuthSession _authenticatedSession() {
  return StoredAuthSession(
    accessToken: 'execution-access-token',
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.utc(2026, 10, 12, 7),
  );
}

MobileMoneyPayoutExecutionHandoff _handoff() {
  return MobileMoneyPayoutExecutionHandoff(
    quoteId: '11111111-1111-4111-8111-111111111111',
    sourceCurrencyCode: 'EUR',
    totalSourceDebitMinor: 10250,
    destinationCurrencyCode: 'XAF',
    destinationAmountMinor: 6720000,
    fundingAllocations: const <FundingAllocation>[
      FundingAllocation(
        sourceId: 'wallet-1',
        sourceType: FundingSourceType.wallet,
        amountMinor: 2500,
        currencyCode: 'EUR',
      ),
      FundingAllocation(
        sourceId: 'google-pay-1',
        sourceType: FundingSourceType.googlePay,
        amountMinor: 7750,
        currencyCode: 'EUR',
      ),
    ],
    preparedAtUtc: DateTime.utc(2026, 10, 12, 5, 55),
    quoteExpiresAtUtc: DateTime.utc(2026, 10, 12, 6, 5),
  );
}

Map<String, Object?> _validResponse() => <String, Object?>{
      'bindingId': '33333333-3333-4333-8333-333333333333',
      'quoteId': '11111111-1111-4111-8111-111111111111',
      'fundingCorrelationId': '22222222-2222-4222-8222-222222222222',
      'idempotencyKey': 'exec-42',
      'status': 'Prepared',
      'isReplay': false,
      'preparedAtUtc': '2026-10-12T06:00:00Z',
    };

class _CountingAuthSessionStore implements AuthSessionStore {
  _CountingAuthSessionStore(this._session);

  StoredAuthSession? _session;
  int readCount = 0;

  @override
  Future<void> save(StoredAuthSession session) async {
    _session = session;
  }

  @override
  Future<StoredAuthSession?> read() async {
    readCount += 1;
    return _session;
  }

  @override
  Future<void> clear() async {
    _session = null;
  }
}
