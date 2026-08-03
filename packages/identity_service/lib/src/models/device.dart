class Device {
  const Device({
    required this.id,
    required this.userId,
    required this.deviceName,
    required this.platform,
    required this.trusted,
    required this.createdAt,
    this.lastSeen,
  });

  final String id;
  final String userId;
  final String deviceName;
  final String platform;
  final bool trusted;
  final DateTime createdAt;
  final DateTime? lastSeen;
}
