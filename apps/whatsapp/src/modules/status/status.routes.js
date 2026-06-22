import { requirePairingToken } from '../../middleware/auth.middleware.js';
import { statusController } from './status.controller.js';

export function registerStatusRoutes(app, config, client) {
  app.get('/status', requirePairingToken(config), statusController(config, client));
}
