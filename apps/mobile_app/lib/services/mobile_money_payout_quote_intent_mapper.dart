import '../data/remote/mobile_money_payout_quote_dto.dart';
import '../models/mobile_money_payout_quote_intent.dart';
import 'mobile_money_beneficiary_lookup_repository.dart';

class MobileMoneyPayoutQuoteIntentMapper {
  const MobileMoneyPayoutQuoteIntentMapper._();

  static BeneficiaryQuoteDraft beneficiaryFromSearchResult(
    MobileMoneyBeneficiarySearchResult result,
  ) {
    final accountHolderName = result.lookup.accountHolderName;
    final operatorCode = result.operatorCode;

    if (!result.isFullyResolved ||
        accountHolderName == null ||
        operatorCode == null) {
      throw StateError(
        'A payout quote requires a fully resolved Mobile Money beneficiary.',
      );
    }

    final draft = BeneficiaryQuoteDraft(
      normalizedPhoneNumber: result.lookup.normalizedPhoneNumber,
      countryCode: result.lookup.countryCode,
      operatorCode: operatorCode,
      accountHolderName: accountHolderName,
    );
    draft.validate();
    return draft;
  }

  static MobileMoneyPayoutQuoteRequestDto toRequestDto(
    MobileMoneyPayoutQuoteIntent intent,
  ) {
    intent.validate();

    return MobileMoneyPayoutQuoteRequestDto(
      sourceCountryCode: intent.sourceCountryCode,
      sourceCurrency: intent.sourceCurrencyCode,
      destinationCountryCode: intent.beneficiary.countryCode,
      destinationCurrency: intent.destinationCurrencyCode,
      operatorCode: _toBackendQuoteOperatorCode(
        intent.beneficiary.operatorCode,
      ),
      sourceAmountMinor: intent.sourceAmountMinor,
    );
  }

  static String _toBackendQuoteOperatorCode(String operatorCode) {
    return operatorCode.trim().toUpperCase().replaceAll('_', '-');
  }
}
