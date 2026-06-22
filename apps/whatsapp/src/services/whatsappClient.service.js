import makeWASocket, { DisconnectReason, fetchLatestBaileysVersion } from '@whiskeysockets/baileys';
import qrcode from 'qrcode';

import { useMongoAuthState } from './mongoAuthState.service.js';
import { WhatsappAuthRepository } from '../repositories/whatsappAuth.repository.js';
import { logger } from '../config/logger.js';
import { maskPhone, toWhatsAppJid } from '../utils/phone.util.js';

// Baileys logs raw connection/session details (JIDs, payload metadata) at
// debug/trace levels. Cap its logger at warn to avoid leaking that data
// regardless of the application's LOG_LEVEL.
const baileysLogger = logger.child({ module: 'baileys' }, { level: 'warn' });

export class WhatsAppClientService {
  constructor(config, rateLimiter) {
    this.config = config;
    this.rateLimiter = rateLimiter;
    this.authRepo = new WhatsappAuthRepository(config.mongodbUri);
    this.sock = null;
    this.starting = null;
    this.state = config.mongodbUri ? 'initializing' : 'configuration_error';
    this.qrDataUri = null;
    this.lastSentAt = null;
    this.lastProviderMessageId = null;
    this.lastError = config.mongodbUri ? null : 'MONGODB_URI is required';
    this.reconnectTimer = null;
  }

  async initialize() {
    if (!this.config.mongodbUri) return;
    if (this.starting) return this.starting;

    this.starting = this.connect().finally(() => {
      this.starting = null;
    });
    return this.starting;
  }

  async connect() {
    try {
      this.state = 'initializing';
      await this.authRepo.ensureConnection();
      const collection = this.authRepo.getCollection();
      const { state, saveCreds } = await useMongoAuthState(collection);
      const { version } = await fetchLatestBaileysVersion();

      this.sock = makeWASocket({
        version,
        auth: state,
        logger: baileysLogger,
      });

      this.sock.ev.on('creds.update', saveCreds);
      this.sock.ev.on('connection.update', async (update) => {
        const { connection, lastDisconnect, qr } = update;

        if (qr) {
          this.qrDataUri = await qrcode.toDataURL(qr);
          this.state = 'qr_required';
          this.lastError = null;
          logger.info('WhatsApp QR refreshed');
        }

        if (connection === 'open') {
          this.state = 'connected';
          this.qrDataUri = null;
          this.lastError = null;
          logger.info('WhatsApp connected');
        }

        if (connection === 'close') {
          const statusCode = lastDisconnect?.error?.output?.statusCode;
          const loggedOut = statusCode === DisconnectReason.loggedOut;
          this.sock = null;
          this.state = loggedOut ? 'auth_failed' : 'disconnected';
          this.lastError = lastDisconnect?.error?.message ?? 'connection_closed';
          logger.warn({ statusCode, loggedOut }, 'WhatsApp connection closed');
          if (!loggedOut) this.scheduleReconnect();
        }
      });
    } catch (err) {
      this.state = 'disconnected';
      this.lastError = err instanceof Error ? err.message : 'startup_failed';
      logger.error({ err }, 'WhatsApp connect failed');
      this.scheduleReconnect();
    }
  }

  scheduleReconnect() {
    if (this.reconnectTimer) return;
    this.reconnectTimer = setTimeout(() => {
      this.reconnectTimer = null;
      void this.initialize();
    }, 5000);
  }

  status() {
    return {
      state: this.state,
      qrAvailable: Boolean(this.qrDataUri),
      lastSentAt: this.lastSentAt,
      lastProviderMessageId: this.lastProviderMessageId,
      error: this.lastError,
    };
  }

  pair() {
    return {
      state: this.state,
      qrAvailable: Boolean(this.qrDataUri),
      qrDataUri: this.qrDataUri,
    };
  }

  async sendMessage(phone, message) {
    if (!this.sock || this.state !== 'connected') {
      const err = new Error('not_connected');
      err.statusCode = 503;
      throw err;
    }

    const jid = toWhatsAppJid(phone);
    if (!jid || !message) {
      const err = new Error('phone_and_message_required');
      err.statusCode = 400;
      throw err;
    }

    const reservation = this.rateLimiter.reserve(phone);
    if (!reservation.ok) {
      const err = new Error(reservation.reason);
      err.statusCode = 429;
      throw err;
    }

    const timeoutMs = this.config.sendTimeoutMs ?? 30000;
    let timer;
    try {
      const result = await Promise.race([
        this.sock.sendMessage(jid, { text: String(message) }),
        new Promise((_, reject) => {
          timer = setTimeout(() => {
            const err = new Error('send_timeout');
            err.statusCode = 503;
            // Not retryable: the underlying Baileys send may still complete,
            // so retrying risks duplicate delivery.
            err.retryable = false;
            reject(err);
          }, timeoutMs);
        }),
      ]);
      this.lastProviderMessageId = result?.key?.id ?? null;
    } catch (err) {
      // send_timeout is ambiguous (the message may still be delivered), so
      // only release the reservation for definitive non-delivery failures.
      if (err.message !== 'send_timeout') {
        this.rateLimiter.release(phone, reservation);
      }
      throw err;
    } finally {
      clearTimeout(timer);
    }

    this.lastSentAt = new Date().toISOString();
    this.lastError = null;
    logger.info({ phone: maskPhone(phone), providerMessageId: this.lastProviderMessageId }, 'WhatsApp message sent');
    return { providerMessageId: this.lastProviderMessageId };
  }

  async logout() {
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.reconnectTimer = null;

    try {
      await this.sock?.logout?.();
    } catch (err) {
      logger.warn({ err }, 'WhatsApp socket logout failed; clearing auth state anyway');
    }

    try {
      this.sock?.end?.();
    } catch {
      // Socket may already be closed after logout.
    }

    this.sock = null;
    await this.authRepo.ensureConnection();
    await this.authRepo.clearAll();
    this.state = 'disconnected';
    this.qrDataUri = null;
    this.lastError = null;
    logger.info('WhatsApp auth state cleared by logout');
    await this.initialize();
  }

  async shutdown() {
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.reconnectTimer = null;
    try {
      this.sock?.end?.();
    } catch {
      // Baileys socket may already be closed during Render shutdown.
    }
    await this.authRepo.close();
  }
}
