import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_lookup.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/presentation/mobile_money_beneficiary_search_controller.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';

typedef _SearchLoader = Future<MobileMoneyBeneficiarySearchResult> Function(
  String phoneNumber,
);

class _FakeMobileMoneyBeneficiaryLookupRepository
    implements MobileMoneyBeneficiaryLookupRepository {
  _FakeMobileMoneyBeneficiaryLookupRepository(this._loader);

  final _SearchLoader _loader;
  int callCount = 0;

  @override
  Future<MobileMoneyBeneficiarySearchResult> search({
    required String phoneNumber,
  }) {
    callCount++;
    return _loader(phoneNumber);
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

void main() {
  test('fully resolved search exposes resolved state', () async {
    final expected = _resolvedResult();
    final repository = _FakeMobileMoneyBeneficiaryLookupRepository(
      (phoneNumber) async {
        expect(phoneNumber, '670123456');
        return expected;
      },
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );

    await controller.search(phoneNumber: '670123456');

    expect(controller.status, MobileMoneyBeneficiarySearchStatus.resolved);
    expect(controller.result, same(expected));
    expect(controller.isResolved, isTrue);
    expect(controller.requiresManualEntry, isFalse);
    expect(controller.error, isNull);
    expect(repository.callCount, 1);
  });

  test('unresolved search exposes manual-entry-required state', () async {
    final expected = _manualResult();
    final repository = _FakeMobileMoneyBeneficiaryLookupRepository(
      (_) async => expected,
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );

    await controller.search(phoneNumber: '660123456');

    expect(
      controller.status,
      MobileMoneyBeneficiarySearchStatus.manualEntryRequired,
    );
    expect(controller.result, same(expected));
    expect(controller.requiresManualEntry, isTrue);
    expect(controller.isResolved, isFalse);
    expect(controller.error, isNull);
  });

  test('repository failure exposes failed state without inventing a result',
      () async {
    final failure = StateError('beneficiary lookup unavailable');
    final repository = _FakeMobileMoneyBeneficiaryLookupRepository(
      (_) async => throw failure,
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );

    await controller.search(phoneNumber: '670123456');

    expect(controller.status, MobileMoneyBeneficiarySearchStatus.failed);
    expect(controller.hasFailed, isTrue);
    expect(controller.result, isNull);
    expect(controller.error, same(failure));
  });

  test('duplicate concurrent searches are ignored while loading', () async {
    final completer = Completer<MobileMoneyBeneficiarySearchResult>();
    final repository = _FakeMobileMoneyBeneficiaryLookupRepository(
      (_) => completer.future,
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );

    final first = controller.search(phoneNumber: '670123456');
    final duplicate = controller.search(phoneNumber: '690123456');

    expect(controller.isSearching, isTrue);
    expect(repository.callCount, 1);

    completer.complete(_resolvedResult());
    await Future.wait(<Future<void>>[first, duplicate]);

    expect(repository.callCount, 1);
    expect(controller.isResolved, isTrue);
  });

  test('reset clears result and failure state', () async {
    final repository = _FakeMobileMoneyBeneficiaryLookupRepository(
      (_) async => _resolvedResult(),
    );
    final controller = MobileMoneyBeneficiarySearchController(
      repository: repository,
    );

    await controller.search(phoneNumber: '670123456');
    controller.reset();

    expect(controller.status, MobileMoneyBeneficiarySearchStatus.idle);
    expect(controller.result, isNull);
    expect(controller.error, isNull);
    expect(controller.isResolved, isFalse);
    expect(controller.requiresManualEntry, isFalse);
    expect(controller.hasFailed, isFalse);
  });
}
