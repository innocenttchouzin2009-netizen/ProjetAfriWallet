import { PrismaClient } from '@prisma/client';

const globalForPrisma = globalThis as unknown as {
  prisma: PrismaClient | undefined;
};

const FALLBACK_DATABASE_URL = 'postgresql://postgres:postgres@127.0.0.1:5432/postgres?schema=public';

const isProduction = process.env.NODE_ENV === 'production';
const databaseUrl = process.env.DATABASE_URL ?? (isProduction ? undefined : FALLBACK_DATABASE_URL);

if (!databaseUrl) {
  throw new Error('Missing DATABASE_URL in production environment.');
}

process.env.DATABASE_URL = databaseUrl;

export const prisma =
  globalForPrisma.prisma ??
  new PrismaClient({
    datasources: {
      db: {
        url: databaseUrl,
      },
    },
    log: process.env.NODE_ENV === 'development' ? ['query', 'warn', 'error'] : ['error'],
  });

if (process.env.NODE_ENV !== 'production') {
  globalForPrisma.prisma = prisma;
}
