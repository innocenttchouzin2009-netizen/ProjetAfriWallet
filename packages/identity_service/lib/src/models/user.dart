enum UserStatus {
  pending,
  otpVerified,
  pinCreated,
  deviceTrusted,
  active,
  suspended,
  closed,
}

class User {
  const User({
    required this.id,
    required this.email,
    required this.phone,
    required this.status,
    required this.language,
    required this.country,
    required this.createdAt,
    this.updatedAt,
  });

  final String id;
  final String email;
  final String phone;
  final UserStatus status;
  final String language;
  final String country;
  final DateTime createdAt;
  final DateTime? updatedAt;
}
