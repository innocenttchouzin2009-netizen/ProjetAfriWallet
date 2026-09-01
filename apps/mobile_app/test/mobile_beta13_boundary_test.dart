import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/services/identity_repository.dart';

void main() {
  test('production identity repository does not fabricate an AfWal ID', () async {
    const repository = UnavailableIdentityRepository();

    expect(
      repository.loadCurrentProfile,
      throwsA(isA<IdentityUnavailableException>()),
    );
  });
}
