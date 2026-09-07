import '../../network/api_client.dart';
import '../../network/api_exception.dart';

class HealthStatus {
  const HealthStatus({required this.status});

  final String status;

  bool get isOk => status.toLowerCase() == 'ok';
}

class HealthRemoteDataSource {
  const HealthRemoteDataSource(this._apiClient);

  final ApiClient _apiClient;

  Future<HealthStatus> check() async {
    final payload = await _apiClient.getJson('/health');

    if (payload is! Map<String, dynamic>) {
      throw const ApiMalformedResponseException(
        'The health endpoint returned an unexpected payload.',
      );
    }

    final status = payload['status'];
    if (status is! String || status.trim().isEmpty) {
      throw const ApiMalformedResponseException(
        'The health endpoint did not return a valid status.',
      );
    }

    return HealthStatus(status: status);
  }
}
