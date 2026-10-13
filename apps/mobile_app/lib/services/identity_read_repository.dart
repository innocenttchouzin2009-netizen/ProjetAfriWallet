import '../data/remote/identity_remote_data_source.dart';
import '../models/current_identity_profile.dart';
import '../models/public_afwal_identity.dart';
import '../network/api_exception.dart';
import 'authenticated_request_executor.dart';

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
  const RemoteIdentityReadRepository(
    this._remoteDataSource,
    this._requestExecutor,
  );

  final IdentityRemoteDataSource _remoteDataSource;
  final AuthenticatedRequestExecutor _requestExecutor;

  @override
  Future<CurrentIdentityProfile> loadCurrentProfile() async {
    try {
      return await _requestExecutor.execute(
        _remoteDataSource.loadCurrentProfile,
      );
    } on ApiUnauthorizedException {
      throw const IdentityReadUnavailableException();
    }
  }

  @override
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) =>
      _remoteDataSource.loadPublicAfWalId(afWalId);
}
