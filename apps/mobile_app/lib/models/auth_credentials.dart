class AuthCredentials {
  const AuthCredentials({
    required this.identifier,
    required this.password,
    required this.deviceId,
    required this.platform,
    this.deviceName,
  });

  final String identifier;
  final String password;
  final String deviceId;
  final String platform;
  final String? deviceName;

  Map<String, Object?> toJson() => <String, Object?>{
    'identifier': identifier,
    'password': password,
    'deviceId': deviceId,
    'platform': platform,
    'deviceName': deviceName,
  };
}
