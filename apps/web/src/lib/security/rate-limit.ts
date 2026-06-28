type RateLimitBucket = {
  count: number;
  resetAt: number;
};

const buckets = new Map<string, RateLimitBucket>();

export type RateLimitOptions = {
  windowMs: number;
  max: number;
};

export function evaluateRateLimit(key: string, options: RateLimitOptions) {
  const now = Date.now();
  const existing = buckets.get(key);

  if (!existing || existing.resetAt <= now) {
    const resetAt = now + options.windowMs;
    buckets.set(key, { count: 1, resetAt });
    return {
      allowed: true,
      remaining: options.max - 1,
      resetAt,
    };
  }

  existing.count += 1;

  const allowed = existing.count <= options.max;
  return {
    allowed,
    remaining: Math.max(0, options.max - existing.count),
    resetAt: existing.resetAt,
  };
}
