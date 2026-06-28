import { NextRequest, NextResponse } from 'next/server';

type Bucket = {
  count: number;
  resetAt: number;
};

type Store = Map<string, Bucket>;

const globalStore = globalThis as unknown as {
  dcsRateLimitStore?: Store;
};

const store: Store = globalStore.dcsRateLimitStore ?? new Map<string, Bucket>();
if (!globalStore.dcsRateLimitStore) {
  globalStore.dcsRateLimitStore = store;
}

export type IpRateLimitRule = {
  scope: string;
  max: number;
  windowMs: number;
};

export function getClientIp(request: NextRequest): string {
  const forwarded = request.headers.get('x-forwarded-for');
  if (forwarded) {
    const [first] = forwarded.split(',');
    return (first ?? 'unknown').trim();
  }

  return request.ip ?? 'unknown';
}

function takeToken(key: string, max: number, windowMs: number) {
  const now = Date.now();
  const current = store.get(key);

  if (!current || current.resetAt <= now) {
    const resetAt = now + windowMs;
    store.set(key, { count: 1, resetAt });
    return {
      allowed: true,
      remaining: Math.max(0, max - 1),
      resetAt,
    };
  }

  current.count += 1;

  return {
    allowed: current.count <= max,
    remaining: Math.max(0, max - current.count),
    resetAt: current.resetAt,
  };
}

export function enforceIpRateLimit(request: NextRequest, rule: IpRateLimitRule): NextResponse | null {
  const ip = getClientIp(request);
  const key = `${rule.scope}:${ip}`;
  const result = takeToken(key, rule.max, rule.windowMs);

  if (result.allowed) {
    return null;
  }

  const retryAfterSeconds = Math.max(1, Math.ceil((result.resetAt - Date.now()) / 1000));

  return NextResponse.json(
    {
      message: 'Too Many Requests',
    },
    {
      status: 429,
      headers: {
        'Retry-After': String(retryAfterSeconds),
        'X-RateLimit-Limit': String(rule.max),
        'X-RateLimit-Remaining': String(result.remaining),
        'X-RateLimit-Reset': String(Math.floor(result.resetAt / 1000)),
      },
    },
  );
}
