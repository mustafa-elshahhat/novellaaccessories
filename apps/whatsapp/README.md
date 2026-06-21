# Reusable WhatsApp Sidecar Service

A production-ready, standalone WhatsApp messaging service template built with
[Baileys](https://github.com/whiskeysockets/baileys) (WhatsApp Web API).

This service runs as an independent HTTP sidecar that any application — regardless of
backend or frontend technology — can integrate with over REST. It is designed to be
deployed separately from your main application, called exclusively by trusted backends,
and never exposed directly to frontends or end users.

---

## Novella Accessories Integration

This copy lives at `apps/whatsapp` in the **Novella Accessories** monorepo. It is
**adapted from** [`whatsapp-service-template`](https://github.com/mustafa-elshahhat/whatsapp-service-template)
and remains a real Express + Baileys WhatsApp Web sidecar — not a placeholder.

- **Deployment target:** Render (deployed separately from `apps/api`).
- **Local port:** runs on `http://localhost:4000` (`PORT=4000`).
- **Caller:** it is called **only** by `apps/api`. The storefront (`apps/storefront`)
  and admin dashboard (`apps/admin`) must **never** call this service directly.
- **No business logic.** This service only sends WhatsApp messages and manages the
  WhatsApp Web connection/session. It does not know about orders, coupons, customers,
  or reminder rules.
- **No OTP logic.** OTP **generation and verification both belong to `apps/api`.**
  `apps/api` generates OTPs, verifies OTPs, renders the final message text, decides
  when to send, and stores all message logs in SQL Server. This service only delivers
  the already-rendered text passed to `POST /send-message`.
- **MongoDB is for Baileys only.** `MONGODB_URI` stores Baileys auth/session state so
  the WhatsApp account stays linked across restarts. It is **separate** from the main
  SQL Server database and must never hold business data.
- **Account risk.** Baileys is unofficial WhatsApp Web automation; account/ban risk is
  mitigated (not eliminated) by randomized send delays, per-phone and global rate
  limits, and the circuit breaker.
- **Logging discipline.** Never log OTP values in plain text; phone numbers are masked
  in service logs.

### Endpoint contract (Novella)

```text
GET  /health        # public liveness/readiness — no secrets
GET  /status        # protected — connection/session state
POST /send-message  # primary send endpoint (final text from apps/api)
POST /send-template # deprecated/optional compatibility only
GET  /pair          # pairing
GET  /qr            # pairing
POST /api/logout    # logout WhatsApp + clear auth/session state
```

There is **no `/send` endpoint** — use `/send-message`. Protected endpoints require
`x-internal-api-key` or `Authorization: Bearer`; pairing endpoints may also use
`PAIRING_ADMIN_TOKEN`. `ENABLE_PAIRING_UI=false` by default.

`INTERNAL_API_KEY`, `PAIRING_ADMIN_TOKEN`, and `MONGODB_URI` are server-side secrets and
must **never** be exposed to any frontend (no `NEXT_PUBLIC_*`, no `VITE_*`).

---

## What This Is

- A **standalone WhatsApp sidecar** that any backend can call over HTTP.
- An **independent runtime** that manages its own WhatsApp session in an external MongoDB.
- A **reusable service template** that can be copied into any project and customized.
- A **backend-only integration point** — frontends must never call this service directly.

## What This Is NOT

- Not tied to any specific project, company, or vendor.
- Not a frontend package or component.
- Not a backend library to import directly into your application.
- Not responsible for business logic, orders, payments, or user workflows.
- Not a shared database dependency — it uses its own dedicated MongoDB.
- Not a guarantee of WhatsApp account reliability — see
  [WhatsApp Account Risk](#whatsapp-account-risk).

---

## Architecture

```
┌──────────────────────────┐
│  Your App / Any Backend  │
└────────────┬─────────────┘
             │  HTTP REST
             │  x-internal-api-key auth
             ▼
┌──────────────────────────┐
│  WhatsApp Sidecar        │  ← this service
│  (Express + Baileys)     │
└────────────┬─────────────┘
             │  Baileys Auth Session
             ▼
┌──────────────────────────┐
│  External MongoDB        │  ← dedicated to WhatsApp sessions only
│  (separate from app DB)  │
└──────────────────────────┘
```

The integration model is always:

**Your Backend → HTTP → WhatsApp Service**

- The backend never imports this service's code directly.
- The frontend never calls this service directly.
- WhatsApp session data stays in the service's own MongoDB.

---

## Features

- **REST API** — Simple HTTP endpoints for sending messages, checking status, and pairing.
- **MongoDB Session Storage** — WhatsApp auth state persisted in a dedicated MongoDB collection.
- **Rate Limiting** — Per-phone cooldown, daily per-phone limit, and global per-minute limit.
- **Circuit Breaker** — Stops sending after repeated failures (configurable threshold, default 3); auto-recovers after cooldown.
- **Pairing UI Control** — Browser-based QR pairing page disabled by default (`ENABLE_PAIRING_UI=false`).
- **Constant-Time Auth** — API key comparison uses constant-time equality to prevent timing attacks.
- **Graceful Shutdown** — Clean socket and MongoDB connection teardown on SIGTERM/SIGINT.
- **Structured Logging** — JSON logging via Pino with automatic phone number masking.
- **Send Timeout** — Configurable timeout per `sendMessage` call to prevent hanging requests.
- **Auto-Reconnect** — Automatic reconnection on transient WhatsApp disconnections.
- **Template Messages** — Deprecated convenience route for server-side template rendering.

---

## Quick Start

```bash
cd whatsapp-service-template
cp .env.example .env
# Edit .env — set MONGODB_URI and INTERNAL_API_KEY
npm install
npm start
```

The service starts on `http://localhost:4000`.

See [docs/QUICK_START.md](./docs/QUICK_START.md) for the full step-by-step guide.

---

## Environment Variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `MONGODB_URI` | Yes | — | MongoDB connection string for WhatsApp session storage |
| `INTERNAL_API_KEY` | Yes | — | API key for backend authentication (min 32 chars) |
| `PORT` | No | `4000` | HTTP port |
| `PAIRING_ADMIN_TOKEN` | No | — | Separate token for pairing endpoints (min 32 chars) |
| `ENABLE_PAIRING_UI` | No | `false` | Enable browser QR pairing page |
| `SEND_DELAY_MIN_MS` | No | `5000` | Min delay between sends to same phone (ms) |
| `SEND_DELAY_MAX_MS` | No | `15000` | Max delay between sends to same phone (ms) |
| `DAILY_PHONE_LIMIT` | No | `10` | Max messages per phone per day |
| `GLOBAL_SEND_LIMIT_PER_MINUTE` | No | `60` | Max messages globally per minute |
| `SEND_TIMEOUT_MS` | No | `30000` | Timeout for each sendMessage call (ms) |
| `CIRCUIT_BREAKER_THRESHOLD` | No | `3` | Consecutive send failures before the circuit breaker opens |
| `CIRCUIT_BREAKER_COOLDOWN_MS` | No | `30000` | How long the circuit breaker stays open before retrying (ms) |
| `LOG_LEVEL` | No | `info` | Pino log level (`fatal`, `error`, `warn`, `info`, `debug`, `trace`, `silent`) |
| `NODE_ENV` | No | `development` | Runtime environment name, included in startup logs |

See [.env.example](./.env.example) for a copy-ready template.

---

## API Summary

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/health` | None | Liveness check (always 200; see `whatsappState` for readiness) |
| `GET` | `/status` | API key or pairing token | Detailed WhatsApp connection status |
| `POST` | `/send-message` | API key | Send a plain text WhatsApp message |
| `POST` | `/send-template` | API key | *(Deprecated)* Render and send a template |
| `GET` | `/pair` or `/qr` | API key or pairing token | Retrieve QR pairing data |
| `POST` | `/api/logout` | API key | Logout WhatsApp and clear auth state |

See [docs/CONTRACT.md](./docs/CONTRACT.md) for the full HTTP API specification, including
request/response shapes, error codes, and cURL examples.

---

## Security Model

- **Backend-only authentication.** All protected endpoints require `x-internal-api-key`
  or `Authorization: Bearer` header. Frontends must never have access to these keys.
- **Constant-time key comparison.** API key validation uses constant-time string
  comparison to prevent timing side-channel attacks.
- **Pairing UI disabled by default.** `ENABLE_PAIRING_UI=false` in production. The
  browser-based QR page returns HTTP 403 unless explicitly enabled.
- **Separate pairing token.** An optional `PAIRING_ADMIN_TOKEN` isolates pairing access
  from message-sending access.
- **Phone number masking.** Service-emitted logs mask phone numbers; Baileys
  internal logs are capped at warn level to reduce recipient/payload logging risk.
- **No secret logging.** API keys and full message bodies are never logged.
- **Minimum secret length.** `INTERNAL_API_KEY` and `PAIRING_ADMIN_TOKEN` must each be
  at least 32 characters when set.
- **URL token restriction.** Browser-based pairing (`?token=`) only accepts
  `PAIRING_ADMIN_TOKEN`. `INTERNAL_API_KEY` is never accepted through URL query
  parameters to prevent leakage via browser history, server access logs, and referrer
  headers.

---

## Production Notes

- Use **strong random secrets** for `INTERNAL_API_KEY` and `PAIRING_ADMIN_TOKEN`
  (minimum 32 characters each).
- **Restrict network access** to the WhatsApp service when possible (VPC, private
  network, internal DNS).
- **Do not expose pairing endpoints** publicly without protection.
- **Keep `ENABLE_PAIRING_UI=false` in production** — enable the browser QR page only
  temporarily during initial setup, and only when admin access is well-protected.
- **Log carefully** — phone numbers are masked automatically; never log API keys or
  full message content.
- **Add monitoring** for `GET /health` and `GET /status` to detect connection issues.
- **Treat WhatsApp as non-critical async** — business flows should not block on
  WhatsApp delivery.
- WhatsApp may **rate-limit or ban** accounts — respect the built-in rate limiter
  configuration and review [WhatsApp Account Risk](#whatsapp-account-risk).

---

## Single-Instance Limitations

This service is designed to run as a single instance per WhatsApp account:

- **In-memory rate limits and circuit breaker state reset on restart.** Per-phone
  cooldowns, daily counters, and circuit breaker failure counts are not persisted.
- **Per-phone daily limits are not shared across instances.** If you run multiple
  instances (not recommended), each tracks its own counters independently.
- **Free-tier hosting spin-down.** Platforms like Render's free plan spin down idle
  services. This causes the WhatsApp socket to disconnect and rate limiter state to
  reset. Use a persistent hosting plan for production WhatsApp workloads.

---

## DNS Override (Opt-In)

The file `src/dns-fix.js` overrides Node.js DNS resolution to use Google (`8.8.8.8`)
and Cloudflare (`1.1.1.1`) public DNS servers. It is **not enabled by default**.

**When it may be useful:**
- Some hosting platforms (e.g. Render free tier) have unreliable default DNS.
- MongoDB Atlas or WhatsApp Web connections may fail with `ENOTFOUND` errors.

**When it may break things:**
- Private networks, VPCs, or corporate environments with internal DNS.
- Services that rely on custom DNS resolution for private hostnames.

To opt in, start the service with:

```bash
node --import ./src/dns-fix.js src/server.js
```

---

## WhatsApp Account Risk

This service uses the [Baileys](https://github.com/whiskeysockets/baileys) library,
which communicates via the WhatsApp Web protocol. This is **not an official WhatsApp
Business API**.

**Be aware of the following risks:**

- WhatsApp may **rate-limit, temporarily suspend, or permanently ban** accounts that
  send too many messages, send to unknown contacts, or violate WhatsApp's Terms of
  Service.
- The built-in rate limiter (per-phone cooldown, daily limits, global per-minute cap)
  is designed to reduce this risk, but **cannot eliminate it**.
- Baileys is a community-maintained reverse-engineered client. WhatsApp protocol
  changes may break compatibility without notice.
- This template pins Baileys to an exact version (`7.0.0-rc13`). At the time of
  writing this is the version published under npm's `latest` tag — the 7.x line has
  no non-RC release yet, and the only stable build (`6.7.x`, npm `legacy` tag) is an
  older major with a different API. The pin is intentional: review the
  [Baileys releases](https://github.com/whiskeysockets/baileys/releases) and test
  pairing/sending before bumping it.
- For high-volume or business-critical messaging, consider the official
  [WhatsApp Business API](https://developers.facebook.com/docs/whatsapp/) instead.

The service template provides rate-limiting defaults that are conservative, but
**you are responsible for configuring limits appropriate to your use case and for
complying with WhatsApp's Terms of Service**.

---

## Integration Checklist

- [ ] WhatsApp service deployed and accessible from backend
- [ ] External MongoDB configured for WhatsApp sessions
- [ ] `INTERNAL_API_KEY` set in WhatsApp service
- [ ] `WHATSAPP_SERVICE_URL` and `WHATSAPP_INTERNAL_API_KEY` set in backend config
- [ ] HTTP client wrapper created in backend (see `examples/`)
- [ ] WhatsApp paired and status shows `connected`
- [ ] Test message sent and delivered
- [ ] Frontend does not have access to API keys
- [ ] `ENABLE_PAIRING_UI=false` in production (browser pairing UI disabled)
- [ ] Monitoring set up for health/status
- [ ] Production secrets not committed to version control

---

## How to Integrate with Any Backend

1. Deploy this WhatsApp service (separately from your main app).
2. Set `WHATSAPP_SERVICE_URL` and `WHATSAPP_INTERNAL_API_KEY` in your backend's config.
3. Write a thin HTTP client wrapper in your backend (or use the examples in `examples/`).
4. Call `POST /send-message` from your backend when you need to send WhatsApp notifications.
5. Never expose `INTERNAL_API_KEY` to the frontend.
6. Frontend calls its own backend; the backend calls this WhatsApp service.

See [docs/CONTRACT.md](./docs/CONTRACT.md) for the full HTTP API specification.

---

## Deployment Model

| Component | Where |
|---|---|
| WhatsApp Service | Deploy separately (Render, Railway, Fly.io, AWS, etc.) |
| Main Backend | Deploy separately (any tech stack) |
| MongoDB | External (MongoDB Atlas, etc.) — dedicated to WhatsApp sessions only |
| Secrets | Set in hosting provider dashboard or secret manager |

- Keep the service URL private/internal when possible (VPC, private network).
- Use strong random secrets (min 32 characters).
- Restrict network access to the service where feasible.

See [docs/DEPLOYMENT.md](./docs/DEPLOYMENT.md) for deployment instructions.

---

## Pairing WhatsApp

1. Start the service.
2. Set `PAIRING_ADMIN_TOKEN` in `.env` and set `ENABLE_PAIRING_UI=true`.
3. Open in browser: `http://localhost:4000/pair?token=<PAIRING_ADMIN_TOKEN>`
   - If `ENABLE_PAIRING_UI=false` (default), the browser UI returns 403.
   - `INTERNAL_API_KEY` is **not accepted** as a URL token — use header-based
     auth or set `PAIRING_ADMIN_TOKEN`.
4. Scan the QR code with WhatsApp → Link a Device.
5. Verify: `curl http://localhost:4000/health` returns `"whatsappState": "connected"`.
6. Set `ENABLE_PAIRING_UI=false` in production after pairing is complete.

**Alternative — header-based pairing (no browser):**

```bash
curl -H "x-internal-api-key: your-key" http://localhost:4000/pair
```

---

## Testing

```bash
# Syntax check only
npm run check

# Run automated tests
npm test

# Syntax check + tests
npm run verify
```

---

## Project Structure

| Path | Purpose |
|---|---|
| `src/` | Service source code (Express + Baileys) |
| `src/config/` | Environment loading, constants, logger |
| `src/middleware/` | Authentication, error handling, 404 |
| `src/modules/` | Route modules (health, status, pairing, messages, session) |
| `src/services/` | WhatsApp client, rate limiter, circuit breaker, MongoDB auth state |
| `src/repositories/` | MongoDB auth repository |
| `src/templates/` | Deprecated message templates |
| `src/utils/` | Phone normalization and masking |
| `scripts/` | Syntax check script |
| `tests/` | Automated tests (routes, rate limiter, circuit breaker) |
| `docs/` | API contract, quick start, deployment guide, AI integration prompt |
| `examples/` | Integration examples (Node.js, Python, Laravel, .NET, Spring Boot) |
| `.env.example` | Environment variable reference |
| `render.yaml` | Render.com deployment config |
| `LICENSE` | MIT license |

---

## Documentation

| Document | Description |
|---|---|
| [CONTRACT.md](./docs/CONTRACT.md) | Full HTTP API specification with endpoints, auth, error codes, and cURL examples |
| [QUICK_START.md](./docs/QUICK_START.md) | Step-by-step setup and first message guide |
| [DEPLOYMENT.md](./docs/DEPLOYMENT.md) | Deployment guide with Render instructions and production security notes |
| [AI_INTEGRATION_PROMPT.md](./docs/AI_INTEGRATION_PROMPT.md) | Ready-to-copy prompt for AI coding agents to integrate this service |

---

## GitHub Repository Metadata

If publishing this repository on GitHub, set the following metadata in the repository
settings (Settings → General):

**Description:**

> Production-ready reusable WhatsApp sidecar service with REST API, MongoDB session
> storage, pairing UI control, rate limits, and backend-only authentication.

**Topics:**

`nodejs` · `express` · `whatsapp` · `baileys` · `mongodb` · `rest-api` ·
`sidecar-service` · `production-ready` · `messaging-service`
