import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_funding_remote_data_source.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/auth_session_coordinator.dart';
import 'package:mobile_app/services/authenticated_request_executor.dart';
import 'package:mobile_app/services/mobile_money_payout_funding_repository.dart';

void main() {
  group('RemoteMobileMoneyPayoutFundingRepository', () {
    test('uses authenticated executor and loads a bound domain funding plan',
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

      final plan = await repository.planFunding(
        correlationId: _correlationId,
        intent: _intent(),
        requestedAtUtc: _requestedAtUtc,
      );

      expect(
        capturedRequest.url.path,
        '/api/v1/mobile-money/payouts/funding/plan',
      );
      expect(
        capturedRequest.headers['Authorization'],
        'Bearer funding-access-token',
      );
      expect(plan.correlationId, _correlationId);
      expect(plan.requiredAmountMinor, 10250);
      expect(plan.currencyCode, 'EUR');
      expect(plan.allocations, hasLength(2));
      expect(plan.allocations.first.sourceType, FundingSourceType.wallet);
      expect(plan.allocations.last.sourceType, FundingSourceType.googlePay);
      expect(plan.plannedAtUtc, DateTime.utc(2026, 10, 10, 10, 30, 1));
    });

    test('rejects invalid funding intent before the network call', () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.planFunding(
          correlationId: _correlationId,
          intent: MobileMoneyPayoutFundingIntent(
            quoteId: 'quote-1',
            sourceCurrencyCode: 'EUR',
            totalSourceDebitMinor: 10250,
            fundingAllocations: const <FundingAllocation>[
              FundingAllocation(
                sourceId: 'wallet-1',
                sourceType: FundingSourceType.wallet,
                amountMinor: 2500,
                currencyCode: 'EUR',
              ),
            ],
          ),
          requestedAtUtc: _requestedAtUtc,
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('rejects non-UTC request time before the network call', () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.planFunding(
          correlationId: _correlationId,
          intent: _intent(),
          requestedAtUtc: DateTime(2026, 10, 10, 10, 30),
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
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
        repository.planFunding(
          correlationId: _correlationId,
          intent: _intent(),
          requestedAtUtc: _requestedAtUtc,
        ),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      expect(called, isFalse);
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 0);
    });

    test('refreshes once on 401 and retries with refreshed bearer token',
        () async {
      final lifecycle = _FakeAuthSessionLifecycle(
        restoredSession: _session('access-old'),
        refreshedSession: _session('access-new'),
      );
      final authorizations = <String?>[];
      final repository = _repository(
        MockClient((request) async {
          authorizations.add(request.headers['Authorization']);
          if (authorizations.length == 1) {
            return http.Response('', 401);
          }
          return http.Response(
            jsonEncode(_validResponse()),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
        lifecycle: lifecycle,
      );

      final plan = await repository.planFunding(
        correlationId: _correlationId,
        intent: _intent(),
        requestedAtUtc: _requestedAtUtc,
      );

      expect(plan.correlationId, _correlationId);
      expect(
        authorizations,
        <String?>['Bearer access-old', 'Bearer access-new'],
      );
      expect(lifecycle.restoreCalls, 1);
      expect(lifecycle.refreshCalls, 1);
    });

    test('fails closed when backend plan does not bind to selected intent',
        () async {
      final payload = _validResponse();
      payload['requiredAmountMinor'] = 10251;
      payload['allocations'] = <Object?>[
        <String, dynamic>{
          'sourceId': 'wallet-1',
          'sourceType': 0,
          'amountMinor': 2500,
          'currencyCode': 'EUR',
        },
        <String, dynamic>{
          'sourceId': 'google-pay-1',
          'sourceType': 2,
          'amountMinor': 7751,
          'currencyCode': 'EUR',
        },
      ];

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
        repository.planFunding(
          correlationId: _correlationId,
          intent: _intent(),
          requestedAtUtc: _requestedAtUtc,
        ),
        throwsA(isA<MobileMoneyPayoutFundingPlanMismatchException>()),
      );
    });
  });
}

const String _correlationId = '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4';
final DateTime _requestedAtUtc = DateTime.utc(2026, 10, 10, 10, 30);

RemoteMobileMoneyPayoutFundingRepository _repository(
  http.Client client, {
  _FakeAuthSessionLifecycle? lifecycle,
}) {
  final sessionLifecycle = lifecycle ??
      _FakeAuthSessionLifecycle(
        restoredSession: _session('funding-access-token'),
      );
  return RemoteMobileMoneyPayoutFundingRepository(
    MobileMoneyPayoutFundingRemoteDataSource(
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

MobileMoneyPayoutFundingIntent _intent() {
  return MobileMoneyPayoutFundingIntent(
    quoteId: 'quote-1',
    sourceCurrencyCode: 'EUR',
    totalSourceDebitMinor: 10250,
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
  );
}

Map<String, dynamic> _validResponse() => <String, dynamic>{
      'correlationId': _correlationId,
      'requiredAmountMinor': 10250,
      'currencyCode': 'EUR',
      'allocations': <Object?>[
        <String, dynamic>{
          'sourceId': 'wallet-1',
          'sourceType': 0,
          'amountMinor': 2500,
          'currencyCode': 'EUR',
        },
        <String, dynamic>{
          'sourceId': 'google-pay-1',
          'sourceType': 2,
          'amountMinor': 7750,
          'currencyCode': 'EUR',
        },
      ],
      'plannedAtUtc': '2026-10-10T10:30:01Z',
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
