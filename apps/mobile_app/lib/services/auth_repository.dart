import '../models/auth_credentials.dart';
import '../models/auth_session.dart';

abstract interface class AuthRepository {
  Future<AuthSession> login(AuthCredentials credentials);

  Future<AuthSession> refresh(String refreshToken);

  Future<void> logout();

  Future<void> logoutAll();

  Future<AuthSession?> loadCurrentSession();
}
