import { requireInternalApiKey } from '../../middleware/auth.middleware.js';
import { sendMessageController, sendTemplateController } from './messages.controller.js';

export function registerMessagesRoutes(app, config, client, circuitBreaker) {
  const auth = requireInternalApiKey(config);

  app.post('/send-message', auth, sendMessageController(client, circuitBreaker));
  app.post('/send-template', auth, sendTemplateController(client, circuitBreaker));
}
