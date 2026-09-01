import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/theme/afwal_theme.dart';

void main() {
  group('AfWalTheme', () {
    test('uses official deep green as primary color', () {
      final theme = AfWalTheme.light();
      expect(theme.colorScheme.primary, AfWalColors.deepGreen);
    });

    test('uses official gold as secondary color', () {
      final theme = AfWalTheme.light();
      expect(theme.colorScheme.secondary, AfWalColors.gold);
    });

    test('uses Material 3', () {
      expect(AfWalTheme.light().useMaterial3, isTrue);
    });
  });
}
