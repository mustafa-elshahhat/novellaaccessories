import { healthController } from './health.controller.js';

export function registerHealthRoutes(app, client) {
  app.get('/health', healthController(client));
}
