import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Holds the current locale and provides persistence.
class LocaleController {
  LocaleController._(this._prefs, this._locale);

  final SharedPreferences _prefs;
  Locale _locale;

  Locale get currentLocale => _locale;

  static const _key = 'locale';

  static Future<LocaleController> load() async {
    final prefs = await SharedPreferences.getInstance();
    final code = prefs.getString(_key) ?? 'en';
    return LocaleController._(prefs, Locale(code));
  }

  Future<void> save(String languageCode) async {
    _locale = Locale(languageCode);
    await _prefs.setString(_key, languageCode);
  }
}
