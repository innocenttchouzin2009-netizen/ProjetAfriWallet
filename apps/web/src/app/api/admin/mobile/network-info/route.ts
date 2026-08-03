import { networkInterfaces } from 'node:os';
import { NextRequest, NextResponse } from 'next/server';
import { requireRole } from '@/features/auth/guards/require-role';

export const dynamic = 'force-dynamic';

function parseHostHeader(hostHeader: string | null) {
  if (!hostHeader) return { host: null as string | null, port: null as string | null };

  const value = hostHeader.trim();
  if (!value) return { host: null as string | null, port: null as string | null };

  const lastColonIndex = value.lastIndexOf(':');
  if (lastColonIndex > -1 && value.indexOf(':') === lastColonIndex) {
    return {
      host: value.slice(0, lastColonIndex),
      port: value.slice(lastColonIndex + 1),
    };
  }

  return { host: value, port: null as string | null };
}

function isPrivateIpv4(value: string) {
  return value.startsWith('10.') || value.startsWith('192.168.') || /^172\.(1[6-9]|2\d|3[0-1])\./.test(value);
}

function getLocalIpv4Addresses() {
  const interfaces = networkInterfaces();
  const all = new Set<string>();
  const prioritized: string[] = [];

  for (const entries of Object.values(interfaces)) {
    if (!entries) continue;
    for (const entry of entries) {
      if (!entry || entry.family !== 'IPv4' || entry.internal) continue;
      if (!all.has(entry.address)) {
        all.add(entry.address);
        if (isPrivateIpv4(entry.address)) {
          prioritized.push(entry.address);
        }
      }
    }
  }

  const remaining = [...all].filter((item) => !prioritized.includes(item));
  return [...prioritized, ...remaining];
}

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager', 'production', 'vendor', 'support']);
  if (auth instanceof NextResponse) {
    return auth;
  }

  const protocol = request.headers.get('x-forwarded-proto') || 'http';
  const hostHeader = request.headers.get('x-forwarded-host') || request.headers.get('host');
  const parsedHost = parseHostHeader(hostHeader);
  const port = parsedHost.port || (protocol === 'https' ? '443' : '3000');

  const ipAddresses = getLocalIpv4Addresses();
  const originCandidates = ipAddresses.map((ip) => `${protocol}://${ip}:${port}`);

  return NextResponse.json({
    protocol,
    port,
    ipAddresses,
    originCandidates,
  });
}
