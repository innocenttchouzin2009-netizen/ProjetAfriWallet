import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/presentation/mobile_money_payout_quote_controller.dart';
import 'package:mobile_app/services/mobile_money_payout_quote_repository.dart';

typedef _QuoteLoader = Future<MobileMoneyPayoutQuote> Function({
  required String sourceCountryCode,
  required String sourceCurrencyCode,
  required String destinationCountryCode,
  required String destinationCurrencyCode,
  required String operatorCode,
  required int sourceAmountMinor,
});

class _FakeMobileMoneyPayoutQuoteRepository
    implements MobileMoneyPayoutQuoteRepository {
  _FakeMobileMoneyPayoutQuoteRepository(this._loader);

  final _QuoteLoader _loader;
  int callCount = 0;

  @override
  Future<MobileMoneyPayoutQuote> createQuote({
    required String sourceCountryCode,
    required String sourceCurrencyCode,
    required String destinationCountryCode,
    required String destinationCurrencyCode,
    required String operatorCode,
    required int sourceAmountMinor,
  }) {
    callCount++;
    return _loader(
      sourceCountryCode: sourceCountryCode,
      sourceCurrencyCode: sourceCurrencyCode,
      destinationCountryCode: destinationCountryCode,
      destinationCurrencyCode: destinationCurrencyCode,
      operatorCode: operatorCode,
      sourceAmountMinor: sourceAmountMinor,
    );
  }
}

MobileMoneyPayoutQuote _quote() => MobileMoneyPayoutQuote(
      quoteId: 'quote-1',
      sourceCurrencyCode: 'EUR',
      sourceAmountMinor: 1000,
      fees: const <MobileMoneyPayoutQuoteFee>[
        MobileMoneyPayoutQuoteFee(
          code: 'TRANSFER_FEE',
          amountMinor: 50,
          currencyCode: 'EUR',
        ),
      ],
      totalFeeMinor: 50,
      totalSourceDebitMinor: 1050,
      destinationCurrencyCode: 'XAF',
      destinationAmountMinor: 650000,
      fxRate: 650,
      createdAtUtc: DateTime.utc(2026, 10, 7, 16),
      expiresAtUtc: DateTime.utc(2026, 10, 7, 16, 10),
    );

Future<void> _createQuote(MobileMoneyPayoutQuoteController controller) {
  return controller.createQuote(
    sourceCountryCode: 'DE',
    sourceCurrencyCode: 'EUR',
    destinationCountryCode: 'CM',
    destinationCurrencyCode: 'XAF',
    operatorCode: 'MTN_CM',
    sourceAmountMinor: 1000,
  );
}

void main() {
  test('successful quote exposes ready state and backend quote', () async {
    final expected = _quote();
    final repository = _FakeMobileMoneyPayoutQuoteRepository(
      ({
        required String sourceCountryCode,
        required String sourceCurrencyCode,
        required String destinationCountryCode,
        required String destinationCurrencyCode,
        required String operatorCode,
        required int sourceAmountMinor,
      }) async {
        expect(sourceCountryCode, 'DE');
        expect(sourceCurrencyCode, 'EUR');
        expect(destinationCountryCode, 'CM');
        expect(destinationCurrencyCode, 'XAF');
        expect(operatorCode, 'MTN_CM');
        expect(sourceAmountMinor, 1000);
        return expected;
      },
    );
    final controller = MobileMoneyPayoutQuoteController(
      repository: repository,
    );

    await _createQuote(controller);

    expect(
      controller.status,
      MobileMoneyPayoutQuotePresentationStatus.ready,
    );
    expect(controller.quote, same(expected));
    expect(controller.hasQuote, isTrue);
    expect(controller.isLoading, isFalse);
    expect(controller.error, isNull);
    expect(repository.callCount, 1);
  });

  test('repository failure exposes failed state without inventing a quote',
      () async {
    final failure = StateError('quote unavailable');
    final repository = _FakeMobileMoneyPayoutQuoteRepository(
      ({
        required String sourceCountryCode,
        required String sourceCurrencyCode,
        required String destinationCountryCode,
        required String destinationCurrencyCode,
        required String operatorCode,
        required int sourceAmountMinor,
      }) async =>
          throw failure,
    );
    final controller = MobileMoneyPayoutQuoteController(
      repository: repository,
    );

    await _createQuote(controller);

    expect(
      controller.status,
      MobileMoneyPayoutQuotePresentationStatus.failed,
    );
    expect(controller.hasFailed, isTrue);
    expect(controller.hasQuote, isFalse);
    expect(controller.quote, isNull);
    expect(controller.error, same(failure));
  });

  test('duplicate concurrent quote requests are ignored while loading',
      () async {
    final completer = Completer<MobileMoneyPayoutQuote>();
    final repository = _FakeMobileMoneyPayoutQuoteRepository(
      ({
        required String sourceCountryCode,
        required String sourceCurrencyCode,
        required String destinationCountryCode,
        required String destinationCurrencyCode,
        required String operatorCode,
        required int sourceAmountMinor,
      }) =>
          completer.future,
    );
    final controller = MobileMoneyPayoutQuoteController(
      repository: repository,
    );

    final first = _createQuote(controller);
    final duplicate = _createQuote(controller);

    expect(controller.isLoading, isTrue);
    expect(repository.callCount, 1);

    completer.complete(_quote());
    await Future.wait(<Future<void>>[first, duplicate]);

    expect(repository.callCount, 1);
    expect(controller.hasQuote, isTrue);
  });

  test('reset clears quote and failure state', () async {
    final repository = _FakeMobileMoneyPayoutQuoteRepository(
      ({
        required String sourceCountryCode,
        required String sourceCurrencyCode,
        required String destinationCountryCode,
        required String destinationCurrencyCode,
        required String operatorCode,
        required int sourceAmountMinor,
      }) async =>
          _quote(),
    );
    final controller = MobileMoneyPayoutQuoteController(
      repository: repository,
    );

    await _createQuote(controller);
    controller.reset();

    expect(
      controller.status,
      MobileMoneyPayoutQuotePresentationStatus.idle,
    );
    expect(controller.quote, isNull);
    expect(controller.error, isNull);
    expect(controller.hasQuote, isFalse);
    expect(controller.hasFailed, isFalse);
  });
}
