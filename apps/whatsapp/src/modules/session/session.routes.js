import { requireInternalApiKey } from '../../middleware/auth.middleware.js';
import { logoutController } from './session.controller.js';

export function registerSessionRoutes(app, config, client) {
  app.post('/api/logout', requireInternalApiKey(config), logoutController(client));
}
