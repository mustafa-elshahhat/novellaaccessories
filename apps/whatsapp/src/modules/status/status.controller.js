import { SERVICE_NAME, SERVICE_VERSION } from '../../config/constants.js';

export function statusController(config, client) {
  return (_req, res) => {
    const status = client.status();
    res.json({
      ...status,
      service: SERVICE_NAME,
      version: SERVICE_VERSION,
      environment: config.nodeEnv,
      connected: status.state === 'connected',
      keyConfigured: Boolean(config.internalApiKey),
      mongodbConfigured: Boolean(config.mongodbUri),
      pairingAdminTokenConfigured: Boolean(config.pairingAdminToken),
      pairingUiEnabled: Boolean(config.enablePairingUi),
      sendTimeoutMs: config.sendTimeoutMs,
      circuitBreaker: {
        threshold: config.circuitBreakerThreshold,
        cooldownMs: config.circuitBreakerCooldownMs,
      },
    });
  };
}
