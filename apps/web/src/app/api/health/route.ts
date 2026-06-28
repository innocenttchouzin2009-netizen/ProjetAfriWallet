import { NextResponse } from 'next/server';
import { prisma } from '@/lib/prisma';

export const dynamic = 'force-dynamic';

export async function GET() {
  const startedAt = Date.now();

  try {
    await prisma.$queryRaw`SELECT 1`;
    return NextResponse.json({
      status: 'ok',
      service: 'web',
      uptimeSeconds: Math.floor(process.uptime()),
      db: 'up',
      latencyMs: Date.now() - startedAt,
      timestamp: new Date().toISOString(),
    });
  } catch (error) {
    return NextResponse.json(
      {
        status: 'degraded',
        service: 'web',
        db: 'down',
        latencyMs: Date.now() - startedAt,
        error: error instanceof Error ? error.message : 'Unknown database error',
        timestamp: new Date().toISOString(),
      },
      { status: 503 },
    );
  }
}
