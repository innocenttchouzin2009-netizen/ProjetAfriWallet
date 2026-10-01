import '../../models/current_identity_profile.dart';
import '../../models/public_afwal_identity.dart';
import '../../network/api_client.dart';
import '../../network/api_exception.dart';

class IdentityRemoteDataSource {
  const IdentityRemoteDataSource(this._apiClient);

  static const String currentProfilePath = '/api/v1/identity/current-profile';
  static const String publicAfWalIdPathPrefix = '/api/v1/identity/afwal-id';

  final ApiClient _apiClient;

  Future<CurrentIdentityProfile> loadCurrentProfile(String accessToken) async {
    final payload = await _apiClient.getJson(
      currentProfilePath,
      headers: <String, String>{'Authorization': 'Bearer $accessToken'},
    );
    if (payload is! Map<String, dynamic>) {
      throw const ApiMalformedResponseException(
        'The current identity profile endpoint returned an unexpected payload.',
      );
    }
    try {
      return CurrentIdentityProfile.fromJson(payload.cast<String, Object?>());
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The current identity profile endpoint returned an invalid profile.',
      );
    }
  }

  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) async {
    final payload = await _apiClient.getJson('$publicAfWalIdPathPrefix/$afWalId');
    if (payload is! Map<String, dynamic>) {
      throw const ApiMalformedResponseException(
        'The public AfWal ID endpoint returned an unexpected payload.',
      );
    }
    try {
      return PublicAfWalIdentity.fromJson(payload.cast<String, Object?>());
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The public AfWal ID endpoint returned an invalid identity.',
      );
    }
  }
}
