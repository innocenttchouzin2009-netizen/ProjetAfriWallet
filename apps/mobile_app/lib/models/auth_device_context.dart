class AuthDeviceContext {
  const AuthDeviceContext({
    required this.deviceId,
    required this.platform,
    this.deviceName,
  });

  final String deviceId;
  final String platform;
  final String? deviceName;
}
