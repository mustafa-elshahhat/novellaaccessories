import { logger } from '../config/logger.js';

export function errorMiddleware(err, _req, res, _next) {
  // body-parser attaches the raw request body to parse/size errors. Never log the
  // full error for these — it would leak unmasked phone numbers and message content.
  // Log only safe metadata instead.
  if (err?.type === 'entity.parse.failed') {
    logger.warn({ type: err.type, statusCode: 400 }, 'Rejected malformed JSON body');
    return res.status(400).json({ error: 'invalid_json' });
  }
  if (err?.type === 'entity.too.large') {
    logger.warn({ type: err.type, statusCode: 413 }, 'Rejected oversized request body');
    return res.status(413).json({ error: 'payload_too_large' });
  }

  logger.error({ err }, 'Unhandled error');

  const statusCode = Number.isInteger(err?.statusCode) ? err.statusCode : 500;
  const error = statusCode === 500
    ? 'internal_error'
    : (err instanceof Error ? err.message : 'internal_error');
  res.status(statusCode).json({ error });
}

