import '../models/current_identity_profile.dart';
import '../models/public_afwal_identity.dart';

abstract interface class IdentityReadRepository {
  Future<CurrentIdentityProfile> loadCurrentProfile();

  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId);
}
