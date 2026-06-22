export const MIN_SECRET_LENGTH = 32;
export const DEFAULT_PORT = 4000;
export const DEFAULT_SEND_TIMEOUT_MS = 30000;
export const DEFAULT_SEND_DELAY_MIN_MS = 5000;
export const DEFAULT_SEND_DELAY_MAX_MS = 15000;
export const DEFAULT_DAILY_PHONE_LIMIT = 10;
export const DEFAULT_GLOBAL_SEND_LIMIT_PER_MINUTE = 60;
export const CIRCUIT_BREAKER_THRESHOLD = 3;
export const CIRCUIT_BREAKER_COOLDOWN_MS = 30_000;
export const SERVICE_NAME = 'novella-whatsapp';
export const SERVICE_VERSION = '1.0.0';

// MongoDB defaults. The database name is fixed for the service; the connection
// string is supplied via MONGODB_URI and must never be hardcoded here.
export const MONGODB_DATABASE = 'novella_whatsapp';
export const AUTH_STATE_COLLECTION = 'baileys_auth_state';

