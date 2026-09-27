import '../network/api_exception.dart';
import 'read_cache.dart';

class ReadResiliencePolicy {
  const ReadResiliencePolicy({
    required this.maxCacheAge,
  });

  final Duration maxCacheAge;

  bool canFallbackFor(Object error) {
    return error is ApiNetworkException ||
        error is ApiTimeoutException ||
        error is ApiServerException;
  }

  bool isCacheUsable<T>(CachedRead<T> cached, DateTime nowUtc) {
    final age = nowUtc.toUtc().difference(cached.cachedAtUtc.toUtc());
    return !age.isNegative && age.compareTo(maxCacheAge) <= 0;
  }
}
