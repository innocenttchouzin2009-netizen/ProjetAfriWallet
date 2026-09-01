import '../models/awid_profile.dart';

abstract interface class IdentityRepository {
  Future<AwidProfile> loadCurrentProfile();
}

class IdentityUnavailableException implements Exception {
  const IdentityUnavailableException();
}

class UnavailableIdentityRepository implements IdentityRepository {
  const UnavailableIdentityRepository();

  @override
  Future<AwidProfile> loadCurrentProfile() async {
    throw const IdentityUnavailableException();
  }
}
