import { captureException } from './sentry';

type LogLevel = 'info' | 'warn' | 'error';

type LogMeta = Record<string, unknown>;

function write(level: LogLevel, message: string, meta?: LogMeta) {
  const payload = {
    level,
    message,
    timestamp: new Date().toISOString(),
    service: 'web',
    ...meta,
  };

  if (level === 'error') {
    console.error(JSON.stringify(payload));
    return;
  }

  if (level === 'warn') {
    console.warn(JSON.stringify(payload));
    return;
  }

  console.log(JSON.stringify(payload));
}

export const logger = {
  info(message: string, meta?: LogMeta) {
    write('info', message, meta);
  },
  warn(message: string, meta?: LogMeta) {
    write('warn', message, meta);
  },
  error(message: string, error?: unknown, meta?: LogMeta) {
    write('error', message, {
      ...meta,
      error: error instanceof Error ? { name: error.name, message: error.message, stack: error.stack } : error,
    });

    if (error) {
      captureException(error, {
        message,
        ...meta,
      });
    }
  },
};
