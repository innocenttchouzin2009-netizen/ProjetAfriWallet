import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_beneficiary_lookup_remote_data_source.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';

void main() {
  group('RemoteMobileMoneyBeneficiaryLookupRepository', () {
    test('maps a resolved MTN beneficiary to the canonical payout operator',
        () async {
      final repository = _repositoryForPayload(
        _payload(
          operator: 'MTN',
          accountHolderName: 'Ada N.',
          beneficiaryResolved: true,
        ),
      );

      final result = await repository.search(phoneNumber: ' 670123456 ');

      expect(result.lookup.normalizedPhoneNumber, '+237670123456');
      expect(result.lookup.countryCode, 'CM');
      expect(result.operator, MobileMoneyBeneficiaryOperator.mtnCameroon);
      expect(result.operatorCode, 'MTN_CM');
      expect(result.isFullyResolved, isTrue);
      expect(result.requiresManualEntry, isFalse);
    });

    test('maps Orange case-insensitively to the canonical payout operator',
        () async {
      final repository = _repositoryForPayload(
        _payload(
          operator: 'Orange',
          accountHolderName: 'Binta N.',
          beneficiaryResolved: true,
        ),
      );

      final result = await repository.search(phoneNumber: '690123456');

      expect(result.operator, MobileMoneyBeneficiaryOperator.orangeCameroon);
      expect(result.operatorCode, 'ORANGE_CM');
      expect(result.isFullyResolved, isTrue);
    });

    test('preserves an unresolved lookup as a manual-entry result', () async {
      final repository = _repositoryForPayload(
        _payload(
          operator: null,
          operatorResolved: false,
          accountHolderName: null,
          beneficiaryResolved: false,
        ),
      );

      final result = await repository.search(phoneNumber: '660123456');

      expect(result.operator, isNull);
      expect(result.operatorCode, isNull);
      expect(result.isFullyResolved, isFalse);
      expect(result.requiresManualEntry, isTrue);
    });

    test('rejects a resolved but unsupported operator contract', () async {
      final repository = _repositoryForPayload(
        _payload(
          operator: 'UNKNOWN',
          accountHolderName: 'Ada N.',
          beneficiaryResolved: true,
        ),
      );

      await expectLater(
        repository.search(phoneNumber: '670123456'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
    });

    test('rejects a resolved beneficiary without a holder name', () async {
      final repository = _repositoryForPayload(
        _payload(
          operator: 'MTN',
          accountHolderName: null,
          beneficiaryResolved: true,
        ),
      );

      await expectLater(
        repository.search(phoneNumber: '670123456'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
    });

    test('rejects an empty phone number before the network call', () async {
      var called = false;
      final apiClient = ApiClient(
        baseUrl: 'https://api.afrikawallet.test',
        httpClient: MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );
      addTearDown(apiClient.close);
      final repository = RemoteMobileMoneyBeneficiaryLookupRepository(
        MobileMoneyBeneficiaryLookupRemoteDataSource(apiClient),
      );

      await expectLater(
        repository.search(phoneNumber: '   '),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });
  });
}

RemoteMobileMoneyBeneficiaryLookupRepository _repositoryForPayload(
  Map<String, Object?> payload,
) {
  final apiClient = ApiClient(
    baseUrl: 'https://api.afrikawallet.test',
    httpClient: MockClient(
      (_) async => http.Response(
        jsonEncode(payload),
        200,
        headers: <String, String>{'content-type': 'application/json'},
      ),
    ),
  );
  addTearDown(apiClient.close);

  return RemoteMobileMoneyBeneficiaryLookupRepository(
    MobileMoneyBeneficiaryLookupRemoteDataSource(apiClient),
  );
}

Map<String, Object?> _payload({
  required String? operator,
  bool operatorResolved = true,
  required String? accountHolderName,
  required bool beneficiaryResolved,
}) {
  return <String, Object?>{
    'normalizedPhoneNumber': '+237670123456',
    'countryCode': 'CM',
    'operator': operator,
    'operatorResolved': operatorResolved,
    'accountHolderName': accountHolderName,
    'beneficiaryResolved': beneficiaryResolved,
  };
}
