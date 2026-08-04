import '../models/device.dart';
import '../models/identity.dart';
import '../models/user.dart';
import '../models/verification.dart';
import '../validators/auth_validators.dart';

class AuthService {
  const AuthService();

  User registerUser({
    required String email,
    required String phone,
    required String language,
    required String country,
  }) {
    if (!AuthValidators.isValidEmail(email)) {
      throw ArgumentError('Invalid email');
    }
    if (!AuthValidators.isValidPhone(phone)) {
      throw ArgumentError('Invalid phone');
    }

    return User(
      id: 'user-${DateTime.now().microsecondsSinceEpoch}',
      email: email,
      phone: phone,
      status: UserStatus.pending,
      language: language,
      country: country,
      createdAt: DateTime.now(),
    );
  }

  Verification createVerification({
    required String userId,
    required String code,
    required String channel,
  }) {
    if (!AuthValidators.isValidOtp(code)) {
      throw ArgumentError('Invalid OTP');
    }

    return Verification(
      id: 'verification-${DateTime.now().microsecondsSinceEpoch}',
      userId: userId,
      channel: channel,
      codeHash: code,
      expiresAt: DateTime.now().add(const Duration(minutes: 5)),
      attempts: 0,
      verified: false,
    );
  }

  Device registerDevice({
    required String userId,
    required String deviceName,
    required String platform,
  }) {
    return Device(
      id: 'device-${DateTime.now().microsecondsSinceEpoch}',
      userId: userId,
      deviceName: deviceName,
      platform: platform,
      trusted: true,
      createdAt: DateTime.now(),
    );
  }

  Identity createIdentity({
    required String userId,
    required String firstName,
    required String lastName,
    required DateTime birthDate,
    required String country,
    required String residence,
    required String language,
  }) {
    return Identity(
      userId: userId,
      firstName: firstName,
      lastName: lastName,
      birthDate: birthDate,
      country: country,
      residence: residence,
      language: language,
    );
  }
}
