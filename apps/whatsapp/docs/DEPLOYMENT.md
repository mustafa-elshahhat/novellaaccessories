# Deployment Guide

## Prerequisites

- Node.js >= 20
- A MongoDB instance (Atlas, self-hosted, or any MongoDB-compatible service)
- The `.env` file configured with at minimum `MONGODB_URI` and `INTERNAL_API_KEY`

---

## Local Deployment

```bash
# Clone or copy the template
cd whatsapp-service-template

# Copy and configure environment
cp .env.example .env
# Edit .env — set MONGODB_URI, INTERNAL_API_KEY

# Install dependencies
npm install

# Start the service
npm start
```

The service starts on `http://localhost:4000`.

---

## Render Deployment

The project includes `render.yaml` for easy deployment to Render.

1. Push the repository to GitHub/GitLab.
2. In Render dashboard, select **Blueprint** and connect your repository.
3. Render automatically detects `render.yaml` and provisions the service.
4. Set the required environment variables in Render dashboard:
   - `MONGODB_URI` (must be set manually)
   - `INTERNAL_API_KEY` (must be set manually)
5. Deploy.

---

## Required Environment Variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `MONGODB_URI` | Yes | — | MongoDB connection string for WhatsApp session storage |
| `INTERNAL_API_KEY` | Yes | — | API key for backend authentication (min 32 chars) |

---

## Optional Environment Variables

| Variable | Default | Description |
|---|---|---|
| `PORT` | `4000` | HTTP port |
| `PAIRING_ADMIN_TOKEN` | — | Separate token for pairing endpoints (min 32 chars) |
| `ENABLE_PAIRING_UI` | `false` | Enable browser QR pairing page |
| `SEND_DELAY_MIN_MS` | `5000` | Minimum cooldown between sends to the same phone |
| `SEND_DELAY_MAX_MS` | `15000` | Maximum cooldown between sends to the same phone |
| `DAILY_PHONE_LIMIT` | `10` | Max messages per phone per day |
| `GLOBAL_SEND_LIMIT_PER_MINUTE` | `60` | Max messages globally per minute |
| `SEND_TIMEOUT_MS` | `30000` | Timeout for each sendMessage call |
| `CIRCUIT_BREAKER_THRESHOLD` | `3` | Consecutive send failures before the circuit breaker opens |
| `CIRCUIT_BREAKER_COOLDOWN_MS` | `30000` | How long the circuit breaker stays open before retrying |
| `LOG_LEVEL` | `info` | Pino log level (`fatal`, `error`, `warn`, `info`, `debug`, `trace`, `silent`) |
| `NODE_ENV` | `development` | Runtime environment name, included in startup logs |

---

## Pairing Flow

1. Start the service.
2. Authenticate using `INTERNAL_API_KEY` or `PAIRING_ADMIN_TOKEN`.
3. Call `GET /pair` or `GET /qr` to retrieve QR pairing data.
4. Open WhatsApp on your phone → Link a Device → scan the QR code.
5. Verify connection: `GET /health` returns `"whatsappState": "connected"`.

For browser-based pairing, set `ENABLE_PAIRING_UI=true` and navigate to:

```
http://localhost:4000/pair?token=<PAIRING_ADMIN_TOKEN>
```

---

## Production Security Notes

- **Keep `ENABLE_PAIRING_UI=false` in production.** Enable it only temporarily during initial setup.
- **Use strong random secrets** for `INTERNAL_API_KEY` and `PAIRING_ADMIN_TOKEN` (minimum 32 characters).
- **Restrict network access** to the WhatsApp service. Keep it in a private network or VPC when possible.
- **Do not expose the service directly to the internet** without additional authentication layers.
- **Monitor health** with `GET /health` and `GET /status` to detect connection issues.
- **Log carefully** — phone numbers are masked automatically; never log API keys or full message content.
- **WhatsApp may rate-limit or ban accounts** — respect the built-in rate limiter configuration.

---

## Single-Instance Limitations

This service is designed as a single-instance-per-account deployment:

- **In-memory state resets on restart.** Rate limiter counters (per-phone cooldowns,
  daily limits, global rate) and circuit breaker state are held in memory and do not
  survive process restarts.
- **No cross-instance coordination.** Running multiple instances against the same
  WhatsApp account is not supported — each instance tracks its own rate limits
  independently, and Baileys does not support concurrent socket connections.
- **Free-tier hosting caveats.** Platforms like Render's free plan spin down idle
  services, which disconnects the WhatsApp socket and clears all in-memory state.
  Use a persistent hosting plan for production workloads.

---

## DNS Override (Opt-In)

The file `src/dns-fix.js` overrides Node.js DNS to use Google (`8.8.8.8`) and
Cloudflare (`1.1.1.1`) public DNS. It is **not enabled by default**.

Use it only if you experience DNS resolution failures (e.g. `ENOTFOUND` errors
connecting to MongoDB Atlas or WhatsApp servers on certain hosting platforms).

**Do not enable** on private networks, VPCs, or environments with internal DNS —
it will bypass custom DNS resolution for private hostnames.

To opt in:

```bash
node --import ./src/dns-fix.js src/server.js
```
