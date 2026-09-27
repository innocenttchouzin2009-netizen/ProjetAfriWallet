import 'read_cache.dart';
import 'read_resilience_policy.dart';
import 'resilient_read_result.dart';

typedef RemoteRead<T> = Future<T> Function();
typedef UtcNow = DateTime Function();

class OfflineReadResolver<T> {
  const OfflineReadResolver({
    required RemoteRead<T> remoteRead,
    required ReadCache<T> cache,
    required ReadResiliencePolicy policy,
    required UtcNow utcNow,
  }) : _remoteRead = remoteRead,
       _cache = cache,
       _policy = policy,
       _utcNow = utcNow;

  final RemoteRead<T> _remoteRead;
  final ReadCache<T> _cache;
  final ReadResiliencePolicy _policy;
  final UtcNow _utcNow;

  Future<ResilientReadResult<T>> load() async {
    try {
      final value = await _remoteRead();
      final observedAtUtc = _utcNow().toUtc();

      try {
        await _cache.write(
          CachedRead<T>(
            value: value,
            cachedAtUtc: observedAtUtc,
          ),
        );
      } on Object {
        // A local cache failure must never hide a successful remote read.
      }

      return ResilientReadResult<T>(
        value: value,
        source: ResilientReadSource.remote,
        observedAtUtc: observedAtUtc,
      );
    } catch (error, stackTrace) {
      if (!_policy.canFallbackFor(error)) {
        Error.throwWithStackTrace(error, stackTrace);
      }

      final cached = await _readCacheOrRethrow(error, stackTrace);
      final observedAtUtc = _utcNow().toUtc();

      if (cached == null || !_policy.isCacheUsable(cached, observedAtUtc)) {
        Error.throwWithStackTrace(error, stackTrace);
      }

      return ResilientReadResult<T>(
        value: cached.value,
        source: ResilientReadSource.cache,
        observedAtUtc: observedAtUtc,
        cachedAtUtc: cached.cachedAtUtc.toUtc(),
      );
    }
  }

  Future<CachedRead<T>?> _readCacheOrRethrow(
    Object remoteError,
    StackTrace remoteStackTrace,
  ) async {
    try {
      return await _cache.read();
    } on Object {
      Error.throwWithStackTrace(remoteError, remoteStackTrace);
    }
  }
}
