import { NextRequest, NextResponse } from 'next/server';
import { evaluateRateLimit } from '@/lib/security/rate-limit';
import { readRequestAuth, type AppRole } from '@/features/auth/guards/require-role';

const CSRF_COOKIE = 'dcs-csrf-token';

function isMutating(method: string) {
  return method === 'POST' || method === 'PUT' || method === 'PATCH' || method === 'DELETE';
}

function buildCsp() {
  const isProd = process.env.NODE_ENV === 'production';
  const scriptSrc = isProd
    ? "script-src 'self' 'unsafe-inline' https://js.stripe.com https://www.paypal.com"
    : "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://js.stripe.com https://www.paypal.com";
  const connectSrc = isProd
    ? "connect-src 'self' https://api.stripe.com https://www.paypal.com https://www.sandbox.paypal.com"
    : "connect-src 'self' ws: wss: https://api.stripe.com https://www.paypal.com https://www.sandbox.paypal.com";

  const directives = [
    "default-src 'self'",
    scriptSrc,
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data: https:",
    "font-src 'self' data:",
    connectSrc,
    "frame-src https://js.stripe.com https://hooks.stripe.com https://www.paypal.com",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'none'",
  ];

  return directives.join('; ');
}

function ensureSecurityHeaders(response: NextResponse) {
  response.headers.set('Content-Security-Policy', buildCsp());
  response.headers.set('X-Frame-Options', 'DENY');
  response.headers.set('X-Content-Type-Options', 'nosniff');
  response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');
  response.headers.set('Permissions-Policy', 'camera=(), microphone=(), geolocation=()');
  return response;
}

function isOriginAllowed(request: NextRequest): boolean {
  const origin = request.headers.get('origin');
  if (!origin) return true;

  const host = request.headers.get('host');
  if (!host) return false;

  try {
    const requestOrigin = new URL(origin);
    return requestOrigin.host === host;
  } catch {
    return false;
  }
}

function ensureCsrfCookie(request: NextRequest, response: NextResponse) {
  if (!request.cookies.get(CSRF_COOKIE)) {
    response.cookies.set(CSRF_COOKIE, crypto.randomUUID(), {
      httpOnly: false,
      sameSite: 'lax',
      secure: process.env.NODE_ENV === 'production',
      path: '/',
      maxAge: 60 * 60 * 24,
    });
  }
}

function allowedAdminRolesForPath(pathname: string): AppRole[] {
  if (pathname.startsWith('/admin/products')) return ['manager'];
  if (pathname.startsWith('/admin/orders')) return ['manager', 'production', 'support'];
  if (pathname.startsWith('/admin/inventory')) return ['manager'];
  if (pathname.startsWith('/admin/payments')) return ['manager'];
  if (pathname.startsWith('/admin/pos')) return ['vendor', 'manager'];
  if (pathname.startsWith('/admin/audit')) return ['manager', 'support'];
  if (pathname.startsWith('/admin/health')) return ['manager', 'support'];
  return ['manager', 'production', 'vendor', 'support'];
}

export function middleware(request: NextRequest) {
  const pathname = request.nextUrl.pathname;
  const isApi = pathname.startsWith('/api/');

  if (pathname.startsWith('/admin')) {
    const auth = readRequestAuth(request);
    if (!auth) {
      const url = new URL('/login', request.url);
      url.searchParams.set('next', pathname);
      return NextResponse.redirect(url);
    }

    if (auth.role !== 'super-admin') {
      const allowed = allowedAdminRolesForPath(pathname);
      if (!allowed.includes(auth.role)) {
        return NextResponse.redirect(new URL('/', request.url));
      }
    }
  }

  if (isApi) {
    const ip = request.ip ?? request.headers.get('x-forwarded-for') ?? 'unknown';
    const windowMs = 60_000;
    const max = pathname.startsWith('/api/auth/') ? 20 : pathname.startsWith('/api/admin/') ? 80 : 120;
    const rate = evaluateRateLimit(`${ip}:${pathname}`, { windowMs, max });

    if (!rate.allowed) {
      return NextResponse.json(
        { message: 'Too many requests. Please retry shortly.' },
        {
          status: 429,
          headers: {
            'Retry-After': String(Math.ceil((rate.resetAt - Date.now()) / 1000)),
          },
        },
      );
    }

    if (isMutating(request.method) && !pathname.includes('/webhook/')) {
      if (!isOriginAllowed(request)) {
        return NextResponse.json({ message: 'CSRF protection blocked this request.' }, { status: 403 });
      }
    }
  }

  const response = NextResponse.next();
  ensureCsrfCookie(request, response);
  ensureSecurityHeaders(response);
  return response;
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
};
