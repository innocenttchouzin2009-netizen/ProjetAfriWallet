class RegisterRequest {
  const RegisterRequest({
    required this.email,
    required this.phone,
    required this.language,
    required this.country,
  });

  final String email;
  final String phone;
  final String language;
  final String country;
}

class RegisterResponse {
  const RegisterResponse({required this.userId, required this.status});

  final String userId;
  final String status;
}

class OtpRequest {
  const OtpRequest({required this.userId, required this.channel});

  final String userId;
  final String channel;
}

class PinRequest {
  const PinRequest({required this.userId, required this.pin});

  final String userId;
  final String pin;
}
