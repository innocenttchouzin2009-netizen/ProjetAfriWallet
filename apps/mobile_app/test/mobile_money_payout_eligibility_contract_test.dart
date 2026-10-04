import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_eligibility_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_eligibility_mapper.dart';

void main() {
  group('MobileMoneyPayoutEligibility contract', () {
    test('maps the exact backend eligibility request contract', () {
      const request = MobileMoneyPayoutEligibilityRequestDto(
        sourceCountryCode: 'DE',
        sourceCurrency: 'EUR',
        destinationCountryCode: 'CM',
        destinationCurrency: 'XAF',
        operatorCode: 'MTN_CM',
      );

      final json = MobileMoneyPayoutEligibilityMapper.requestToJson(request);

      expect(json, <String, Object?>{
        'sourceCountryCode': 'DE',
        'sourceCurrency': 'EUR',
        'destinationCountryCode': 'CM',
        'destinationCurrency': 'XAF',
        'operatorCode': 'MTN_CM',
      });
    });

    test('maps an eligible backend response with no failure code', () {
      final response = MobileMoneyPayoutEligibilityMapper.responseFromJson(
        <String, Object?>{
          'isEligible': true,
          'failureCode': null,
        },
      );

      expect(response.isEligible, isTrue);
      expect(response.failureCode, isNull);
    });

    test('preserves a backend payout eligibility failure code', () {
      final response = MobileMoneyPayoutEligibilityMapper.responseFromJson(
        <String, Object?>{
          'isEligible': false,
          'failureCode': 'PAYOUT_OPERATOR_NOT_ACTIVATED',
        },
      );

      expect(response.isEligible, isFalse);
      expect(response.failureCode, 'PAYOUT_OPERATOR_NOT_ACTIVATED');
    });

    test('rejects a non-object eligibility response', () {
      expect(
        () => MobileMoneyPayoutEligibilityMapper.responseFromJson(
          <Object?>[true, null],
        ),
        throwsA(isA<FormatException>()),
      );
    });

    test('rejects a response without a boolean isEligible', () {
      expect(
        () => MobileMoneyPayoutEligibilityMapper.responseFromJson(
          <String, Object?>{
            'isEligible': 'true',
            'failureCode': null,
          },
        ),
        throwsA(isA<FormatException>()),
      );
    });

    test('rejects an invalid non-string failureCode', () {
      expect(
        () => MobileMoneyPayoutEligibilityMapper.responseFromJson(
          <String, Object?>{
            'isEligible': false,
            'failureCode': 42,
          },
        ),
        throwsA(isA<FormatException>()),
      );
    });
  });
}
