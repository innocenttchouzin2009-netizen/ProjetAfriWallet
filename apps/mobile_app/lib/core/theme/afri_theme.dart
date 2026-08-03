import 'package:flutter/material.dart';

class AfriTheme {
  static ThemeData light() {
    const background = Color(0xFFF4F1E8);
    const ink = Color(0xFF0D2A23);
    const accent = Color(0xFF0E8D6C);
    const highlight = Color(0xFFD6B35E);

    final scheme = ColorScheme.fromSeed(
      seedColor: accent,
      brightness: Brightness.light,
      surface: Colors.white,
    );

    return ThemeData(
      colorScheme: scheme,
      scaffoldBackgroundColor: background,
      useMaterial3: true,
      textTheme: const TextTheme(
        headlineMedium: TextStyle(
          fontFamily: 'Georgia',
          fontWeight: FontWeight.w700,
          color: ink,
          letterSpacing: 0.2,
        ),
        titleLarge: TextStyle(
          fontFamily: 'Georgia',
          fontWeight: FontWeight.w700,
          color: ink,
        ),
        bodyMedium: TextStyle(
          color: ink,
        ),
      ),
      chipTheme: const ChipThemeData(
        selectedColor: accent,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: background,
        foregroundColor: ink,
        elevation: 0,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: accent,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          side: const BorderSide(color: highlight),
          foregroundColor: ink,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
      ),
    );
  }
}
