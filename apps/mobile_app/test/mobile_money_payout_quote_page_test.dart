import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/models/mobile_money_payout_quote_intent.dart';
import 'package:mobile_app/pages/mobile_money_payout_quote_page.dart';
import 'package:mobile_app/presentation/mobile_money_payout_quote_controller.dart';
import 'package:mobile_app/services/mobile_money_payout_quote_repository.dart';

typedef _QuoteLoader = Future<MobileMoneyPayoutQuote> Function(
  MobileMoneyPayoutQuoteIntent intent,
);

class _FakeQuoteRepository implements MobileMoneyPayoutQuoteRepository {
  _FakeQuoteRepository(this.loader);

  final _QuoteLoader loader;
  int callCount = 0;

  @override
  Future<MobileMoneyPayoutQuote> createQuoteFromIntent(
    MobileMoneyPayoutQuoteIntent intent,
  ) {
    callCount++;
    return loader(intent);
  }

  @override
  Future<MobileMoneyPayoutQuote> createQuote({
    required String sourceCountryCode,
    required String sourceCurrencyCode,
    required String destinationCountryCode,
    required String destinationCurrencyCode,
    required String operatorCode,
    required int sourceAmountMinor,
  }) {
    throw StateError('The presentation must use the payout quote intent.');
  }
}

MobileMoneyPayoutQuoteIntent _intent() =>
    const MobileMoneyPayoutQuoteIntent(
      sourceCountryCode: 'DE',
      sourceCurrencyCode: 'EUR',
      destinationCurrencyCode: 'XAF',
      sourceAmountMinor: 1000,
      beneficiary: BeneficiaryQuoteDraft(
        normalizedPhoneNumber: '+237650000000',
        countryCode: 'CM',
        operatorCode: 'MTN_CM',
        accountHolderName: 'Beneficiary',
      ),
    );

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

Widget _app(
  MobileMoneyPayoutQuoteController controller, {
  ValueChanged<MobileMoneyPayoutQuote>? onContinue,
}) {
  return MaterialApp(
    home: MobileMoneyPayoutQuotePage(
      controller: controller,
      intent: _intent(),
      onContinue: onContinue,
    ),
  );
}

void main() {
  testWidgets('loads the backend quote from the payout quote intent',
      (tester) async {
    final completer = Completer<MobileMoneyPayoutQuote>();
    final repository = _FakeQuoteRepository((intent) {
      expect(intent.sourceCountryCode, 'DE');
      expect(intent.sourceCurrencyCode, 'EUR');
      expect(intent.destinationCurrencyCode, 'XAF');
      expect(intent.sourceAmountMinor, 1000);
      expect(intent.beneficiary.countryCode, 'CM');
      expect(intent.beneficiary.operatorCode, 'MTN_CM');
      expect(intent.beneficiary.normalizedPhoneNumber, '+237650000000');
      expect(intent.beneficiary.accountHolderName, 'Beneficiary');
      return completer.future;
    });
    final controller = MobileMoneyPayoutQuoteController(
      repository: repository,
    );
    MobileMoneyPayoutQuote? continuedQuote;

    await tester.pumpWidget(
      _app(controller, onContinue: (quote) => continuedQuote = quote),
    );
    await tester.pump();

    expect(find.byKey(const Key('momo-quote-loading')), findsOneWidget);
    expect(repository.callCount, 1);

    final quote = _quote();
    completer.complete(quote);
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('momo-quote-ready')), findsOneWidget);
    expect(find.byKey(const Key('momo-quote-source-amount')), findsOneWidget);
    expect(
      find.byKey(const Key('momo-quote-destination-amount')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('momo-quote-no-payout')), findsOneWidget);
    expect(find.byKey(const Key('momo-quote-continue')), findsOneWidget);
    expect(continuedQuote, isNull);

    await tester.tap(find.byKey(const Key('momo-quote-continue')));

    expect(continuedQuote, same(quote));
    expect(repository.callCount, 1);
  });

  testWidgets('retry requests the same intent without launching a payout',
      (tester) async {
    var shouldFail = true;
    final retryCompleter = Completer<MobileMoneyPayoutQuote>();
    final expectedIntent = _intent();
    final repository = _FakeQuoteRepository((intent) {
      expect(intent.sourceCountryCode, expectedIntent.sourceCountryCode);
      expect(
        intent.beneficiary.normalizedPhoneNumber,
        expectedIntent.beneficiary.normalizedPhoneNumber,
      );
      if (shouldFail) {
        return Future<MobileMoneyPayoutQuote>.error(
          StateError('quote unavailable'),
        );
      }
      return retryCompleter.future;
    });
    final controller = MobileMoneyPayoutQuoteController(
      repository: repository,
    );

    await tester.pumpWidget(_app(controller));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('momo-quote-failed')), findsOneWidget);
    expect(repository.callCount, 1);

    shouldFail = false;
    await tester.tap(find.byKey(const Key('momo-quote-retry')));
    await tester.pump();

    expect(find.byKey(const Key('momo-quote-loading')), findsOneWidget);
    expect(repository.callCount, 2);

    retryCompleter.complete(_quote());
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('momo-quote-ready')), findsOneWidget);
    expect(repository.callCount, 2);
  });
}
