import { describe, it, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import request from 'supertest';

import { createApp } from '../src/app.js';

const API_KEY = 'x'.repeat(32);
const ADMIN_TOKEN = 'y'.repeat(32);

function fakeConfig(overrides = {}) {
  return {
    internalApiKey: API_KEY,
    pairingAdminToken: ADMIN_TOKEN,
    enablePairingUi: false,
    sendDelayMinMs: 0,
    sendDelayMaxMs: 0,
    dailyPhoneLimit: 10,
    globalSendLimitPerMinute: 60,
    sendTimeoutMs: 30000,
    ...overrides,
  };
}

function fakeClient(state = 'qr_required', qrDataUri = 'data:image/png;base64,test') {
  const inner = {
    _state: state,
    _qrDataUri: qrDataUri,
    _lastSentAt: null,
    _lastError: null,
  };

  return {
    get _lastSendPhone() { return inner._lastSendPhone; },
    get _lastSendMessage() { return inner._lastSendMessage; },
    get _logoutCalled() { return inner._logoutCalled; },

    status() {
      return {
        state: inner._state,
        qrAvailable: Boolean(inner._qrDataUri),
        lastSentAt: inner._lastSentAt,
        error: inner._lastError,
      };
    },

    pair() {
      return {
        state: inner._state,
        qrAvailable: Boolean(inner._qrDataUri),
        qrDataUri: inner._qrDataUri,
      };
    },

    async sendMessage(phone, message) {
      inner._lastSendPhone = phone;
      inner._lastSendMessage = message;
      inner._lastSentAt = new Date().toISOString();
    },

    async logout() {
      inner._logoutCalled = true;
    },
  };
}

function fakeCircuitBreaker(open = false) {
  let _open = open;
  return {
    isOpen() { return _open; },
    recordSuccess() { _open = false; },
    recordFailure() { _open = true; },
  };
}

function setup(overrides = {}) {
  const {
    state = 'qr_required',
    qrDataUri = 'data:image/png;base64,test',
    circuitOpen = false,
    enablePairingUi = false,
  } = overrides;

  const config = fakeConfig({ enablePairingUi });
  const client = fakeClient(state, qrDataUri);
  const cb = fakeCircuitBreaker(circuitOpen);
  const app = createApp(config, client, cb);
  return { app, config, client, cb };
}

describe('routes', () => {
  describe('GET /health', () => {
    it('returns 200 with service info', async () => {
      const { app } = setup({ state: 'connected' });
      const res = await request(app).get('/health');

      assert.equal(res.status, 200);
      assert.equal(res.body.ok, true);
      assert.equal(res.body.service, 'reusable-whatsapp-service');
      assert.equal(res.body.whatsappState, 'connected');
    });
  });

  describe('GET /status', () => {
    it('returns 401 without auth', async () => {
      const { app } = setup();
      const res = await request(app).get('/status');

      assert.equal(res.status, 401);
      assert.deepEqual(res.body, { error: 'unauthorized' });
    });

    it('returns 200 with valid x-internal-api-key', async () => {
      const { app, client } = setup();
      const res = await request(app)
        .get('/status')
        .set('x-internal-api-key', API_KEY);

      assert.equal(res.status, 200);
      assert.deepEqual(res.body, client.status());
    });

    it('returns 401 for an empty URL token when PAIRING_ADMIN_TOKEN is unset', async () => {
      const config = fakeConfig({ pairingAdminToken: '' });
      const app = createApp(config, fakeClient(), fakeCircuitBreaker());
      const res = await request(app).get('/status?token=');

      assert.equal(res.status, 401);
      assert.deepEqual(res.body, { error: 'unauthorized' });
    });
  });

  describe('GET /pair', () => {
    it('returns JSON with valid auth and Accept: application/json', async () => {
      const { app, client } = setup();
      const res = await request(app)
        .get('/pair')
        .set('x-internal-api-key', API_KEY)
        .set('Accept', 'application/json');

      assert.equal(res.status, 200);
      assert.deepEqual(res.body, client.pair());
    });

    it('returns 403 with Accept: text/html and ENABLE_PAIRING_UI=false', async () => {
      const { app } = setup({ enablePairingUi: false });
      const res = await request(app)
        .get('/pair')
        .set('x-internal-api-key', API_KEY)
        .set('Accept', 'text/html');

      assert.equal(res.status, 403);
      assert.deepEqual(res.body, { error: 'pairing_ui_disabled' });
    });

    it('returns HTML with Accept: text/html and ENABLE_PAIRING_UI=true', async () => {
      const { app } = setup({ enablePairingUi: true, qrDataUri: 'data:image/png;base64,qr' });
      const res = await request(app)
        .get('/pair')
        .set('x-internal-api-key', API_KEY)
        .set('Accept', 'text/html');

      assert.equal(res.status, 200);
      assert.ok(res.headers['content-type'].startsWith('text/html'));
      assert.ok(res.text.includes('data:image/png;base64,qr'));
    });

    it('returns 401 when INTERNAL_API_KEY is passed as URL token', async () => {
      const { app } = setup();
      const res = await request(app)
        .get(`/pair?token=${API_KEY}`)
        .set('Accept', 'application/json');

      assert.equal(res.status, 401);
      assert.deepEqual(res.body, { error: 'unauthorized' });
    });

    it('returns 200 when PAIRING_ADMIN_TOKEN is passed as URL token', async () => {
      const { app, client } = setup();
      const res = await request(app)
        .get(`/pair?token=${ADMIN_TOKEN}`)
        .set('Accept', 'application/json');

      assert.equal(res.status, 200);
      assert.deepEqual(res.body, client.pair());
    });

    it('returns 401 (not 500) when token is passed as an array via query', async () => {
      const { app } = setup();
      const res = await request(app)
        .get('/pair?token[0]=a&token[1]=b')
        .set('Accept', 'application/json');

      assert.equal(res.status, 401);
      assert.deepEqual(res.body, { error: 'unauthorized' });
    });

    it('returns 409 with already_connected when already connected', async () => {
      const { app } = setup({ state: 'connected' });
      const res = await request(app)
        .get('/pair')
        .set('x-internal-api-key', API_KEY)
        .set('Accept', 'application/json');

      assert.equal(res.status, 409);
      assert.deepEqual(res.body, { error: 'already_connected' });
    });

    it('returns 401 for an empty URL token when PAIRING_ADMIN_TOKEN is unset (no QR exposed)', async () => {
      const config = fakeConfig({ pairingAdminToken: '' });
      const app = createApp(config, fakeClient(), fakeCircuitBreaker());
      const res = await request(app)
        .get('/pair?token=')
        .set('Accept', 'application/json');

      assert.equal(res.status, 401);
      assert.deepEqual(res.body, { error: 'unauthorized' });
      assert.equal(res.body.qrDataUri, undefined);
    });

    it('still accepts INTERNAL_API_KEY header when PAIRING_ADMIN_TOKEN is unset', async () => {
      const config = fakeConfig({ pairingAdminToken: '' });
      const client = fakeClient();
      const app = createApp(config, client, fakeCircuitBreaker());
      const res = await request(app)
        .get('/pair')
        .set('x-internal-api-key', API_KEY)
        .set('Accept', 'application/json');

      assert.equal(res.status, 200);
      assert.deepEqual(res.body, client.pair());
    });
  });

  describe('POST /send-message', () => {
    it('returns 401 without auth', async () => {
      const { app } = setup();
      const res = await request(app)
        .post('/send-message')
        .send({ phone: '201234567890', message: 'hello' });

      assert.equal(res.status, 401);
      assert.deepEqual(res.body, { error: 'unauthorized' });
    });

    it('returns 200 with valid auth and valid body', async () => {
      const { app, client } = setup();
      const res = await request(app)
        .post('/send-message')
        .set('x-internal-api-key', API_KEY)
        .send({ phone: '201234567890', message: 'hello' });

      assert.equal(res.status, 200);
      assert.deepEqual(res.body, { success: true });
      assert.equal(client._lastSendPhone, '201234567890');
      assert.equal(client._lastSendMessage, 'hello');
    });

    it('returns 400 with missing phone/message', async () => {
      const { app } = setup();
      const res = await request(app)
        .post('/send-message')
        .set('x-internal-api-key', API_KEY)
        .send({});

      assert.equal(res.status, 400);
      assert.deepEqual(res.body, {
        success: false,
        error: 'phone_and_message_required',
        retryable: false,
      });
    });

    it('returns 503 when circuit breaker is open', async () => {
      const { app } = setup({ circuitOpen: true });
      const res = await request(app)
        .post('/send-message')
        .set('x-internal-api-key', API_KEY)
        .send({ phone: '201234567890', message: 'hello' });

      assert.equal(res.status, 503);
      assert.deepEqual(res.body, {
        success: false,
        error: 'circuit_open',
        retryable: true,
      });
    });

    it('returns send_timeout with retryable false', async () => {
      const config = fakeConfig();
      const client = fakeClient();
      client.sendMessage = async () => {
        const err = new Error('send_timeout');
        err.statusCode = 503;
        err.retryable = false;
        throw err;
      };
      const cb = fakeCircuitBreaker();
      const app = createApp(config, client, cb);

      const res = await request(app)
        .post('/send-message')
        .set('x-internal-api-key', API_KEY)
        .send({ phone: '201234567890', message: 'hello' });

      assert.equal(res.status, 503);
      assert.deepEqual(res.body, {
        success: false,
        error: 'send_timeout',
        retryable: false,
      });
    });

    it('returns 400 invalid_json for malformed JSON body with valid auth', async () => {
      const { app } = setup();
      const res = await request(app)
        .post('/send-message')
        .set('x-internal-api-key', API_KEY)
        .set('Content-Type', 'application/json')
        .send('{"phone":"201234567890","message":');

      assert.equal(res.status, 400);
      assert.deepEqual(res.body, { error: 'invalid_json' });
    });

    it('returns 413 payload_too_large for oversized JSON body', async () => {
      const { app } = setup();
      const res = await request(app)
        .post('/send-message')
        .set('x-internal-api-key', API_KEY)
        .send({ phone: '201234567890', message: 'x'.repeat(70_000) });

      assert.equal(res.status, 413);
      assert.deepEqual(res.body, { error: 'payload_too_large' });
    });

    it('returns 500 with generic error for unexpected failures', async () => {
      const config = fakeConfig();
      const client = fakeClient();
      client.sendMessage = async () => {
        throw new Error('unexpected internal database failure XYZ');
      };
      const cb = fakeCircuitBreaker();
      const app = createApp(config, client, cb);

      const res = await request(app)
        .post('/send-message')
        .set('x-internal-api-key', API_KEY)
        .send({ phone: '201234567890', message: 'hello' });

      assert.equal(res.status, 500);
      assert.equal(res.body.success, false);
      assert.equal(res.body.error, 'send_failed');
      assert.ok(!JSON.stringify(res.body).includes('database'));
    });
  });

  describe('POST /api/logout', () => {
    it('returns 204 with valid auth and calls client.logout', async () => {
      const { app, client } = setup();
      const res = await request(app)
        .post('/api/logout')
        .set('x-internal-api-key', API_KEY);

      assert.equal(res.status, 204);
      assert.equal(client._logoutCalled, true);
    });
  });

  describe('unknown route', () => {
    it('returns 404', async () => {
      const { app } = setup();
      const res = await request(app).get('/nonexistent');

      assert.equal(res.status, 404);
      assert.deepEqual(res.body, { error: 'not_found' });
    });
  });
});
