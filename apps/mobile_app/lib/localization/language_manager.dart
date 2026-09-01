import 'locale_controller.dart';

/// Convenience factory that creates a [LocaleController].
class LanguageManager {
  LanguageManager._();

  static Future<LocaleController> create() => LocaleController.load();
}
