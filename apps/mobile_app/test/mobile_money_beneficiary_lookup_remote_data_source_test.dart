import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_beneficiary_lookup_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_beneficiary_lookup_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  const requestDto = MobileMoneyBeneficiaryLookupRequestDto(
    phoneNumber: '670123456',
  );

  test('posts the exact beneficiary lookup backend contract and maps success',
      () async {
    late http.Request capturedRequest;

    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient((request) async {
        capturedRequest = request;
        return http.Response(
          jsonEncode(<String, Object?>{
            'normalizedPhoneNumber': '+237670123456',
            'countryCode': 'CM',
            'operator': 'MTN',
            'operatorResolved': true,
            'accountHolderName': 'Ada N.',
            'beneficiaryResolved': true,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      }),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyBeneficiaryLookupRemoteDataSource(apiClient);

    final result = await dataSource.lookup(requestDto);

    expect(capturedRequest.method, 'POST');
    expect(
      capturedRequest.url.path,
      '/api/v1/mobile-money/beneficiaries/lookup',
    );
    expect(
      jsonDecode(capturedRequest.body),
      <String, Object?>{'phoneNumber': '670123456'},
    );
    expect(result.normalizedPhoneNumber, '+237670123456');
    expect(result.countryCode, 'CM');
    expect(result.operator, 'MTN');
    expect(result.operatorResolved, isTrue);
    expect(result.accountHolderName, 'Ada N.');
    expect(result.beneficiaryResolved, isTrue);
  });

  test('preserves unresolved beneficiary lookup for manual fallback', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode(<String, Object?>{
            'normalizedPhoneNumber': '+237660123456',
            'countryCode': 'CM',
            'operator': null,
            'operatorResolved': false,
            'accountHolderName': null,
            'beneficiaryResolved': false,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        ),
      ),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyBeneficiaryLookupRemoteDataSource(apiClient);

    final result = await dataSource.lookup(requestDto);

    expect(result.normalizedPhoneNumber, '+237660123456');
    expect(result.operator, isNull);
    expect(result.operatorResolved, isFalse);
    expect(result.accountHolderName, isNull);
    expect(result.beneficiaryResolved, isFalse);
  });

  test('maps an invalid lookup payload to an API response error', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode(<String, Object?>{
            'normalizedPhoneNumber': '+237670123456',
            'countryCode': 'CM',
            'operator': 'MTN',
            'operatorResolved': true,
            'accountHolderName': 'Ada N.',
            'beneficiaryResolved': 'true',
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        ),
      ),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyBeneficiaryLookupRemoteDataSource(apiClient);

    expect(
      () => dataSource.lookup(requestDto),
      throwsA(isA<ApiMalformedResponseException>()),
    );
  });

  test('preserves backend HTTP failures from ApiClient', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient((_) async => http.Response('bad request', 400)),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyBeneficiaryLookupRemoteDataSource(apiClient);

    expect(
      () => dataSource.lookup(requestDto),
      throwsA(isA<ApiHttpException>()),
    );
  });
}
