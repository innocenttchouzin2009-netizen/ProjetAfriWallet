import 'package:flutter/material.dart';

import 'afw_colors.dart';

abstract final class AfwTheme {
  static ThemeData light() => _theme(
        brightness: Brightness.light,
        background: AfwColors.backgroundLight,
        surface: AfwColors.surfaceLight,
        onSurface: AfwColors.textPrimaryLight,
      );

  static ThemeData dark() => _theme(
        brightness: Brightness.dark,
        background: AfwColors.backgroundDark,
        surface: AfwColors.surfaceDark,
        onSurface: AfwColors.textPrimaryDark,
      );

  static ThemeData _theme({
    required Brightness brightness,
    required Color background,
    required Color surface,
    required Color onSurface,
  }) {
    final ColorScheme scheme = ColorScheme.fromSeed(
      seedColor: AfwColors.emerald,
      brightness: brightness,
      surface: surface,
      error: AfwColors.error,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      scaffoldBackgroundColor: background,
      appBarTheme: AppBarTheme(
        backgroundColor: background,
        foregroundColor: onSurface,
        elevation: 0,
        centerTitle: false,
      ),
      cardTheme: CardThemeData(
        color: surface,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(20),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surface,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide.none,
        ),
      ),
    );
  }
}
