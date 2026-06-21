# Quick Start Guide

## 1. Local Run

```bash
# Clone or copy the template into your project
cd whatsapp-service-template

# Copy environment file and edit it
cp .env.example .env
```

Edit `.env` and set at minimum:
- `MONGODB_URI` — your MongoDB connection string
- `INTERNAL_API_KEY` — a strong random key (min 32 chars)

Then:

```bash
npm install
npm start
```

The service starts on `http://localhost:4000`.

---

## 2. Run Tests

```bash
npm run check    # syntax check
npm test         # automated tests
npm run verify   # syntax check + tests
```

---

## 3. Required Environment Variables

| Variable | Required | Description |
|---|---|---|
| `MONGODB_URI` | Yes | MongoDB connection string for WhatsApp session storage |
| `INTERNAL_API_KEY` | Yes | API key for backend authentication (min 32 chars) |
| `PORT` | No | HTTP port (default: 4000) |
| `PAIRING_ADMIN_TOKEN` | No | Separate token for pairing endpoints (min 32 chars) |
| `ENABLE_PAIRING_UI` | No | Enable browser QR page (default: false) |
| `SEND_DELAY_MIN_MS` | No | Min delay between sends to same phone (default: 5000) |
| `SEND_DELAY_MAX_MS` | No | Max delay between sends to same phone (default: 15000) |
| `DAILY_PHONE_LIMIT` | No | Max messages per phone per day (default: 10) |
| `GLOBAL_SEND_LIMIT_PER_MINUTE` | No | Max messages globally per minute (default: 60) |
| `SEND_TIMEOUT_MS` | No | SendMessage timeout (default: 30000) |
| `CIRCUIT_BREAKER_THRESHOLD` | No | Consecutive send failures before the circuit breaker opens (default: 3) |
| `CIRCUIT_BREAKER_COOLDOWN_MS` | No | How long the circuit breaker stays open before retrying (default: 30000) |
| `LOG_LEVEL` | No | Pino log level (default: info) |
| `NODE_ENV` | No | Runtime environment name (default: development) |

---

## 4. Test Health

```bash
curl http://localhost:4000/health
```

Expected response:

```json
{
  "ok": true,
  "service": "reusable-whatsapp-service",
  "whatsappState": "initializing"
}
```

---

## 5. Test Status

```bash
curl -H "x-internal-api-key: your-key" http://localhost:4000/status
```

Expected response:

```json
{
  "state": "initializing",
  "qrAvailable": false,
  "lastSentAt": null,
  "error": null
}
```

---

## 6. Pair WhatsApp and Send Test Message

### Pair

**Option 1 — Browser (requires `PAIRING_ADMIN_TOKEN` and `ENABLE_PAIRING_UI=true`):**

```
http://localhost:4000/pair?token=<PAIRING_ADMIN_TOKEN>
```

> **Note:** `INTERNAL_API_KEY` is not accepted as a URL token. If `PAIRING_ADMIN_TOKEN`
> is not set, use header-based pairing instead.

**Option 2 — Header-based (cURL):**

```bash
curl -H "x-internal-api-key: your-key" http://localhost:4000/pair
```

Scan the QR code with WhatsApp → Link a Device.

### Verify connection

```bash
curl http://localhost:4000/health
# Look for "whatsappState": "connected"
```

### Send test message

```bash
curl -X POST http://localhost:4000/send-message \
  -H "x-internal-api-key: your-key" \
  -H "Content-Type: application/json" \
  -d '{"phone":"201234567890","message":"Hello from the reusable WhatsApp service!"}'
```

---

## 7. Using the AI Integration Prompt

The file `AI_INTEGRATION_PROMPT.md` contains a ready-to-copy prompt that you can give to any AI coding agent.

To use it:

1. Open `AI_INTEGRATION_PROMPT.md`
2. Select and copy the entire content
3. Paste it into a conversation with an AI coding agent
4. The agent will integrate the WhatsApp service into your target project

This prompt is technology-agnostic and works with any backend framework.

---

## 8. Copy This Template Into Another Repository

```bash
# From the target repo root
cp -r /path/to/whatsapp-service-template ./whatsapp-service
cd whatsapp-service
cp .env.example .env
# Edit .env
npm install
```

Then customize:
- Update `package.json` name
- Update service name in `src/config/logger.js`
- Update service name in `src/modules/health/health.controller.js`
- Customize phone normalization in `src/utils/phone.util.js` if needed
- Adjust `render.yaml` for your deployment platform
