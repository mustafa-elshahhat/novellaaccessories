# References — Novella Accessories

## 1. Purpose

This file lists external services and provider categories that should be checked before implementation or activation. It does not lock the project into one provider.

## 2. Payment Providers to Evaluate

Possible Egyptian payment providers:

- Paymob.
- Fawry.
- Geidea.
- Kashier.
- Any provider that confirms support for bank cards, wallets, and Instapay/IPN-compatible flows.

## 3. Payment Verification Rule

Before activating online payment methods, verify directly from the selected provider:

- Bank card support.
- Electronic wallet support.
- Instapay support or IPN-compatible flow.
- Fees and settlement rules.
- Webhook/callback support.
- Test credentials.
- Production approval requirements.

## 4. Instapay Note

Instapay support should not be assumed for every checkout provider. The system should be prepared for it, but activation depends on provider confirmation.

## 5. WhatsApp Reference

Base repository (production-ready, not a placeholder):

```text
https://github.com/mustafa-elshahhat/whatsapp-service-template
```

It is a standalone Express + Baileys (WhatsApp Web) sidecar: Node.js, MongoDB session storage,
Pino logging, rate limiting, circuit breaker, and a pairing/QR flow.

Planned project app:

```text
apps/whatsapp
```

Architecture:

```text
apps/api -> HTTP REST -> apps/whatsapp -> Baileys -> WhatsApp Web
```

Deployment:

```text
Render (separate service)
```

Session storage:

```text
External MongoDB (Baileys auth/session only, separate from SQL Server)
```

Notes:

- Called only by `apps/api`; OTP generation/verification stay in `apps/api`; the sidecar only sends.
- Baileys is unofficial WhatsApp Web automation (account/ban risk to consider). The official
  WhatsApp Business API is a possible future replacement, not part of the current MVP contract.

## 6. Image Hosting

Cloudinary is selected for MVP image storage.

Use it for:

- Product images.
- Category images.
- Hero images.
- Static page images if needed.

## 7. Deployment References

Deployment targets:

- Vercel for storefront.
- Vercel for admin.
- SmarterASP.NET for API.
- Render for the WhatsApp service.
- SQL Server for the main business database.
- External MongoDB for WhatsApp (Baileys) session storage only.

## 8. Domain

Target domain:

```text
novellaaccessories.store
```

## 9. Brand References

The brand style is based on the provided Novella images:

- Warm ivory background.
- Rose/champagne gold logo.
- Lowercase wordmark.
- Jewelry and feminine line-art motif.
- English tagline: `Jewelry That Tells Your Story`.
- Arabic/English announcement poster style.
