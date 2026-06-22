import { SERVICE_NAME, SERVICE_VERSION } from '../../config/constants.js';

export function healthController(config, client) {
  return (_req, res) => {
    const status = client.status();
    res.json({
      ok: true,
      service: SERVICE_NAME,
      version: SERVICE_VERSION,
      environment: config.nodeEnv,
      whatsappState: status.state,
      connected: status.state === 'connected',
      mongodbConfigured: Boolean(config.mongodbUri),
      keyConfigured: Boolean(config.internalApiKey),
    });
  };
}
