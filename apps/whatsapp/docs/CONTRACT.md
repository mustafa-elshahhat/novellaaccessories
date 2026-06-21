# Reusable WhatsApp Service — HTTP Contract

## Base URL

```
http://localhost:4000
```

In production, replace with the deployed service URL (e.g. `https://whatsapp-service.yourcompany.com`).

---

## Authentication

Every protected endpoint requires one of the following headers:

**Option 1 — Internal API Key header:**

```
x-internal-api-key: <INTERNAL_API_KEY>
```

**Option 2 — Authorization Bearer:**

```
Authorization: Bearer <INTERNAL_API_KEY>
```

Pairing endpoints additionally accept:
- `x-pairing-admin-token` header
- `token` query parameter (for browser-based pairing; accepts `PAIRING_ADMIN_TOKEN` only)

> **Note:** `INTERNAL_API_KEY` is never accepted through URL query or path parameters.
> Use header-based authentication for programmatic pairing access.

---

## Endpoints

### GET /health

Basic liveness check. Always returns HTTP 200 when the process is up; inspect
`whatsappState` (or `GET /status`) for readiness. Does **not** require authentication.

**Example response:**

```json
{
  "ok": true,
  "service": "reusable-whatsapp-service",
  "whatsappState": "connected"
}
```

`whatsappState` reflects the current WhatsApp connection state (see status endpoint for possible values).

---

### GET /status

Returns detailed WhatsApp connection status.

**Authentication:** `INTERNAL_API_KEY` or `PAIRING_ADMIN_TOKEN`

**Example response:**

```json
{
  "state": "connected",
  "qrAvailable": false,
  "lastSentAt": "2026-01-01T12:00:00.000Z",
  "error": null
}
```

**Possible states:**

| State | Description |
|---|---|
| `initializing` | Service is starting up or reconnecting |
| `qr_required` | Waiting for QR scan to link WhatsApp |
| `connected` | WhatsApp is connected and ready to send |
| `disconnected` | Connection was lost, will auto-reconnect |
| `auth_failed` | Credentials invalid or logged out |
| `configuration_error` | MONGODB_URI not configured |

---

### POST /send-message

Sends a plain text WhatsApp message.

**Authentication:** `INTERNAL_API_KEY` only

**Request headers:**

```
x-internal-api-key: <INTERNAL_API_KEY>
Content-Type: application/json
```

**Request body:**

```json
{
  "phone": "<recipient phone number>",
  "message": "<message text>"
}
```

**Success response (200):**

```json
{
  "success": true
}
```

**Error response (4xx/5xx):**

```json
{
  "success": false,
  "error": "<error_reason>",
  "retryable": true
}
```

---

### POST /send-template (deprecated)

Convenience route that renders a template and sends it. **Not recommended for production use.**

**Authentication:** `INTERNAL_API_KEY` only

**Request body:**

```json
{
  "phone": "<recipient phone>",
  "template": "<template_name>",
  "data": { "...": "..." }
}
```

Use `/send-message` directly for production workloads. Template rendering should happen in the calling application.

---

### GET /pair or GET /qr

Returns QR data for pairing WhatsApp Web.

**Authentication:** `INTERNAL_API_KEY` or `PAIRING_ADMIN_TOKEN`

When the `Accept` header contains `text/html` **and** `ENABLE_PAIRING_UI=true`, returns a browser-friendly QR page.

If `ENABLE_PAIRING_UI=false` (default) and the request asks for HTML, returns:
```json
{ "error": "pairing_ui_disabled" }
```
with HTTP 403. Authenticated JSON responses are unaffected regardless of this setting.

**JSON response:**

```json
{
  "state": "qr_required",
  "qrAvailable": true,
  "qrDataUri": "data:image/png;base64,..."
}
```

When already connected, returns HTTP 409:

```json
{
  "error": "already_connected"
}
```

---

### POST /api/logout

Logs out WhatsApp and clears auth state from MongoDB.

**Authentication:** `INTERNAL_API_KEY` only

**Success response:** `204 No Content`

---

## Error Reference

| error | Meaning | Retryable |
|---|---|---|
| `unauthorized` | Missing or invalid API key | No |
| `not_connected` | WhatsApp is not in connected state | Yes |
| `phone_and_message_required` | Request body missing phone or message | No |
| `send_timeout` | Baileys sendMessage exceeded timeout | No (message may have been sent) |
| `circuit_open` | Circuit breaker is open due to repeated failures | Yes |
| `global_rate_limit` | Global send rate exceeded | Yes (after cooldown) |
| `daily_phone_limit` | Daily limit reached for this phone | No (next day) |
| `phone_send_cooldown` | Message was sent to this phone too recently | Yes (after cooldown) |
| `not_found` | Route does not exist | No |
| `already_connected` | Pairing attempted while already connected (HTTP 409) | No |
| `pairing_ui_disabled` | Browser QR pairing UI is disabled (ENABLE_PAIRING_UI=false) | No |
| `send_failed` | Generic send failure | Depends |
| `invalid_json` | Request body is not valid JSON (HTTP 400) | No |
| `payload_too_large` | Request body exceeds the size limit (HTTP 413) | No |

---

## Phone Number Format

The service accepts phone numbers as digits only or in any format (it strips all non-digit characters internally).

**Recommendation:** Callers should normalize phone numbers to full international format (e.g. `201234567890`) before sending to ensure consistent rate-limiting and daily limit tracking.

---

## Duplicate Delivery Risk

When a `send_timeout` error is returned, the underlying Baileys `sendMessage` call may
still complete successfully. The timeout only races against the Baileys promise — it does
not cancel the in-flight message.

**Implications for callers:**

- Do **not** automatically retry on `send_timeout` — the message may have been delivered.
- If you must retry, implement idempotency or deduplication in your application.
- `send_timeout` is marked `retryable: false` in the error response to signal this risk.

---

## cURL Examples

```bash
# Health check
curl http://localhost:4000/health

# Status
curl -H "x-internal-api-key: your-key" http://localhost:4000/status

# Send message
curl -X POST http://localhost:4000/send-message \
  -H "x-internal-api-key: your-key" \
  -H "Content-Type: application/json" \
  -d '{"phone":"201234567890","message":"Hello from the reusable WhatsApp service!"}'

# Pairing (QR via header)
curl -H "x-internal-api-key: your-key" http://localhost:4000/pair

# Logout
curl -X POST -H "x-internal-api-key: your-key" http://localhost:4000/api/logout
```
