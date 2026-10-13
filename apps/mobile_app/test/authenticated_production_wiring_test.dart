import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_handoff.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/authenticated_production_wiring.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  test(
    'authenticated production wiring shares one secure session across wallet, P2P, transaction history, payout funding, and payout execution',
    () async {
      final requests = <http.Request>[];
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          requests.add(request);

          if (request.method == 'GET' &&
              request.url.path == '/api/v1/wallets/mobile') {
            return http.Response(
              jsonEncode(<String, Object?>{
                'wallets': <Object?>[
                  <String, Object?>{
                    'walletId': '11111111-1111-1111-1111-111111111111',
                    'currency': 'XAF',
                    'availableMinor': 125000,
                    'status': 'ACTIVE',
                    'countryCode': 'CM',
                  },
                ],
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }

          if (request.method == 'GET' &&
              request.url.path == '/api/v1/transactions') {
            return http.Response(
              jsonEncode(<String, Object?>{
                'items': <Object?>[
                  <String, Object?>{
                    'transactionId': 'txn-wiring-1',
                    'walletId': '11111111-1111-1111-1111-111111111111',
                    'amountMinor': 2500,
                    'currencyCode': 'XAF',
                    'direction': 'Incoming',
                    'status': 'Completed',
                    'occurredAtUtc': '2026-10-02T16:00:00Z',
                    'reference': 'AFW-WIRING-1',
                    'counterpartyLabel': 'Merchant Wiring',
                  },
                ],
                'nextCursor': null,
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }

          if (request.method == 'POST' &&
              request.url.path == '/api/v1/p2p/receive-identity') {
            return http.Response(
              jsonEncode(<String, Object?>{
                'publicLabel': '@innocent',
                'qrToken': 'qr-token',
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }

          if (request.method == 'POST' &&
              request.url.path ==
                  '/api/v1/mobile-money/payouts/funding/plan') {
            return http.Response(
              jsonEncode(<String, Object?>{
                'correlationId':
                    '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
                'requiredAmountMinor': 10250,
                'currencyCode': 'EUR',
                'allocations': <Object?>[
                  <String, Object?>{
                    'sourceId': 'wallet-1',
                    'sourceType': 0,
                    'amountMinor': 2500,
                    'currencyCode': 'EUR',
                  },
                  <String, Object?>{
                    'sourceId': 'google-pay-1',
                    'sourceType': 2,
                    'amountMinor': 7750,
                    'currencyCode': 'EUR',
                  },
                ],
                'plannedAtUtc': '2026-10-10T10:30:01Z',
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }

          if (request.method == 'POST' &&
              request.url.path ==
                  '/api/v1/mobile-money/payouts/execution/prepare') {
            return http.Response(
              jsonEncode(<String, Object?>{
                'bindingId':
                    '33333333-3333-4333-8333-333333333333',
                'quoteId':
                    '11111111-1111-4111-8111-111111111111',
                'fundingCorrelationId':
                    '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
                'idempotencyKey': 'wiring-exec-42',
                'status': 'Prepared',
                'isReplay': false,
                'preparedAtUtc': '2026-10-12T14:00:00Z',
              }),
              200,
              headers: <String, String>{'content-type': 'application/json'},
            );
          }

          return http.Response('', 404);
        }),
      );
      final wiring = AuthenticatedProductionWiring(
        apiClient: apiClient,
        secureStorageAdapter: _MemorySecureStorageAdapter(),
      );
      addTearDown(() {
        wiring.dispose();
        apiClient.close();
      });

      await wiring.sessionStore.save(
        StoredAuthSession(
          accessToken: 'shared-access-token',
          refreshToken: 'refresh-token',
          tokenType: 'Bearer',
          sessionId: 'session-1',
          userId: 'user-1',
          accessTokenExpiresAtUtc:
              DateTime.now().toUtc().add(const Duration(hours: 1)),
        ),
      );

      final wallets = await wiring.walletRepository.loadWalletBalances();
      final receiveIdentity =
          await wiring.transferRepository.loadReceiveIdentity();
      final transactions =
          await wiring.transactionHistoryRepository.listTransactions();
      final fundingPlan =
          await wiring.mobileMoneyPayoutFundingRepository.planFunding(
        correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
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
            FundingAllocation(
              sourceId: 'google-pay-1',
              sourceType: FundingSourceType.googlePay,
              amountMinor: 7750,
              currencyCode: 'EUR',
            ),
          ],
        ),
        requestedAtUtc: DateTime.utc(2026, 10, 10, 10, 30),
      );

      final executionPreparation =
          await wiring.mobileMoneyPayoutExecutionRepository.prepareExecution(
        fundingCorrelationId:
            '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
        idempotencyKey: 'wiring-exec-42',
        handoff: MobileMoneyPayoutExecutionHandoff(
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
          preparedAtUtc: DateTime.utc(2026, 10, 12, 13, 55),
          quoteExpiresAtUtc: DateTime.utc(2026, 10, 12, 14, 5),
        ),
      );

      expect(wallets, hasLength(1));
      expect(receiveIdentity.qrToken, 'qr-token');
      expect(transactions, hasLength(1));
      expect(transactions.single.transactionId, 'txn-wiring-1');
      expect(fundingPlan.requiredAmountMinor, 10250);
      expect(executionPreparation.status, 'Prepared');
      expect(
        executionPreparation.fundingCorrelationId,
        '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
      );
      expect(requests, hasLength(5));
      expect(
        requests.every(
          (request) =>
              request.headers['Authorization'] == 'Bearer shared-access-token',
        ),
        isTrue,
      );
    },
  );
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
