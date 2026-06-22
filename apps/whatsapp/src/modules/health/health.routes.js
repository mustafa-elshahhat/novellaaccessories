import { healthController } from './health.controller.js';

export function registerHealthRoutes(app, config, client) {
  app.get('/health', healthController(config, client));
}
