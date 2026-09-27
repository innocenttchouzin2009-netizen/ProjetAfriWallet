enum ResilientReadSource {
  remote,
  cache,
}

class ResilientReadResult<T> {
  const ResilientReadResult({
    required this.value,
    required this.source,
    required this.observedAtUtc,
    this.cachedAtUtc,
  });

  final T value;
  final ResilientReadSource source;
  final DateTime observedAtUtc;
  final DateTime? cachedAtUtc;

  bool get isOfflineFallback => source == ResilientReadSource.cache;
}
