class Verification {
  const Verification({
    required this.id,
    required this.userId,
    required this.channel,
    required this.codeHash,
    required this.expiresAt,
    required this.attempts,
    required this.verified,
  });

  final String id;
  final String userId;
  final String channel;
  final String codeHash;
  final DateTime expiresAt;
  final int attempts;
  final bool verified;
}
