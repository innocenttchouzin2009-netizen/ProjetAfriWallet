import '../data/remote/mobile_money_payout_quote_dto.dart';
import '../data/remote/mobile_money_payout_quote_mapper.dart';
import '../data/remote/mobile_money_payout_quote_remote_data_source.dart';
import '../models/mobile_money_payout_quote.dart';
import '../models/mobile_money_payout_quote_intent.dart';
import 'mobile_money_payout_quote_intent_mapper.dart';

abstract interface class MobileMoneyPayoutQuoteRepository {
  Future<MobileMoneyPayoutQuote> createQuote({
    required String sourceCountryCode,
    required String sourceCurrencyCode,
    required String destinationCountryCode,
    required String destinationCurrencyCode,
    required String operatorCode,
    required int sourceAmountMinor,
  });

  Future<MobileMoneyPayoutQuote> createQuoteFromIntent(
    MobileMoneyPayoutQuoteIntent intent,
  );
}

class RemoteMobileMoneyPayoutQuoteRepository
    implements MobileMoneyPayoutQuoteRepository {
  const RemoteMobileMoneyPayoutQuoteRepository(this._remoteDataSource);

  final MobileMoneyPayoutQuoteRemoteDataSource _remoteDataSource;

  @override
  Future<MobileMoneyPayoutQuote> createQuoteFromIntent(
    MobileMoneyPayoutQuoteIntent intent,
  ) async {
    final response = await _remoteDataSource.createQuote(
      MobileMoneyPayoutQuoteIntentMapper.toRequestDto(intent),
    );

    return MobileMoneyPayoutQuoteMapper.toDomain(response);
  }

  @override
  Future<MobileMoneyPayoutQuote> createQuote({
    required String sourceCountryCode,
    required String sourceCurrencyCode,
    required String destinationCountryCode,
    required String destinationCurrencyCode,
    required String operatorCode,
    required int sourceAmountMinor,
  }) async {
    _validateCountryCode(sourceCountryCode, 'sourceCountryCode');
    _validateCurrencyCode(sourceCurrencyCode, 'sourceCurrencyCode');
    _validateCountryCode(destinationCountryCode, 'destinationCountryCode');
    _validateCurrencyCode(destinationCurrencyCode, 'destinationCurrencyCode');
    _requireNonEmpty(operatorCode, 'operatorCode');

    if (sourceAmountMinor <= 0) {
      throw ArgumentError.value(
        sourceAmountMinor,
        'sourceAmountMinor',
        'must be greater than zero',
      );
    }

    final response = await _remoteDataSource.createQuote(
      MobileMoneyPayoutQuoteRequestDto(
        sourceCountryCode: sourceCountryCode,
        sourceCurrency: sourceCurrencyCode,
        destinationCountryCode: destinationCountryCode,
        destinationCurrency: destinationCurrencyCode,
        operatorCode: operatorCode,
        sourceAmountMinor: sourceAmountMinor,
      ),
    );

    return MobileMoneyPayoutQuoteMapper.toDomain(response);
  }

  void _validateCountryCode(String value, String name) {
    if (!RegExp(r'^[A-Z]{2}$').hasMatch(value)) {
      throw ArgumentError.value(
        value,
        name,
        'must be an uppercase ISO-3166 alpha-2 code',
      );
    }
  }

  void _validateCurrencyCode(String value, String name) {
    if (!RegExp(r'^[A-Z]{3}$').hasMatch(value)) {
      throw ArgumentError.value(
        value,
        name,
        'must be an uppercase ISO-4217 code',
      );
    }
  }

  void _requireNonEmpty(String value, String name) {
    if (value.trim().isEmpty) {
      throw ArgumentError.value(value, name, 'must not be empty');
    }
  }
}
