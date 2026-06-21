import {
  MIN_SECRET_LENGTH,
  DEFAULT_PORT,
  DEFAULT_SEND_DELAY_MIN_MS,
  DEFAULT_SEND_DELAY_MAX_MS,
  DEFAULT_DAILY_PHONE_LIMIT,
  DEFAULT_GLOBAL_SEND_LIMIT_PER_MINUTE,
  DEFAULT_SEND_TIMEOUT_MS,
  CIRCUIT_BREAKER_THRESHOLD,
  CIRCUIT_BREAKER_COOLDOWN_MS,
} from './constants.js';

export function loadConfig() {
  const internalApiKey = process.env.INTERNAL_API_KEY ?? '';
  const pairingAdminToken = process.env.PAIRING_ADMIN_TOKEN ?? '';

  if (!internalApiKey) {
    throw new Error('INTERNAL_API_KEY environment variable is required but not set.');
  }
  if (internalApiKey.length < MIN_SECRET_LENGTH) {
    throw new Error(`INTERNAL_API_KEY must be at least ${MIN_SECRET_LENGTH} characters long.`);
  }
  // PAIRING_ADMIN_TOKEN is optional — when absent, the pairing endpoints accept
  // INTERNAL_API_KEY instead (see requirePairingToken in auth.middleware.js).
  if (pairingAdminToken && pairingAdminToken.length < MIN_SECRET_LENGTH) {
    throw new Error(`PAIRING_ADMIN_TOKEN must be at least ${MIN_SECRET_LENGTH} characters long when set.`);
  }

  return {
    port: intFromEnv('PORT', DEFAULT_PORT, 1),
    nodeEnv: process.env.NODE_ENV ?? 'development',
    mongodbUri: process.env.MONGODB_URI ?? '',
    internalApiKey,
    pairingAdminToken,
    enablePairingUi: boolFromEnv('ENABLE_PAIRING_UI', false),
    sendDelayMinMs: intFromEnv('SEND_DELAY_MIN_MS', DEFAULT_SEND_DELAY_MIN_MS),
    sendDelayMaxMs: intFromEnv('SEND_DELAY_MAX_MS', DEFAULT_SEND_DELAY_MAX_MS),
    dailyPhoneLimit: intFromEnv('DAILY_PHONE_LIMIT', DEFAULT_DAILY_PHONE_LIMIT),
    globalSendLimitPerMinute: intFromEnv('GLOBAL_SEND_LIMIT_PER_MINUTE', DEFAULT_GLOBAL_SEND_LIMIT_PER_MINUTE),
    sendTimeoutMs: intFromEnv('SEND_TIMEOUT_MS', DEFAULT_SEND_TIMEOUT_MS),
    circuitBreakerThreshold: intFromEnv('CIRCUIT_BREAKER_THRESHOLD', CIRCUIT_BREAKER_THRESHOLD),
    circuitBreakerCooldownMs: intFromEnv('CIRCUIT_BREAKER_COOLDOWN_MS', CIRCUIT_BREAKER_COOLDOWN_MS),
  };
}

function intFromEnv(name, fallback, min = 0) {
  const value = Number(process.env[name]);
  return Number.isFinite(value) && value >= min ? value : fallback;
}

function boolFromEnv(name, fallback) {
  const raw = process.env[name];
  if (raw === undefined) return fallback;
  return ['1', 'true', 'yes', 'on'].includes(raw.toLowerCase());
}
