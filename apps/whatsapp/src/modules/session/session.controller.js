import { logger } from '../../config/logger.js';

export function logoutController(client) {
  return async (_req, res) => {
    try {
      await client.logout();
      res.status(204).send();
    } catch (err) {
      const statusCode = Number.isInteger(err?.statusCode) ? err.statusCode : 500;
      if (statusCode === 500) {
        logger.error({ err }, 'Logout error');
      }
      const error = statusCode === 500
        ? 'internal_error'
        : (err instanceof Error ? err.message : 'internal_error');
      res.status(statusCode).json({ error });
    }
  };
}

