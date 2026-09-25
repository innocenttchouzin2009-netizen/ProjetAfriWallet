import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/wallet_contracts.dart';

void main() {
  group('WalletResponse', () {
    test('parses the backend wallet contract exactly', () {
      final wallet = WalletResponse.fromJson(<String, Object?>{
        'walletId': '11111111-1111-1111-1111-111111111111',
        'ownerId': '22222222-2222-2222-2222-222222222222',
        'currencyCode': 'EUR',
        'countryCode': 'DE',
        'status': 'ACTIVE',
        'createdAtUtc': '2026-09-24T10:00:00Z',
        'updatedAtUtc': '2026-09-24T11:00:00Z',
      });

      expect(wallet.walletId, '11111111-1111-1111-1111-111111111111');
      expect(wallet.ownerId, '22222222-2222-2222-2222-222222222222');
      expect(wallet.currencyCode, 'EUR');
      expect(wallet.countryCode, 'DE');
      expect(wallet.status, 'ACTIVE');
      expect(wallet.isActive, isTrue);
      expect(wallet.createdAtUtc.isUtc, isTrue);
      expect(wallet.updatedAtUtc.isUtc, isTrue);
    });

    test('accepts nullable countryCode without inventing a value', () {
      final wallet = WalletResponse.fromJson(<String, Object?>{
        'walletId': '11111111-1111-1111-1111-111111111111',
        'ownerId': '22222222-2222-2222-2222-222222222222',
        'currencyCode': 'XAF',
        'countryCode': null,
        'status': 'ACTIVE',
        'createdAtUtc': '2026-09-24T10:00:00Z',
        'updatedAtUtc': '2026-09-24T10:00:00Z',
      });

      expect(wallet.countryCode, isNull);
    });

    test('rejects incomplete wallet payloads', () {
      expect(
        () => WalletResponse.fromJson(<String, Object?>{
          'walletId': '11111111-1111-1111-1111-111111111111',
          'currencyCode': 'EUR',
        }),
        throwsA(isA<FormatException>()),
      );
    });
  });
}
