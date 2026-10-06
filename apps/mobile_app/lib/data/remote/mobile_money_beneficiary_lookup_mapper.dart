import '../../models/mobile_money_beneficiary_lookup.dart';
import 'mobile_money_beneficiary_lookup_dto.dart';

class MobileMoneyBeneficiaryLookupMapper {
  const MobileMoneyBeneficiaryLookupMapper._();

  static Map<String, Object?> requestToJson(
    MobileMoneyBeneficiaryLookupRequestDto request,
  ) {
    return <String, Object?>{
      'phoneNumber': request.phoneNumber,
    };
  }

  static MobileMoneyBeneficiaryLookupResponseDto responseFromJson(
    Object? payload,
  ) {
    if (payload is! Map<String, dynamic>) {
      throw const FormatException(
        'Expected a beneficiary lookup JSON object.',
      );
    }

    final normalizedPhoneNumber = payload['normalizedPhoneNumber'];
    if (normalizedPhoneNumber is! String ||
        normalizedPhoneNumber.trim().isEmpty) {
      throw const FormatException(
        'Missing or invalid beneficiary lookup normalizedPhoneNumber.',
      );
    }

    final countryCode = payload['countryCode'];
    if (countryCode is! String || countryCode.trim().isEmpty) {
      throw const FormatException(
        'Missing or invalid beneficiary lookup countryCode.',
      );
    }

    final operator = payload['operator'];
    if (operator != null && (operator is! String || operator.trim().isEmpty)) {
      throw const FormatException(
        'Invalid beneficiary lookup operator.',
      );
    }

    final operatorResolved = payload['operatorResolved'];
    if (operatorResolved is! bool) {
      throw const FormatException(
        'Missing or invalid beneficiary lookup operatorResolved.',
      );
    }

    final accountHolderName = payload['accountHolderName'];
    if (accountHolderName != null &&
        (accountHolderName is! String || accountHolderName.trim().isEmpty)) {
      throw const FormatException(
        'Invalid beneficiary lookup accountHolderName.',
      );
    }

    final beneficiaryResolved = payload['beneficiaryResolved'];
    if (beneficiaryResolved is! bool) {
      throw const FormatException(
        'Missing or invalid beneficiary lookup beneficiaryResolved.',
      );
    }

    if (!operatorResolved && operator != null) {
      throw const FormatException(
        'Unresolved beneficiary lookup operator must be null.',
      );
    }

    if (!beneficiaryResolved && accountHolderName != null) {
      throw const FormatException(
        'Unresolved beneficiary lookup holder name must be null.',
      );
    }

    return MobileMoneyBeneficiaryLookupResponseDto(
      normalizedPhoneNumber: normalizedPhoneNumber,
      countryCode: countryCode,
      operator: operator as String?,
      operatorResolved: operatorResolved,
      accountHolderName: accountHolderName as String?,
      beneficiaryResolved: beneficiaryResolved,
    );
  }

  static MobileMoneyBeneficiaryLookup toModel(
    MobileMoneyBeneficiaryLookupResponseDto response,
  ) {
    return MobileMoneyBeneficiaryLookup(
      normalizedPhoneNumber: response.normalizedPhoneNumber,
      countryCode: response.countryCode,
      operator: response.operator,
      operatorResolved: response.operatorResolved,
      accountHolderName: response.accountHolderName,
      beneficiaryResolved: response.beneficiaryResolved,
    );
  }
}
