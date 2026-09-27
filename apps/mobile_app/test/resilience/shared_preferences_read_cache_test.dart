import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/resilience/read_cache.dart';
import 'package:mobile_app/resilience/shared_preferences_read_cache.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  SharedPreferencesReadCache<Map<String, Object?>> createCache() {
    return SharedPreferencesReadCache<Map<String, Object?>>(
      key: 'wallet-read-cache',
      encode: (value) => value,
      decode: (value) => Map<String, Object?>.from(value! as Map),
    );
  }

  test('persists and restores a timestamped read value', () async {
    final cache = createCache();
    final timestamp = DateTime.utc(2026, 9, 27, 20);

    await cache.write(
      CachedRead(
        value: const {'currency': 'EUR', 'balance': 42},
        cachedAtUtc: timestamp,
      ),
    );

    final restored = await cache.read();

    expect(restored?.value, const {'currency': 'EUR', 'balance': 42});
    expect(restored?.cachedAtUtc, timestamp);
  });

  test('treats corrupted cache as unavailable and removes it', () async {
    SharedPreferences.setMockInitialValues(
      <String, Object>{'wallet-read-cache': 'not-json'},
    );
    final cache = createCache();

    expect(await cache.read(), isNull);

    final preferences = await SharedPreferences.getInstance();
    expect(preferences.containsKey('wallet-read-cache'), isFalse);
  });

  test('clear removes persisted read data', () async {
    final cache = createCache();
    await cache.write(
      CachedRead(
        value: const {'currency': 'EUR'},
        cachedAtUtc: DateTime.utc(2026, 9, 27, 20),
      ),
    );

    await cache.clear();

    expect(await cache.read(), isNull);
  });
}
