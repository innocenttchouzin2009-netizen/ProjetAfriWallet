import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_lookup.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/pages/mobile_money_beneficiary_search_page.dart';
import 'package:mobile_app/presentation/mobile_money_beneficiary_search_controller.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';

typedef _SearchLoader = Future<MobileMoneyBeneficiarySearchResult> Function(
  String phoneNumber,
);

class _FakeBeneficiaryLookupRepository
    implements MobileMoneyBeneficiaryLookupRepository {
  _FakeBeneficiaryLookupRepository(this.loader);

  final _SearchLoader loader;
  int callCount = 0;
  String? lastPhoneNumber;

  @override
  Future<MobileMoneyBeneficiarySearchResult> search({
    required String phoneNumber,
  }) {
    callCount++;
    lastPhoneNumber = phoneNumber;
    return loader(phoneNumber);
  }
}

MobileMoneyBeneficiarySearchResult _resolvedResult() {
  return const MobileMoneyBeneficiarySearchResult(
    lookup: MobileMoneyBeneficiaryLookup(
      normalizedPhoneNumber: '+237670123456',
      countryCode: 'CM',
      operator: 'MTN',
      operatorResolved: true,
      accountHolderName: 'Ada N.',
      beneficiaryResolved: true,
    ),
    operator: MobileMoneyBeneficiaryOperator.mtnCameroon,
  );
}

MobileMoneyBeneficiarySearchResult _manualResult() {
  return const MobileMoneyBeneficiarySearchResult(
    lookup: MobileMoneyBeneficiaryLookup(
      normalizedPhoneNumber: '+237660123456',
      countryCode: 'CM',
      operator: null,
      operatorResolved: false,
      accountHolderName: null,
      beneficiaryResolved: false,
    ),
    operator: null,
  );
}

Widget _app(
  MobileMoneyBeneficiarySearchController controller, {
  ValueChanged<MobileMoneyBeneficiarySearchResult>? onContinue,
  ValueChanged<String>? onManualEntry,
}) {
  return MaterialApp(
    home: MobileMoneyBeneficiarySearchPage(
      controller: controller,
      onContinue: onContinue,
      onManualEntry: onManualEntry,
    ),
  );
}

void main() {
  testWidgets('searches a phone number and renders resolved beneficiary details',
      (tester) async {
    final completer = Completer<MobileMoneyBeneficiarySearchResult>();
    final repository = _FakeBeneficiaryLookupRepository(
      (_) => completer.future,
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );
    MobileMoneyBeneficiarySearchResult? continuedResult;

    await tester.pumpWidget(
      _app(
        controller,
        onContinue: (result) => continuedResult = result,
      ),
    );

    await tester.enterText(
      find.byKey(const Key('momo-beneficiary-search-input')),
      ' +237670123456 ',
    );
    await tester.pump();
    await tester.tap(
      find.byKey(const Key('momo-beneficiary-search-submit')),
    );
    await tester.pump();

    expect(repository.callCount, 1);
    expect(repository.lastPhoneNumber, '+237670123456');
    expect(
      find.byKey(const Key('momo-beneficiary-search-loading')),
      findsOneWidget,
    );

    final resolved = _resolvedResult();
    completer.complete(resolved);
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('momo-beneficiary-search-resolved')),
      findsOneWidget,
    );
    expect(find.text('Ada N.'), findsOneWidget);
    expect(find.text('CM'), findsOneWidget);
    expect(find.text('MTN Mobile Money'), findsOneWidget);
    expect(find.text('+237670123456'), findsOneWidget);
    expect(continuedResult, isNull);

    await tester.tap(
      find.byKey(const Key('momo-beneficiary-search-continue')),
    );

    expect(continuedResult, same(resolved));
  });

  testWidgets('offers manual entry when lookup cannot fully resolve beneficiary',
      (tester) async {
    final repository = _FakeBeneficiaryLookupRepository(
      (_) async => _manualResult(),
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );
    String? manualPhoneNumber;

    await tester.pumpWidget(
      _app(
        controller,
        onManualEntry: (phoneNumber) => manualPhoneNumber = phoneNumber,
      ),
    );

    await tester.enterText(
      find.byKey(const Key('momo-beneficiary-search-input')),
      '+237660123456',
    );
    await tester.pump();
    await tester.tap(
      find.byKey(const Key('momo-beneficiary-search-submit')),
    );
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('momo-beneficiary-search-manual-required')),
      findsOneWidget,
    );
    expect(repository.callCount, 1);
    expect(manualPhoneNumber, isNull);

    await tester.tap(
      find.byKey(
        const Key('momo-beneficiary-search-manual-required-action'),
      ),
    );

    expect(manualPhoneNumber, '+237660123456');
  });

  testWidgets('renders failure and allows a fresh search attempt',
      (tester) async {
    var shouldFail = true;
    final repository = _FakeBeneficiaryLookupRepository((_) async {
      if (shouldFail) {
        throw StateError('lookup unavailable');
      }
      return _resolvedResult();
    });
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );

    await tester.pumpWidget(_app(controller));

    await tester.enterText(
      find.byKey(const Key('momo-beneficiary-search-input')),
      '+237670123456',
    );
    await tester.pump();
    await tester.tap(
      find.byKey(const Key('momo-beneficiary-search-submit')),
    );
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('momo-beneficiary-search-failed')),
      findsOneWidget,
    );
    expect(repository.callCount, 1);

    shouldFail = false;
    await tester.tap(
      find.byKey(const Key('momo-beneficiary-search-submit')),
    );
    await tester.pumpAndSettle();

    expect(repository.callCount, 2);
    expect(
      find.byKey(const Key('momo-beneficiary-search-resolved')),
      findsOneWidget,
    );
  });

  testWidgets('manual entry stays available before performing a lookup',
      (tester) async {
    final repository = _FakeBeneficiaryLookupRepository(
      (_) async => _resolvedResult(),
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );
    String? manualPhoneNumber;

    await tester.pumpWidget(
      _app(
        controller,
        onManualEntry: (phoneNumber) => manualPhoneNumber = phoneNumber,
      ),
    );

    await tester.enterText(
      find.byKey(const Key('momo-beneficiary-search-input')),
      '+237690123456',
    );
    await tester.tap(
      find.byKey(const Key('momo-beneficiary-search-manual')),
    );

    expect(manualPhoneNumber, '+237690123456');
    expect(repository.callCount, 0);
  });
}
