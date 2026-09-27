import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/resilience/offline_read_resolver.dart';
import 'package:mobile_app/resilience/read_cache.dart';
import 'package:mobile_app/resilience/read_resilience_policy.dart';
import 'package:mobile_app/resilience/resilient_read_result.dart';

void main() {
  final now = DateTime.utc(2026, 9, 27, 20);

  group('OfflineReadResolver', () {
    test('returns remote data and refreshes cache on success', () async {
      final cache = _FakeReadCache<String>();
      final resolver = OfflineReadResolver<String>(
        remoteRead: () async => 'remote',
        cache: cache,
        policy: const ReadResiliencePolicy(maxCacheAge: Duration(hours: 1)),
        utcNow: () => now,
      );

      final result = await resolver.load();

      expect(result.value, 'remote');
      expect(result.source, ResilientReadSource.remote);
      expect(result.isOfflineFallback, isFalse);
      expect(cache.entry?.value, 'remote');
      expect(cache.entry?.cachedAtUtc, now);
    });

    test('uses fresh cache for a network failure', () async {
      final cache = _FakeReadCache<String>(
        CachedRead(
          value: 'cached',
          cachedAtUtc: now.subtract(const Duration(minutes: 20)),
        ),
      );
      final resolver = OfflineReadResolver<String>(
        remoteRead: () async => throw const ApiNetworkException('offline'),
        cache: cache,
        policy: const ReadResiliencePolicy(maxCacheAge: Duration(hours: 1)),
        utcNow: () => now,
      );

      final result = await resolver.load();

      expect(result.value, 'cached');
      expect(result.source, ResilientReadSource.cache);
      expect(result.isOfflineFallback, isTrue);
      expect(
        result.cachedAtUtc,
        now.subtract(const Duration(minutes: 20)),
      );
    });

    test('uses fresh cache for a timeout or server failure', () async {
      for (final error in <ApiException>[
        const ApiTimeoutException('timeout'),
        const ApiServerException(statusCode: 503),
      ]) {
        final cache = _FakeReadCache<String>(
          CachedRead(
            value: 'cached',
            cachedAtUtc: now.subtract(const Duration(minutes: 5)),
          ),
        );
        final resolver = OfflineReadResolver<String>(
          remoteRead: () async => throw error,
          cache: cache,
          policy: const ReadResiliencePolicy(maxCacheAge: Duration(hours: 1)),
          utcNow: () => now,
        );

        final result = await resolver.load();

        expect(result.value, 'cached');
        expect(result.isOfflineFallback, isTrue);
      }
    });

    test('does not hide authentication failures with cached data', () async {
      final cache = _FakeReadCache<String>(
        CachedRead(value: 'cached', cachedAtUtc: now),
      );
      final resolver = OfflineReadResolver<String>(
        remoteRead: () async => throw const ApiUnauthorizedException(),
        cache: cache,
        policy: const ReadResiliencePolicy(maxCacheAge: Duration(hours: 1)),
        utcNow: () => now,
      );

      await expectLater(
        resolver.load(),
        throwsA(isA<ApiUnauthorizedException>()),
      );
    });

    test('rejects cache older than the configured maximum age', () async {
      final cache = _FakeReadCache<String>(
        CachedRead(
          value: 'too-old',
          cachedAtUtc: now.subtract(const Duration(hours: 2)),
        ),
      );
      final resolver = OfflineReadResolver<String>(
        remoteRead: () async => throw const ApiNetworkException('offline'),
        cache: cache,
        policy: const ReadResiliencePolicy(maxCacheAge: Duration(hours: 1)),
        utcNow: () => now,
      );

      await expectLater(
        resolver.load(),
        throwsA(isA<ApiNetworkException>()),
      );
    });

    test('rethrows remote failure when cache read also fails', () async {
      final cache = _FakeReadCache<String>()..throwOnRead = true;
      final resolver = OfflineReadResolver<String>(
        remoteRead: () async => throw const ApiNetworkException('offline'),
        cache: cache,
        policy: const ReadResiliencePolicy(maxCacheAge: Duration(hours: 1)),
        utcNow: () => now,
      );

      await expectLater(
        resolver.load(),
        throwsA(isA<ApiNetworkException>()),
      );
    });

    test('cache write failure does not mask successful remote data', () async {
      final cache = _FakeReadCache<String>()..throwOnWrite = true;
      final resolver = OfflineReadResolver<String>(
        remoteRead: () async => 'remote',
        cache: cache,
        policy: const ReadResiliencePolicy(maxCacheAge: Duration(hours: 1)),
        utcNow: () => now,
      );

      final result = await resolver.load();

      expect(result.value, 'remote');
      expect(result.source, ResilientReadSource.remote);
    });
  });
}

class _FakeReadCache<T> implements ReadCache<T> {
  _FakeReadCache([this.entry]);

  CachedRead<T>? entry;
  bool throwOnRead = false;
  bool throwOnWrite = false;

  @override
  Future<CachedRead<T>?> read() async {
    if (throwOnRead) {
      throw StateError('cache read failed');
    }
    return entry;
  }

  @override
  Future<void> write(CachedRead<T> value) async {
    if (throwOnWrite) {
      throw StateError('cache write failed');
    }
    entry = value;
  }

  @override
  Future<void> clear() async {
    entry = null;
  }
}
