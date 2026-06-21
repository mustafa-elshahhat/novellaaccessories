import express from 'express';

import { registerHealthRoutes } from './modules/health/health.routes.js';
import { registerStatusRoutes } from './modules/status/status.routes.js';
import { registerPairingRoutes } from './modules/pairing/pairing.routes.js';
import { registerMessagesRoutes } from './modules/messages/messages.routes.js';
import { registerSessionRoutes } from './modules/session/session.routes.js';
import { notFoundMiddleware } from './middleware/notFound.middleware.js';
import { errorMiddleware } from './middleware/error.middleware.js';

export function createApp(config, client, circuitBreaker) {
  const app = express();
  app.disable('x-powered-by');
  app.use(express.json({ limit: '64kb' }));

  registerHealthRoutes(app, client);
  registerStatusRoutes(app, config, client);
  registerPairingRoutes(app, config, client);
  registerMessagesRoutes(app, config, client, circuitBreaker);
  registerSessionRoutes(app, config, client);

  app.use(notFoundMiddleware);
  app.use(errorMiddleware);

  return app;
}
