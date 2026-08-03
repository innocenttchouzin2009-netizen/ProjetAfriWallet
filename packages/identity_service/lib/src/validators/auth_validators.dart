class AuthValidators {
  static bool isValidEmail(String value) {
    return RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(value);
  }

  static bool isValidPhone(String value) {
    final normalized = value.replaceAll(RegExp(r'\s+'), '');
    return RegExp(r'^\+?[0-9]{7,15}$').hasMatch(normalized);
  }

  static bool isValidPin(String value) {
    if (value.length != 6) return false;
    final forbidden = <String>{'111111', '123456', '000000', '654321'};
    return !forbidden.contains(value) && !RegExp(r'(\d)\1{5}').hasMatch(value);
  }

  static bool isValidOtp(String value) {
    return RegExp(r'^\d{6}$').hasMatch(value);
  }
}
