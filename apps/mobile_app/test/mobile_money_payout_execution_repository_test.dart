import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_execution_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_handoff.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/authenticated_request_executor.dart';
import 'package:mobile_app/services/mobile_money_payout_execution_repository.dart';

void main() {
  group('RemoteMobileMoneyPayoutExecutionRepository', () {
    test('uses authenticated executor and returns strictly bound preparation',
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
        fundingCorrelationId: _fundingCorrelationId,
        idempotencyKey: _idempotencyKey,
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
      expect(capturedRequest.headers['Idempotency-Key'], _idempotencyKey);
      expect(preparation.quoteId, _quoteId);
      expect(preparation.fundingCorrelationId, _fundingCorrelationId);
      expect(preparation.idempotencyKey, _idempotencyKey);
      expect(preparation.status, 'Prepared');
      expect(preparation.isReplay, isFalse);
      expect(preparation.preparedAtUtc.isUtc, isTrue);
    });

    test('refreshes once on 401 and retries with the same idempotency key',
        () async {
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('access-old'),
        refreshedSession: _session('access-new'),
      );
      final authorizations = <String?>[];
      final idempotencyKeys = <String?>[];
      final repository = _repository(
        MockClient((request) async {
          authorizations.add(request.headers['Authorization']);
          idempotencyKeys.add(request.headers['Idempotency-Key']);
          if (authorizations.length == 1) {
            return http.Response('', 401);
          }
          final payload = _validResponse()..['isReplay'] = true;
          return http.Response(
            jsonEncode(payload),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
        lifecycle: lifecycle,
      );

      final preparation = await repository.prepareExecution(
        fundingCorrelationId: _fundingCorrelationId,
        idempotencyKey: _idempotencyKey,
        handoff: _handoff(),
      );

      expect(
        authorizations,
        <String?>['Bearer access-old', 'Bearer access-new'],
      );
      expect(
        idempotencyKeys,
        <String?>[_idempotencyKey, _idempotencyKey],
      );
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 1);
      expect(preparation.isReplay, isTrue);
    });

    test('fails closed without an authenticated session before network',
        () async {
      var called = false;
      final lifecycle = _FakeAuthSessionLifecycle(restoredSession: null);
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
        lifecycle: lifecycle,
      );

      await expectLater(
        repository.prepareExecution(
          fundingCorrelationId: _fundingCorrelationId,
          idempotencyKey: _idempotencyKey,
          handoff: _handoff(),
        ),
        throwsA(isA<ApiUnauthorizedException>()),
      );

      expect(called, isFalse);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
    });

    test('rejects invalid request values before authentication or network',
        () async {
      var called = false;
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('execution-access-token'),
      );
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
        lifecycle: lifecycle,
      );

      await expectLater(
        repository.prepareExecution(
          fundingCorrelationId: '   ',
          idempotencyKey: _idempotencyKey,
          handoff: _handoff(),
        ),
        throwsArgumentError,
      );
      await expectLater(
        repository.prepareExecution(
          fundingCorrelationId: _fundingCorrelationId,
          idempotencyKey: '   ',
          handoff: _handoff(),
        ),
        throwsArgumentError,
      );

      expect(called, isFalse);
      expect(lifecycle.restoreCalls, 0);
    });

    test('fails closed when backend preparation contradicts request binding',
        () async {
      for (final mutate in <void Function(Map<String, Object?>)>[
        (payload) => payload['quoteId'] =
            'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
        (payload) => payload['fundingCorrelationId'] =
            'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
        (payload) => payload['idempotencyKey'] = 'other-key',
        (payload) => payload['status'] = 'Executed',
      ]) {
        final payload = _validResponse();
        mutate(payload);
        final repository = _repository(
          MockClient(
            (_) async => http.Response(
              jsonEncode(payload),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            ),
          ),
        );

        await expectLater(
          repository.prepareExecution(
            fundingCorrelationId: _fundingCorrelationId,
            idempotencyKey: _idempotencyKey,
            handoff: _handoff(),
          ),
          throwsA(
            isA<MobileMoneyPayoutExecutionPreparationMismatchException>(),
          ),
        );
      }
    });
  });
}

const String _quoteId = '11111111-1111-4111-8111-111111111111';
const String _fundingCorrelationId =
    '22222222-2222-4222-8222-222222222222';
const String _idempotencyKey =
    '44444444-4444-4444-8444-444444444444';

RemoteMobileMoneyPayoutExecutionRepository _repository(
  http.Client client, {
  _FakeAuthSessionLifecycle? lifecycle,
}) {
  final sessionLifecycle = lifecycle ??
      _FakeAuthSessionLifecycle(
        restoredSession: _session('execution-access-token'),
      );
  return RemoteMobileMoneyPayoutExecutionRepository(
    MobileMoneyPayoutExecutionRemoteDataSource(
      ApiClient(
        baseUrl: 'https://api.afrikawallet.test',
        httpClient: client,
      ),
    ),
    AuthenticatedRequestExecutor(sessionLifecycle),
  );
}

StoredAuthSession _session(String accessToken) {
  return StoredAuthSession(
    accessToken: accessToken,
    refreshToken: 'refresh-token',
    tokenType: 'Bearer',
    sessionId: 'session-1',
    userId: 'user-1',
    accessTokenExpiresAtUtc: DateTime.utc(2099),
  );
}

MobileMoneyPayoutExecutionHandoff _handoff() {
  return MobileMoneyPayoutExecutionHandoff(
    quoteId: _quoteId,
    sourceCurrencyCode: 'EUR',
    totalSourceDebitMinor: 10250,
    destinationCurrencyCode: 'XAF',
    destinationAmountMinor: 6720000,
    fundingAllocations: const <FundingAllocation>[
      FundingAllocation(
        sourceId: 'wallet-1',
        sourceType: FundingSourceType.wallet,
        amountMinor: 10250,
        currencyCode: 'EUR',
      ),
    ],
    preparedAtUtc: DateTime.utc(2026, 10, 16, 12),
    quoteExpiresAtUtc: DateTime.utc(2026, 10, 16, 12, 10),
  );
}

Map<String, Object?> _validResponse() => <String, Object?>{
      'bindingId': '33333333-3333-4333-8333-333333333333',
      'quoteId': _quoteId,
      'fundingCorrelationId': _fundingCorrelationId,
      'idempotencyKey': _idempotencyKey,
      'status': 'Prepared',
      'isReplay': false,
      'preparedAtUtc': '2026-10-16T12:00:01Z',
    };

class _FakeAuthSessionLifecycle implements AuthSessionLifecycle {
  _FakeAuthSessionLifecycle({
    required this.restoredSession,
    this.refreshedSession,
  });

  final StoredAuthSession? restoredSession;
  final StoredAuthSession? refreshedSession;

  int restoreCalls = 0;
  int refreshCalls = 0;

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
  Future<void> clearLocalSession() async {}
}
