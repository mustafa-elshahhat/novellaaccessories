# WhatsApp Service Plan — Novella Accessories

## 1. Decision

The WhatsApp service is a **production-ready, standalone sidecar** copied/adapted from this real repository:

```text
https://github.com/mustafa-elshahhat/whatsapp-service-template
```

Copy/adapt it into the monorepo as:

```text
apps/whatsapp
```

Deployment target:

```text
Render (deployed separately from apps/api)
```

This is **not** an empty provider-placeholder skeleton. It is a working Node.js service that automates WhatsApp Web through Baileys. Swapping it for another transport (for example the official WhatsApp Business Cloud API) is a future option that would require real adaptation, not a drop-in provider change.

## 2. Technology Stack

`apps/whatsapp` is built with:

```text
Node.js
Express (HTTP REST)
Baileys / WhatsApp Web (message transport)
MongoDB (Baileys auth/session storage only)
Pino (structured logging)
Rate limiting (per-phone and global)
Circuit breaker (protects against repeated send failures)
Pairing / QR flow (link the WhatsApp account)
Health and status endpoints
```

## 3. Core Architecture

```text
apps/api -> HTTP REST -> apps/whatsapp -> Baileys -> WhatsApp Web
```

Rules:

- `apps/whatsapp` runs as a **separate sidecar service**, deployed on its own Render service.
- It is **adapted from `whatsapp-service-template`**, not written from scratch.
- It uses its **own external MongoDB database** only for Baileys authentication/session storage.
- It does **not** use the main SQL Server database for any WhatsApp data.
- It is **never** called directly by the storefront or the admin frontend.
- **Only `apps/api`** calls `apps/whatsapp`.
- `apps/api` owns all business logic and stores business message logs in SQL Server.
- `apps/whatsapp` stores only WhatsApp Web session/auth state in MongoDB.
- OTP generation **and** OTP verification belong to `apps/api`. `apps/whatsapp` only sends messages.
- Order logic, customer logic, coupon logic, reminder logic, and template-decision logic all live in `apps/api`.
- The WhatsApp service must not know Novella business rules. Prefer rendering the final message text in `apps/api` and then calling `/send-message`. (`/send-template` may be kept for compatibility but is treated as deprecated.)

## 4. Purpose

The WhatsApp service is the single outbound channel that delivers all WhatsApp messages for the store, while keeping `apps/api` as the only owner of business logic and message decisions.

## 5. Required Use Cases

`apps/api` decides when to send and what text to send. `apps/whatsapp` only delivers. Use cases:

- Account registration OTP.
- Password reset OTP.
- Phone-number change OTP.
- Order confirmation message.
- Automatic coupon after two Delivered orders.
- Abandoned checkout reminder.
- Inactive customer reminder.
- Failed message retry (retry orchestration lives in `apps/api`).
- Test message from admin.

## 6. Endpoint Contract

`apps/whatsapp` exposes:

```text
GET  /health
GET  /status
POST /send-message
POST /send-template
GET  /pair
GET  /qr
POST /api/logout
```

Endpoint rules:

- `GET /health` — **public**, used for liveness/readiness (Render health check). Returns service status, version if available, and whether the WhatsApp connection is up. No secrets.
- `GET /status` — **protected** by the internal API key or a pairing token. Reports connection/session state (connected, needs pairing, etc.).
- `POST /send-message` — **the primary sending endpoint.** Accepts the final message text already rendered by `apps/api`.
- `POST /send-template` — exists in the template but is **deprecated / optional compatibility only.** Prefer `/send-message`.
- `GET /pair` and `GET /qr` — WhatsApp pairing endpoints. The browser QR pairing UI must remain **disabled by default in production** (`ENABLE_PAIRING_UI=false`).
- `POST /api/logout` — logs out the WhatsApp account and clears auth/session state.

> The old `/send` endpoint is **removed**. Use `/send-message` everywhere.

Response shape from send endpoints should include:

- Success/failure.
- Provider/transport message ID if available.
- Error reason.
- Retryable flag if possible.

## 7. Authentication & Secrets

- Protected endpoints require **`x-internal-api-key`** or **`Authorization: Bearer <key>`**.
- Pairing endpoints (`/pair`, `/qr`) may additionally use **`PAIRING_ADMIN_TOKEN`**.
- `INTERNAL_API_KEY` must **never** be exposed to any frontend app (no `NEXT_PUBLIC_*`, no `VITE_*`).
- `PAIRING_ADMIN_TOKEN` must **never** be exposed publicly.
- The admin dashboard must **not** display raw secrets — only configured/not-configured status.
- If the admin needs status/pairing visibility, it must go through `apps/api` (which calls `/status` with the internal API key) or through a documented protected operational flow. Pairing itself is handled as an operational task directly on the WhatsApp service.
- Do not log OTP values in plain text. Mask phone numbers in logs where possible.
- Store all secrets only in environment variables; never commit real values.

## 8. Message Types

`apps/api` renders the final text for each message type from `WhatsAppSettings` templates before calling `/send-message`.

### OTP

Purpose: register, reset password, change phone.

Variables:

- Customer name (optional).
- OTP code (generated and verified in `apps/api`).
- Expiry minutes.

### Order Confirmation

Variables:

- Customer name.
- Order number.
- Grand total.
- Payment method.
- Order link if available.

### Two-Delivered-Orders Coupon

Variables:

- Customer name.
- Coupon code.
- Discount percentage.
- Expiry date.

### Abandoned Checkout Reminder

