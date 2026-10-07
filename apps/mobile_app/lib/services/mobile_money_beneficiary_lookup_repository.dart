import '../data/remote/mobile_money_beneficiary_lookup_dto.dart';
import '../data/remote/mobile_money_beneficiary_lookup_mapper.dart';
import '../data/remote/mobile_money_beneficiary_lookup_remote_data_source.dart';
import '../models/mobile_money_beneficiary_lookup.dart';
import '../models/mobile_money_beneficiary_operator.dart';
import '../network/api_exception.dart';

class MobileMoneyBeneficiarySearchResult {
  const MobileMoneyBeneficiarySearchResult({
    required this.lookup,
    required this.operator,
  });

  final MobileMoneyBeneficiaryLookup lookup;
  final MobileMoneyBeneficiaryOperator? operator;

  String? get operatorCode => operator?.operatorCode;

  bool get isFullyResolved =>
      operator != null &&
      lookup.operatorResolved &&
      lookup.beneficiaryResolved &&
      lookup.accountHolderName != null;

  bool get requiresManualEntry => !isFullyResolved;
}

abstract interface class MobileMoneyBeneficiaryLookupRepository {
  Future<MobileMoneyBeneficiarySearchResult> search({
    required String phoneNumber,
  });
}

class RemoteMobileMoneyBeneficiaryLookupRepository
    implements MobileMoneyBeneficiaryLookupRepository {
  const RemoteMobileMoneyBeneficiaryLookupRepository(this._remoteDataSource);

  final MobileMoneyBeneficiaryLookupRemoteDataSource _remoteDataSource;

  @override
  Future<MobileMoneyBeneficiarySearchResult> search({
    required String phoneNumber,
  }) async {
    if (phoneNumber.trim().isEmpty) {
      throw ArgumentError.value(
        phoneNumber,
        'phoneNumber',
        'must not be empty',
      );
    }

    final response = await _remoteDataSource.lookup(
      MobileMoneyBeneficiaryLookupRequestDto(
        phoneNumber: phoneNumber.trim(),
      ),
    );
    final lookup = MobileMoneyBeneficiaryLookupMapper.toModel(response);

    if (lookup.beneficiaryResolved && lookup.accountHolderName == null) {
      throw const ApiMalformedResponseException(
        'Resolved beneficiary lookup must include an account holder name.',
      );
    }

    final mappedOperator = MobileMoneyBeneficiaryOperatorMapper.fromBackend(
      countryCode: lookup.countryCode,
      operator: lookup.operator,
    );

    if (lookup.operatorResolved && mappedOperator == null) {
      throw const ApiMalformedResponseException(
        'Resolved beneficiary lookup returned an unsupported operator.',
      );
    }

    return MobileMoneyBeneficiarySearchResult(
      lookup: lookup,
      operator: mappedOperator,
    );
  }
}
