class ApiConfig {
  static const String baseUrl = String.fromEnvironment(
    'AFW_API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000',
  );
}
