import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_quote_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_quote_mapper.dart';

void main() {
  const request = MobileMoneyPayoutQuoteRequestDto(
    sourceCountryCode: 'DE',
    sourceCurrency: 'EUR',
    destinationCountryCode: 'CM',
    destinationCurrency: 'XAF',
    operatorCode: 'MTN-CM',
    sourceAmountMinor: 10000,
  );

  test('maps the exact payout quote request JSON contract', () {
    expect(
      MobileMoneyPayoutQuoteMapper.requestToJson(request),
      <String, Object?>{
        'sourceCountryCode': 'DE',
        'sourceCurrency': 'EUR',
        'destinationCountryCode': 'CM',
        'destinationCurrency': 'XAF',
        'operatorCode': 'MTN-CM',
        'sourceAmountMinor': 10000,
      },
    );
  });

  test('maps the complete payout quote response into the domain model', () {
    final dto = MobileMoneyPayoutQuoteMapper.responseFromJson(
      <String, dynamic>{
        'quoteId': 'dd5d65b9-83ba-463a-87f8-d4b8513c8595',
        'sourceCurrency': 'EUR',
        'sourceAmountMinor': 10000,
        'fees': <Object?>[
          <String, dynamic>{
            'code': 'SERVICE_FEE',
            'amountMinor': 250,
            'currency': 'EUR',
          },
        ],
        'totalFeeMinor': 250,
        'totalSourceDebitMinor': 10250,
        'destinationCurrency': 'XAF',
        'destinationAmountMinor': 6559570,
        'fxRate': 655.957,
        'createdAtUtc': '2026-10-07T00:00:00+00:00',
        'expiresAtUtc': '2026-10-07T00:10:00+00:00',
      },
    );

    final quote = MobileMoneyPayoutQuoteMapper.toDomain(dto);

    expect(quote.quoteId, 'dd5d65b9-83ba-463a-87f8-d4b8513c8595');
    expect(quote.sourceCurrencyCode, 'EUR');
    expect(quote.sourceAmountMinor, 10000);
    expect(quote.fees, hasLength(1));
    expect(quote.fees.single.code, 'SERVICE_FEE');
    expect(quote.fees.single.amountMinor, 250);
    expect(quote.fees.single.currencyCode, 'EUR');
    expect(quote.totalFeeMinor, 250);
    expect(quote.totalSourceDebitMinor, 10250);
    expect(quote.destinationCurrencyCode, 'XAF');
    expect(quote.destinationAmountMinor, 6559570);
    expect(quote.fxRate, 655.957);
    expect(quote.createdAtUtc.isUtc, isTrue);
    expect(quote.expiresAtUtc.isUtc, isTrue);
  });

  test('accepts an integral JSON fxRate without weakening its domain type', () {
    final dto = MobileMoneyPayoutQuoteMapper.responseFromJson(
      _validPayload()..['fxRate'] = 656,
    );

    expect(dto.fxRate, 656.0);
  });

  test('rejects malformed nested fees and invalid monetary fields', () {
    expect(
      () => MobileMoneyPayoutQuoteMapper.responseFromJson(
        _validPayload()..['fees'] = <Object?>['invalid'],
      ),
      throwsFormatException,
    );
    expect(
      () => MobileMoneyPayoutQuoteMapper.responseFromJson(
        _validPayload()..['sourceAmountMinor'] = 0,
      ),
      throwsFormatException,
    );
    expect(
      () => MobileMoneyPayoutQuoteMapper.responseFromJson(
        _validPayload()..['totalFeeMinor'] = -1,
      ),
      throwsFormatException,
    );
  });

  test('rejects non-UTC or non-increasing quote timestamps', () {
    expect(
      () => MobileMoneyPayoutQuoteMapper.responseFromJson(
        _validPayload()..['createdAtUtc'] = '2026-10-07T02:00:00+02:00',
      ),
      throwsFormatException,
    );
    expect(
      () => MobileMoneyPayoutQuoteMapper.responseFromJson(
        _validPayload()
          ..['expiresAtUtc'] = '2026-10-07T00:00:00+00:00',
      ),
      throwsFormatException,
    );
  });
}

Map<String, dynamic> _validPayload() => <String, dynamic>{
  'quoteId': 'dd5d65b9-83ba-463a-87f8-d4b8513c8595',
  'sourceCurrency': 'EUR',
  'sourceAmountMinor': 10000,
  'fees': <Object?>[
    <String, dynamic>{
      'code': 'SERVICE_FEE',
      'amountMinor': 250,
      'currency': 'EUR',
    },
  ],
  'totalFeeMinor': 250,
  'totalSourceDebitMinor': 10250,
  'destinationCurrency': 'XAF',
  'destinationAmountMinor': 6559570,
  'fxRate': 655.957,
  'createdAtUtc': '2026-10-07T00:00:00+00:00',
  'expiresAtUtc': '2026-10-07T00:10:00+00:00',
};
