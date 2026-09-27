class CachedRead<T> {
  const CachedRead({
    required this.value,
    required this.cachedAtUtc,
  });

  final T value;
  final DateTime cachedAtUtc;
}

abstract interface class ReadCache<T> {
  Future<CachedRead<T>?> read();

  Future<void> write(CachedRead<T> entry);

  Future<void> clear();
}
