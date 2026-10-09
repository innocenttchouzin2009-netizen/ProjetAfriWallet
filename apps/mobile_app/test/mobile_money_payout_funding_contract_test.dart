import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_funding_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_funding_mapper.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';

void main() {
  const correlationId = '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4';

  test('maps the exact backend payout funding request contract', () {
    final request = MobileMoneyPayoutFundingMapper.requestFromIntent(
      correlationId: correlationId,
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
            sourceId: 'apple-pay-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 7750,
            currencyCode: 'EUR',
          ),
        ],
      ),
      requestedAtUtc: DateTime.utc(2026, 10, 10, 0, 15),
    );

    expect(
      MobileMoneyPayoutFundingMapper.requestToJson(request),
      <String, Object?>{
        'correlationId': correlationId,
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
            'sourceId': 'apple-pay-1',
            'sourceType': 1,
            'amountMinor': 7750,
            'currencyCode': 'EUR',
          },
        ],
        'requestedAtUtc': '2026-10-10T00:15:00.000Z',
      },
    );
  });

  test('pins every backend FundingSourceType numeric wire value', () {
    const types = <FundingSourceType>[
      FundingSourceType.wallet,
      FundingSourceType.applePay,
      FundingSourceType.googlePay,
      FundingSourceType.sepa,
      FundingSourceType.paymentCard,
    ];

    for (var index = 0; index < types.length; index += 1) {
      final request = MobileMoneyPayoutFundingMapper.requestFromIntent(
        correlationId: correlationId,
        intent: MobileMoneyPayoutFundingIntent(
          quoteId: 'quote-$index',
          sourceCurrencyCode: 'EUR',
          totalSourceDebitMinor: 100,
          fundingAllocations: <FundingAllocation>[
            FundingAllocation(
              sourceId: 'source-$index',
              sourceType: types[index],
              amountMinor: 100,
              currencyCode: 'EUR',
            ),
          ],
        ),
        requestedAtUtc: DateTime.utc(2026, 10, 10),
      );

      final json = MobileMoneyPayoutFundingMapper.requestToJson(request);
      final allocations = json['allocations']! as List<Object?>;
      final allocation = allocations.single! as Map<String, Object?>;

      expect(allocation['sourceType'], index);
    }
  });

  test('maps the funding plan response back to domain allocations', () {
    final response = MobileMoneyPayoutFundingMapper.responseFromJson(
      <String, dynamic>{
        'correlationId': correlationId,
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
        'plannedAtUtc': '2026-10-10T00:15:01+00:00',
      },
    );

    final allocations =
        MobileMoneyPayoutFundingMapper.allocationsToDomain(response);

    expect(response.correlationId, correlationId);
    expect(response.requiredAmountMinor, 10250);
    expect(response.currencyCode, 'EUR');
    expect(response.plannedAtUtc.isUtc, isTrue);
    expect(allocations, hasLength(2));
    expect(allocations.first.sourceType, FundingSourceType.wallet);
    expect(allocations.last.sourceType, FundingSourceType.googlePay);
  });

  test('rejects a malformed backend correlation GUID', () {
    expect(
      () => MobileMoneyPayoutFundingMapper.responseFromJson(
        _validResponse()..['correlationId'] = 'not-a-guid',
      ),
      throwsFormatException,
    );
  });

  test('rejects an unknown backend funding source type', () {
    expect(
      () => MobileMoneyPayoutFundingMapper.responseFromJson(
        _validResponse()..['allocations'] = <Object?>[
          <String, dynamic>{
            'sourceId': 'wallet-1',
            'sourceType': 99,
            'amountMinor': 10250,
            'currencyCode': 'EUR',
          },
        ],
      ),
      throwsFormatException,
    );
  });

  test('rejects a funding response whose allocations do not balance', () {
    expect(
      () => MobileMoneyPayoutFundingMapper.responseFromJson(
        _validResponse()..['requiredAmountMinor'] = 9999,
      ),
      throwsFormatException,
    );
  });

  test('rejects a non-UTC funding response timestamp', () {
    expect(
      () => MobileMoneyPayoutFundingMapper.responseFromJson(
        _validResponse()..['plannedAtUtc'] = '2026-10-10T02:15:01+02:00',
      ),
      throwsFormatException,
    );
  });
}

Map<String, dynamic> _validResponse() => <String, dynamic>{
  'correlationId': '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
  'requiredAmountMinor': 10250,
  'currencyCode': 'EUR',
  'allocations': <Object?>[
    <String, dynamic>{
      'sourceId': 'wallet-1',
      'sourceType': 0,
      'amountMinor': 10250,
      'currencyCode': 'EUR',
    },
  ],
  'plannedAtUtc': '2026-10-10T00:15:01Z',
};
