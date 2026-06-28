import * as Sentry from '@sentry/nextjs';

const dsn = process.env.SENTRY_DSN;
const environment = process.env.SENTRY_ENVIRONMENT ?? process.env.NODE_ENV ?? 'development';

let initialized = false;

export function initSentry() {
  if (initialized || !dsn) {
    return;
  }

  Sentry.init({
    dsn,
    environment,
    tracesSampleRate: Number(process.env.SENTRY_TRACES_SAMPLE_RATE ?? 0.2),
  });

  initialized = true;
}

export function captureException(error: unknown, context?: Record<string, unknown>) {
  if (!dsn) {
    return;
  }

  initSentry();
  Sentry.captureException(error, {
    extra: context,
  });
}

export function captureMessage(message: string, context?: Record<string, unknown>) {
  if (!dsn) {
    return;
  }

  initSentry();
  Sentry.captureMessage(message, {
    extra: context,
  });
}
