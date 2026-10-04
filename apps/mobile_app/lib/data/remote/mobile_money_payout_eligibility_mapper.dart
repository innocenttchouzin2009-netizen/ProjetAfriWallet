import 'mobile_money_payout_eligibility_dto.dart';

class MobileMoneyPayoutEligibilityMapper {
  const MobileMoneyPayoutEligibilityMapper._();

  static Map<String, Object?> requestToJson(
    MobileMoneyPayoutEligibilityRequestDto request,
  ) {
    return <String, Object?>{
      'sourceCountryCode': request.sourceCountryCode,
      'sourceCurrency': request.sourceCurrency,
      'destinationCountryCode': request.destinationCountryCode,
      'destinationCurrency': request.destinationCurrency,
      'operatorCode': request.operatorCode,
    };
  }

  static MobileMoneyPayoutEligibilityResponseDto responseFromJson(
    Object? payload,
  ) {
    if (payload is! Map<String, dynamic>) {
      throw const FormatException(
        'Expected a payout eligibility JSON object.',
      );
    }

    final isEligible = payload['isEligible'];
    if (isEligible is! bool) {
      throw const FormatException(
        'Missing or invalid payout eligibility isEligible.',
      );
    }

    final failureCode = payload['failureCode'];
    if (failureCode != null &&
        (failureCode is! String || failureCode.trim().isEmpty)) {
      throw const FormatException(
        'Invalid payout eligibility failureCode.',
      );
    }

    return MobileMoneyPayoutEligibilityResponseDto(
      isEligible: isEligible,
      failureCode: failureCode as String?,
    );
  }
}
