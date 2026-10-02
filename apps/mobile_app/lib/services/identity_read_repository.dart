import '../data/remote/identity_remote_data_source.dart';
import '../models/current_identity_profile.dart';
import '../models/public_afwal_identity.dart';
import 'secure_session_store.dart';

abstract interface class IdentityReadRepository {
  Future<CurrentIdentityProfile> loadCurrentProfile();
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId);
}

class IdentityReadUnavailableException implements Exception {
  const IdentityReadUnavailableException();
}

class UnavailableIdentityReadRepository implements IdentityReadRepository {
  const UnavailableIdentityReadRepository();

  @override
  Future<CurrentIdentityProfile> loadCurrentProfile() async {
    throw const IdentityReadUnavailableException();
  }

  @override
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) async {
    throw const IdentityReadUnavailableException();
  }
}

class RemoteIdentityReadRepository implements IdentityReadRepository {
  const RemoteIdentityReadRepository(this._remoteDataSource, this._sessionStore);

  final IdentityRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;

  @override
  Future<CurrentIdentityProfile> loadCurrentProfile() async {
    final session = await _sessionStore.read();
    if (session == null) {
      throw const IdentityReadUnavailableException();
    }
    return _remoteDataSource.loadCurrentProfile(session.accessToken);
  }

  @override
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) =>
      _remoteDataSource.loadPublicAfWalId(afWalId);
}
