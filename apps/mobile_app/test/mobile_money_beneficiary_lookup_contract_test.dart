import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/data/remote/mobile_money_beneficiary_lookup_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_beneficiary_lookup_mapper.dart';

void main() {
  group('MobileMoneyBeneficiaryLookup contract', () {
    test('maps the exact backend lookup request contract', () {
      const request = MobileMoneyBeneficiaryLookupRequestDto(
        phoneNumber: '670123456',
      );

      final json = MobileMoneyBeneficiaryLookupMapper.requestToJson(request);

      expect(json, <String, Object?>{
        'phoneNumber': '670123456',
      });
    });

    test('maps a fully resolved backend response', () {
      final response = MobileMoneyBeneficiaryLookupMapper.responseFromJson(
        <String, Object?>{
          'normalizedPhoneNumber': '+237670123456',
          'countryCode': 'CM',
          'operator': 'MTN',
          'operatorResolved': true,
          'accountHolderName': 'Ada N.',
          'beneficiaryResolved': true,
        },
      );

      expect(response.normalizedPhoneNumber, '+237670123456');
      expect(response.countryCode, 'CM');
      expect(response.operator, 'MTN');
      expect(response.operatorResolved, isTrue);
      expect(response.accountHolderName, 'Ada N.');
      expect(response.beneficiaryResolved, isTrue);

      final model = MobileMoneyBeneficiaryLookupMapper.toModel(response);
      expect(model.normalizedPhoneNumber, '+237670123456');
      expect(model.countryCode, 'CM');
      expect(model.operator, 'MTN');
      expect(model.operatorResolved, isTrue);
      expect(model.accountHolderName, 'Ada N.');
      expect(model.beneficiaryResolved, isTrue);
    });

    test('preserves unresolved operator and beneficiary for manual fallback', () {
      final response = MobileMoneyBeneficiaryLookupMapper.responseFromJson(
        <String, Object?>{
          'normalizedPhoneNumber': '+237660123456',
          'countryCode': 'CM',
          'operator': null,
          'operatorResolved': false,
          'accountHolderName': null,
          'beneficiaryResolved': false,
        },
      );

      expect(response.normalizedPhoneNumber, '+237660123456');
      expect(response.countryCode, 'CM');
      expect(response.operator, isNull);
      expect(response.operatorResolved, isFalse);
      expect(response.accountHolderName, isNull);
      expect(response.beneficiaryResolved, isFalse);
    });

    test('rejects a non-object lookup response', () {
      expect(
        () => MobileMoneyBeneficiaryLookupMapper.responseFromJson(
          <Object?>['+237670123456', 'CM'],
        ),
        throwsA(isA<FormatException>()),
      );
    });

    test('rejects an unresolved operator carrying an operator value', () {
      expect(
        () => MobileMoneyBeneficiaryLookupMapper.responseFromJson(
          <String, Object?>{
            'normalizedPhoneNumber': '+237660123456',
            'countryCode': 'CM',
            'operator': 'MTN',
            'operatorResolved': false,
            'accountHolderName': null,
            'beneficiaryResolved': false,
          },
        ),
        throwsA(isA<FormatException>()),
      );
    });

    test('rejects an unresolved beneficiary carrying a holder name', () {
      expect(
        () => MobileMoneyBeneficiaryLookupMapper.responseFromJson(
          <String, Object?>{
            'normalizedPhoneNumber': '+237670123456',
            'countryCode': 'CM',
            'operator': 'MTN',
            'operatorResolved': true,
            'accountHolderName': 'Ada N.',
            'beneficiaryResolved': false,
          },
        ),
        throwsA(isA<FormatException>()),
      );
    });
  });
}
