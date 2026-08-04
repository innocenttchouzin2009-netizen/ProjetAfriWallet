import 'package:identity_service/identity_service.dart';
import 'package:test/test.dart';

void main() {
  group('IdentityService', () {
    test('registers a new user', () {
      final service = IdentityService();
      final response = service.register(
        const RegisterRequest(
          email: 'user@example.com',
          phone: '+33123456789',
          language: 'fr',
          country: 'FR',
        ),
      );

      expect(response.status, 'PENDING');
      expect(response.userId, isNotEmpty);
    });

    test('creates an otp verification', () {
      final service = IdentityService();
      final verification = service.sendOtp(const OtpRequest(userId: 'user-1', channel: 'sms'));

      expect(verification.userId, 'user-1');
      expect(verification.channel, 'sms');
    });
  });
}
