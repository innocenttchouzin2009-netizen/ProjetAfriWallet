import '../data/remote/mobile_money_payout_remote_data_source.dart';
import '../models/mobile_money_payout.dart';

class MobileMoneyPayoutEligibility {
  const MobileMoneyPayoutEligibility({
    required this.isEligible,
    this.failureCode,
  });

  final bool isEligible;
  final String? failureCode;
}

abstract interface class MobileMoneyPayoutRepository {
  Future<MobileMoneyPayoutEligibility> checkEligibility({
    required String sourceCountryCode,
    required MobileMoneyPayoutRequest payout,
    Iterable<FundingSource>? fundingSources,
  });
}

class RemoteMobileMoneyPayoutRepository implements MobileMoneyPayoutRepository {
  const RemoteMobileMoneyPayoutRepository(this._remoteDataSource);

  final MobileMoneyPayoutRemoteDataSource _remoteDataSource;

  @override
  Future<MobileMoneyPayoutEligibility> checkEligibility({
    required String sourceCountryCode,
    required MobileMoneyPayoutRequest payout,
    Iterable<FundingSource>? fundingSources,
  }) async {
    _validateSourceCountryCode(sourceCountryCode);
    payout.validate(fundingSources: fundingSources);

    final response = await _remoteDataSource.checkEligibility(
      sourceCountryCode: sourceCountryCode,
      payout: payout,
    );

    return MobileMoneyPayoutEligibility(
      isEligible: response.isEligible,
      failureCode: response.failureCode,
    );
  }

  void _validateSourceCountryCode(String value) {
    if (!RegExp(r'^[A-Z]{2}$').hasMatch(value)) {
      throw ArgumentError.value(
        value,
        'sourceCountryCode',
        'must be an uppercase ISO-3166 alpha-2 code',
      );
    }
  }
}
