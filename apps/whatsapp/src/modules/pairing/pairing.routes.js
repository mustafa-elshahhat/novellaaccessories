import { requirePairingToken } from '../../middleware/auth.middleware.js';
import { pairingController } from './pairing.controller.js';

export function registerPairingRoutes(app, config, client) {
  const auth = requirePairingToken(config);
  app.get(['/pair', '/qr', '/qr/:token', '/qr:token'], auth, pairingController(config, client));
}