Variables:

- Customer name.
- Cart/checkout link.
- Optional product summary.

### Inactive Customer Reminder

Variables:

- Customer name.
- Store link.
- Optional offer/campaign text.

## 9. Message Logs (in `apps/api` / SQL Server)

`apps/api` stores all business message logs in SQL Server (`WhatsAppMessageLogs`). `apps/whatsapp` does **not** store business logs — it keeps only Baileys session/auth data in MongoDB.

Fields:

- Customer.
- Phone number.
- Message type.
- Template key.
- Message body.
- Status.
- Failure reason.
- Retry count.
- Sent date.
- Created date.

## 10. MongoDB (Baileys Session Storage)

- `apps/whatsapp` requires its **own external MongoDB** (for example MongoDB Atlas) configured via `MONGODB_URI`.
- MongoDB stores **only** Baileys authentication/session state so the WhatsApp account stays linked across restarts/redeploys.
- MongoDB is **separate** from the main SQL Server database and must never be used for business data.
- No customer, order, coupon, or message-log data is stored in MongoDB.

## 11. Admin Page

Admin (through `apps/api`) can:

- Enable/disable WhatsApp sending.
- Edit message templates.
- See sent messages.
- See failed messages.
- Retry failed messages.
- Send a test message.
- Filter by message type/status/date.
- View connection/session status (proxied from `apps/whatsapp` `/status` via `apps/api`).

The admin never calls `apps/whatsapp` directly and never sees raw secrets.

## 12. Retry Rules

- Failed messages can be retried manually by admin (orchestrated by `apps/api`).
- Retry count must be incremented in SQL Server.
- Failure reason must be preserved or updated.
- Do not retry endlessly (bounded retries).
- `apps/whatsapp` also has a built-in **circuit breaker** that pauses sending after repeated transport failures and cools down before resuming.

## 13. OTP Rules

OTP **generation and verification both live in `apps/api`.** `apps/whatsapp` only delivers the rendered OTP text.

OTP sending must respect (enforced in `apps/api`):

- Expiry time.
- Resend cooldown.
- Attempt limits.
- Temporary lockout.
- Purpose (register, reset_password, change_phone).

OTP verification belongs to `apps/api`, **never** to the WhatsApp service.

## 14. Reminder Rules

Reminders apply only to registered customers. Reminder scheduling and dedupe live in `apps/api`.

Abandoned checkout:

- Sent once per abandoned checkout/cart event after admin-defined delay.

Inactive customer:

- Sent once per absence cycle after admin-defined number of days.

## 15. Sending Safeguards (in `apps/whatsapp`)

Because Baileys automates WhatsApp Web, the service throttles and protects sending to reduce account risk:

- Randomized delay between sends (`SEND_DELAY_MIN_MS` / `SEND_DELAY_MAX_MS`).
- Per-phone daily limit (`DAILY_PHONE_LIMIT`).
- Global per-minute send limit (`GLOBAL_SEND_LIMIT_PER_MINUTE`).
- Send timeout (`SEND_TIMEOUT_MS`).
- Circuit breaker (`CIRCUIT_BREAKER_THRESHOLD` / `CIRCUIT_BREAKER_COOLDOWN_MS`).

## 16. Environment Variables (`apps/whatsapp`)

```text
NODE_ENV=development
PORT=4000
MONGODB_URI=
INTERNAL_API_KEY=
PAIRING_ADMIN_TOKEN=
ENABLE_PAIRING_UI=false
SEND_DELAY_MIN_MS=5000
SEND_DELAY_MAX_MS=15000
DAILY_PHONE_LIMIT=10
GLOBAL_SEND_LIMIT_PER_MINUTE=60
SEND_TIMEOUT_MS=30000
CIRCUIT_BREAKER_THRESHOLD=3
CIRCUIT_BREAKER_COOLDOWN_MS=30000
LOG_LEVEL=info
```

Notes:

- The upstream template default port may be `3005`, but Novella local development uses `PORT=4000` to match the monorepo local port plan.
- `MONGODB_URI` is **required** for Baileys auth/session storage and must point to a MongoDB separate from the main SQL Server database.
- Real values must never be committed.

## 17. API-Side Configuration (`apps/api`)

`apps/api` calls the sidecar using:

```text
WhatsApp__BaseUrl=
WhatsApp__InternalApiKey=
```

`WhatsApp__InternalApiKey` is sent as `x-internal-api-key` (or `Authorization: Bearer`) on every protected call. Pairing/status is operational; a pairing token is only added to `apps/api` if the API is explicitly given the job of proxying pairing operations — by default pairing is handled directly on the WhatsApp service.

## 18. Future Replacement (Optional)

If the official **WhatsApp Business Cloud API** is adopted later, it would replace the Baileys transport inside `apps/whatsapp` (or a new sidecar). At that point the official provider variables (for example `WHATSAPP_API_KEY`, `WHATSAPP_PHONE_NUMBER_ID`, `WHATSAPP_ACCESS_TOKEN`) would apply. These are **future** variables and are **not** part of the current MVP contract, which is Baileys-based. `apps/api` keeps a thin `IWhatsAppClient` abstraction over the HTTP call so this swap is contained to `apps/api` + the sidecar.

## 19. Health & Status

- `GET /health` — public liveness/readiness for Render. Returns status, connection up/down, version. No secrets.
- `GET /status` — protected; returns detailed session/connection state (connected, needs pairing, last error). Surfaced to admin only through `apps/api`.

Do not expose secrets through either endpoint.
