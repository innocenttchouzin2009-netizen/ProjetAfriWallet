import '../contracts/auth_contracts.dart';
import '../models/device.dart';
import '../models/identity.dart';
import '../models/user.dart';
import '../models/verification.dart';
import '../validators/auth_validators.dart';

class IdentityService {
  const IdentityService();

  RegisterResponse register(RegisterRequest request) {
    if (!AuthValidators.isValidEmail(request.email)) {
      throw ArgumentError('Invalid email');
    }
    if (!AuthValidators.isValidPhone(request.phone)) {
      throw ArgumentError('Invalid phone');
    }

    return RegisterResponse(
      userId: 'user-${DateTime.now().microsecondsSinceEpoch}',
      status: 'PENDING',
    );
  }

  Verification sendOtp(OtpRequest request) {
    if (!AuthValidators.isValidOtp('123456')) {
      throw ArgumentError('Invalid OTP');
    }

    return Verification(
      id: 'verification-${DateTime.now().microsecondsSinceEpoch}',
      userId: request.userId,
      channel: request.channel,
      codeHash: 'hashed-code',
      expiresAt: DateTime.now().add(const Duration(minutes: 5)),
      attempts: 0,
      verified: false,
    );
  }

  Device registerDevice({required String userId, required String deviceName, required String platform}) {
    return Device(
      id: 'device-${DateTime.now().microsecondsSinceEpoch}',
      userId: userId,
      deviceName: deviceName,
      platform: platform,
      trusted: true,
      createdAt: DateTime.now(),
    );
  }

  Identity createIdentity({required String userId, required String firstName, required String lastName, required DateTime birthDate, required String country, required String residence, required String language}) {
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
