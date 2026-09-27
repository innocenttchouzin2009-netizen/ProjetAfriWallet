import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import 'read_cache.dart';

typedef ReadCacheEncoder<T> = Object? Function(T value);
typedef ReadCacheDecoder<T> = T Function(Object? value);

class SharedPreferencesReadCache<T> implements ReadCache<T> {
  const SharedPreferencesReadCache({
    required this.key,
    required ReadCacheEncoder<T> encode,
    required ReadCacheDecoder<T> decode,
  }) : _encode = encode,
       _decode = decode;

  final String key;
  final ReadCacheEncoder<T> _encode;
  final ReadCacheDecoder<T> _decode;

  @override
  Future<CachedRead<T>?> read() async {
    final preferences = await SharedPreferences.getInstance();
    final raw = preferences.getString(key);
    if (raw == null) {
      return null;
    }

    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map) {
        await preferences.remove(key);
        return null;
      }

      final envelope = Map<String, Object?>.from(decoded);
      final cachedAtRaw = envelope['cachedAtUtc'];
      if (cachedAtRaw is! String || !envelope.containsKey('payload')) {
        await preferences.remove(key);
        return null;
      }

      final cachedAtUtc = DateTime.tryParse(cachedAtRaw);
      if (cachedAtUtc == null) {
        await preferences.remove(key);
        return null;
      }

      return CachedRead<T>(
        value: _decode(envelope['payload']),
        cachedAtUtc: cachedAtUtc.toUtc(),
      );
    } on Object {
      await preferences.remove(key);
      return null;
    }
  }

  @override
  Future<void> write(CachedRead<T> entry) async {
    final preferences = await SharedPreferences.getInstance();
    final payload = jsonEncode({
      'cachedAtUtc': entry.cachedAtUtc.toUtc().toIso8601String(),
      'payload': _encode(entry.value),
    });
    await preferences.setString(key, payload);
  }

  @override
  Future<void> clear() async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.remove(key);
  }
}
