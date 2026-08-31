import 'package:flutter/material.dart';

abstract final class AfWalColors {
  static const deepGreen = Color(0xFF063D32);
  static const green = Color(0xFF0B5D4B);
  static const gold = Color(0xFFD6A84B);
  static const warmWhite = Color(0xFFF8F7F2);
  static const ink = Color(0xFF13211D);
}

abstract final class AfWalTheme {
  static ThemeData light() {
    final scheme = ColorScheme.fromSeed(
      seedColor: AfWalColors.deepGreen,
      brightness: Brightness.light,
      primary: AfWalColors.deepGreen,
      secondary: AfWalColors.gold,
      surface: AfWalColors.warmWhite,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: AfWalColors.warmWhite,
      appBarTheme: const AppBarTheme(
        backgroundColor: AfWalColors.warmWhite,
        foregroundColor: AfWalColors.ink,
        centerTitle: false,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size.fromHeight(54),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
        ),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      ),
    );
  }
}
