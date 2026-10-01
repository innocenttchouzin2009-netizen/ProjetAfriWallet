import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/current_identity_profile.dart';
import 'package:mobile_app/models/public_afwal_identity.dart';

void main() {
  group('CurrentIdentityProfile', () {
    test('parses the authoritative current-profile contract', () {
      final profile = CurrentIdentityProfile.fromJson(<String, Object?>{
        'userId': '8fc35fe0-0f60-48e4-9df7-8269ad497721',
        'identifier': 'user@example.com',
        'createdAtUtc': '2026-10-01T09:30:00Z',
      });

      expect(profile.userId, '8fc35fe0-0f60-48e4-9df7-8269ad497721');
      expect(profile.identifier, 'user@example.com');
      expect(profile.createdAtUtc, DateTime.utc(2026, 10, 1, 9, 30));
    });

    test('rejects missing or malformed required fields', () {
      expect(
        () => CurrentIdentityProfile.fromJson(<String, Object?>{
          'userId': '',
          'identifier': 'user@example.com',
          'createdAtUtc': '2026-10-01T09:30:00Z',
        }),
        throwsFormatException,
      );

      expect(
        () => CurrentIdentityProfile.fromJson(<String, Object?>{
          'userId': '8fc35fe0-0f60-48e4-9df7-8269ad497721',
          'identifier': 'user@example.com',
          'createdAtUtc': 'not-a-date',
        }),
        throwsFormatException,
      );
    });
  });

  group('PublicAfWalIdentity', () {
    test('parses the authoritative public AfWal ID contract', () {
      final identity = PublicAfWalIdentity.fromJson(<String, Object?>{
        'afWalId': 'public.user',
      });

      expect(identity.afWalId, 'public.user');
    });

    test('rejects a missing or blank AfWal ID', () {
      expect(
        () => PublicAfWalIdentity.fromJson(<String, Object?>{}),
        throwsFormatException,
      );

      expect(
        () => PublicAfWalIdentity.fromJson(<String, Object?>{
          'afWalId': '   ',
        }),
        throwsFormatException,
      );
    });
  });
}
