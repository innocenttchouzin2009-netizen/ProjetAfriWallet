class PublicAfWalIdentity {
  const PublicAfWalIdentity({
    required this.afWalId,
  });

  final String afWalId;

  factory PublicAfWalIdentity.fromJson(Map<String, Object?> json) {
    final value = json['afWalId'];
    if (value is! String || value.trim().isEmpty) {
      throw const FormatException('Missing or invalid afWalId.');
    }

    return PublicAfWalIdentity(afWalId: value);
  }
}
