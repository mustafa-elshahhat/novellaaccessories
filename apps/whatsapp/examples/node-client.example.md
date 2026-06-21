# Node.js / Express Integration Example

## Configuration (.env)

```env
WHATSAPP_ENABLED=true
WHATSAPP_SERVICE_URL=http://localhost:4000
WHATSAPP_INTERNAL_API_KEY=your-32-char-key-here
WHATSAPP_TIMEOUT_MS=10000
```

## HTTP Client Wrapper

```js
// services/whatsappClient.js
const TIMEOUT_MS = parseInt(process.env.WHATSAPP_TIMEOUT_MS || '10000', 10);
const SERVICE_URL = process.env.WHATSAPP_SERVICE_URL;
const API_KEY = process.env.WHATSAPP_INTERNAL_API_KEY;

function maskPhone(phone) {
  if (!phone || phone.length < 6) return '****';
  return phone.slice(0, 2) + '*'.repeat(phone.length - 6) + phone.slice(-4);
}

export async function sendMessage(phone, message) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), TIMEOUT_MS);

  try {
    const res = await fetch(`${SERVICE_URL}/send-message`, {
      method: 'POST',
      headers: {
        'x-internal-api-key': API_KEY,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ phone, message }),
      signal: controller.signal,
    });
    const data = await res.json();
    if (!data.success) {
      console.warn('WhatsApp send failed', { phone: maskPhone(phone), error: data.error });
      return { ok: false, retryable: data.retryable };
    }
    return { ok: true };
  } catch (err) {
    console.warn('WhatsApp send error', { phone: maskPhone(phone), error: err.message });
    return { ok: false, retryable: true };
  } finally {
    clearTimeout(timer);
  }
}

export async function getStatus() {
  const res = await fetch(`${SERVICE_URL}/status`, {
    headers: { 'x-internal-api-key': API_KEY },
  });
  return res.json();
}

export async function healthCheck() {
  try {
    const res = await fetch(`${SERVICE_URL}/health`);
    const data = await res.json();
    return data.ok === true;
  } catch {
    return false;
  }
}
```

## Usage

```js
import { sendMessage } from './services/whatsappClient.js';

async function notifyCustomer(phone, orderNumber) {
  const result = await sendMessage(phone, `Order #${orderNumber} has been confirmed!`);
  if (!result.ok) {
    // Queue for retry or log
    console.warn('WhatsApp notification will be retried');
  }
}
```
