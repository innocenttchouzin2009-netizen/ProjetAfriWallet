import '../../models/mobile_money_payout_quote.dart';
import 'mobile_money_payout_quote_dto.dart';

class MobileMoneyPayoutQuoteMapper {
  const MobileMoneyPayoutQuoteMapper._();

  static Map<String, Object?> requestToJson(
    MobileMoneyPayoutQuoteRequestDto request,
  ) {
    return <String, Object?>{
      'sourceCountryCode': request.sourceCountryCode,
      'sourceCurrency': request.sourceCurrency,
      'destinationCountryCode': request.destinationCountryCode,
      'destinationCurrency': request.destinationCurrency,
      'operatorCode': request.operatorCode,
      'sourceAmountMinor': request.sourceAmountMinor,
    };
  }

  static MobileMoneyPayoutQuoteResponseDto responseFromJson(Object? payload) {
    if (payload is! Map<String, dynamic>) {
      throw const FormatException('Expected a payout quote JSON object.');
    }

    final quoteId = _requiredNonEmptyString(payload, 'quoteId');
    final sourceCurrency = _requiredCurrency(payload, 'sourceCurrency');
    final sourceAmountMinor = _requiredPositiveInt(
      payload,
      'sourceAmountMinor',
    );

    final rawFees = payload['fees'];
    if (rawFees is! List) {
      throw const FormatException('Missing or invalid payout quote fees.');
    }
    final fees = rawFees.map(_feeFromJson).toList(growable: false);

    final totalFeeMinor = _requiredNonNegativeInt(payload, 'totalFeeMinor');
    final totalSourceDebitMinor = _requiredPositiveInt(
      payload,
      'totalSourceDebitMinor',
    );
    final destinationCurrency = _requiredCurrency(
      payload,
      'destinationCurrency',
    );
    final destinationAmountMinor = _requiredPositiveInt(
      payload,
      'destinationAmountMinor',
    );

    final rawFxRate = payload['fxRate'];
    if (rawFxRate is! num || !rawFxRate.isFinite || rawFxRate <= 0) {
      throw const FormatException('Missing or invalid payout quote fxRate.');
    }

    final createdAtUtc = _requiredUtcDateTime(payload, 'createdAtUtc');
    final expiresAtUtc = _requiredUtcDateTime(payload, 'expiresAtUtc');
    if (!expiresAtUtc.isAfter(createdAtUtc)) {
      throw const FormatException(
        'Payout quote expiresAtUtc must be after createdAtUtc.',
      );
    }

    return MobileMoneyPayoutQuoteResponseDto(
      quoteId: quoteId,
      sourceCurrency: sourceCurrency,
      sourceAmountMinor: sourceAmountMinor,
      fees: fees,
      totalFeeMinor: totalFeeMinor,
      totalSourceDebitMinor: totalSourceDebitMinor,
      destinationCurrency: destinationCurrency,
      destinationAmountMinor: destinationAmountMinor,
      fxRate: rawFxRate.toDouble(),
      createdAtUtc: createdAtUtc,
      expiresAtUtc: expiresAtUtc,
    );
  }

  static MobileMoneyPayoutQuote toDomain(
    MobileMoneyPayoutQuoteResponseDto response,
  ) {
    return MobileMoneyPayoutQuote(
      quoteId: response.quoteId,
      sourceCurrencyCode: response.sourceCurrency,
      sourceAmountMinor: response.sourceAmountMinor,
      fees: response.fees
          .map(
            (fee) => MobileMoneyPayoutQuoteFee(
              code: fee.code,
              amountMinor: fee.amountMinor,
              currencyCode: fee.currency,
            ),
          )
          .toList(growable: false),
      totalFeeMinor: response.totalFeeMinor,
      totalSourceDebitMinor: response.totalSourceDebitMinor,
      destinationCurrencyCode: response.destinationCurrency,
      destinationAmountMinor: response.destinationAmountMinor,
      fxRate: response.fxRate,
      createdAtUtc: response.createdAtUtc,
      expiresAtUtc: response.expiresAtUtc,
    );
  }

  static MobileMoneyPayoutQuoteFeeDto _feeFromJson(Object? payload) {
    if (payload is! Map<String, dynamic>) {
      throw const FormatException('Invalid payout quote fee.');
    }

    return MobileMoneyPayoutQuoteFeeDto(
      code: _requiredNonEmptyString(payload, 'code'),
      amountMinor: _requiredNonNegativeInt(payload, 'amountMinor'),
      currency: _requiredCurrency(payload, 'currency'),
    );
  }

  static String _requiredNonEmptyString(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = payload[key];
    if (value is! String || value.trim().isEmpty) {
      throw FormatException('Missing or invalid payout quote $key.');
    }
    return value;
  }

  static String _requiredCurrency(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = _requiredNonEmptyString(payload, key);
    if (!RegExp(r'^[A-Z]{3}$').hasMatch(value)) {
      throw FormatException('Invalid payout quote $key.');
    }
    return value;
  }

  static int _requiredPositiveInt(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = payload[key];
    if (value is! int || value <= 0) {
      throw FormatException('Missing or invalid payout quote $key.');
    }
    return value;
  }

  static int _requiredNonNegativeInt(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = payload[key];
    if (value is! int || value < 0) {
      throw FormatException('Missing or invalid payout quote $key.');
    }
    return value;
  }

  static DateTime _requiredUtcDateTime(
    Map<String, dynamic> payload,
    String key,
  ) {
    final value = payload[key];
    if (value is! String ||
        !(value.endsWith('Z') || value.endsWith('+00:00'))) {
      throw FormatException('Missing or invalid UTC payout quote $key.');
    }

    final parsed = DateTime.tryParse(value);
    if (parsed == null) {
      throw FormatException('Missing or invalid UTC payout quote $key.');
    }

    return parsed.toUtc();
  }
}
