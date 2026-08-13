import 'package:flutter/material.dart';
import 'afw_colors.dart';

class AfwTheme {
  static ThemeData light() {
    return ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(AfwColors.primary)),
      useMaterial3: true,
    );
  }

  static ThemeData dark() {
    return ThemeData.dark(useMaterial3: true).copyWith(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(AfwColors.primary)),
    );
  }
}
