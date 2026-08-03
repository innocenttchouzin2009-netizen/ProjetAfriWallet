import 'package:identity_service/identity_service.dart';
import 'package:test/test.dart';

void main() {
  group('AuthService', () {
    test('registers a valid user', () {
      final service = AuthService();
      final user = service.registerUser(
        email: 'user@example.com',
        phone: '+33123456789',
        language: 'fr',
        country: 'FR',
      );

      expect(user.status, UserStatus.pending);
      expect(user.email, 'user@example.com');
    });

    test('rejects invalid email', () {
      final service = AuthService();
      expect(
        () => service.registerUser(
          email: 'bad-email',
          phone: '+33123456789',
          language: 'fr',
          country: 'FR',
        ),
        throwsArgumentError,
      );
    });

    test('rejects weak pin', () {
      expect(AuthValidators.isValidPin('111111'), isFalse);
      expect(AuthValidators.isValidPin('123456'), isFalse);
      expect(AuthValidators.isValidPin('000000'), isFalse);
      expect(AuthValidators.isValidPin('654321'), isFalse);
    });

    test('accepts valid otp', () {
      expect(AuthValidators.isValidOtp('123456'), isTrue);
      expect(AuthValidators.isValidOtp('12345'), isFalse);
    });
  });
}
