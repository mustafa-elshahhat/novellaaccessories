# Deployment & Environment Plan — Novella Accessories

## 1. Deployment Targets

```text
apps/storefront   Vercel
apps/admin        Vercel
apps/api          SmarterASP.NET
apps/whatsapp     Render
Main database     SQL Server (all business data)
WhatsApp sessions MongoDB (Baileys auth/session storage only, separate from SQL Server)
Images            Cloudinary
Domain            novellaaccessories.store
```

## 2. Environments

Recommended environments:

- Local development.
- Staging optional.
- Production.

## 3. Storefront Environment Variables

```text
NEXT_PUBLIC_API_BASE_URL=
NEXT_PUBLIC_SITE_URL=https://novellaaccessories.store
NEXT_PUBLIC_DEFAULT_LOCALE=ar
NEXT_PUBLIC_SUPPORTED_LOCALES=ar,en
NEXT_PUBLIC_ANALYTICS_ENABLED=true
```

## 4. Admin Environment Variables

```text
VITE_API_BASE_URL=
VITE_APP_NAME=Novella Admin
```

## 5. API Environment Variables

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=
Jwt__Issuer=
Jwt__Audience=
Jwt__SigningKey=
Auth__CookieDomain=
Cloudinary__CloudName=
Cloudinary__ApiKey=
Cloudinary__ApiSecret=
WhatsApp__BaseUrl=
WhatsApp__InternalApiKey=
Payment__ActiveProvider=
Payment__WebhookSecret=
Cors__StorefrontOrigin=https://novellaaccessories.store
Cors__AdminOrigin=
```

Admin origin depends on the final Vercel admin domain.

## 6. WhatsApp Service Environment Variables

`apps/whatsapp` is the Express + Baileys (WhatsApp Web) sidecar. Its environment contract:

```text
NODE_ENV=production
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

- `MONGODB_URI` is **required** for Baileys auth/session storage and must point to a MongoDB that is **separate** from the main SQL Server database.
- `INTERNAL_API_KEY` authenticates calls from `apps/api` (sent as `x-internal-api-key` / `Authorization: Bearer`). It must **never** be exposed to any frontend.
- `PAIRING_ADMIN_TOKEN` protects the pairing endpoints and must never be exposed publicly.
- `ENABLE_PAIRING_UI` stays `false` in production (browser QR UI disabled by default).
- The upstream template default port may be `3005`; Novella uses `PORT=4000` to match the monorepo local port plan.
- The official WhatsApp Business API variables (`WHATSAPP_API_KEY`, `WHATSAPP_PHONE_NUMBER_ID`, `WHATSAPP_ACCESS_TOKEN`, etc.) are **future-only** and not part of the current Baileys-based MVP contract.

## 7. Cloudinary

Use Cloudinary for:

- Product images.
- Category images.
- Hero images.
- Static page images if needed.

Store in database:

- Secure URL.
- Public ID.
- Alt text.
- Sort order.

## 8. Databases

### SQL Server (main business database)

Hosted through the selected provider. Used by `apps/api` for all business data.

Requirements:

- Migrations.
- Seed data.
- Backups if available.
- Connection string stored only in API environment.

### MongoDB (WhatsApp sessions only)

External MongoDB (for example MongoDB Atlas) used by `apps/whatsapp` for Baileys auth/session storage only.

Requirements:

- Reachable from the Render WhatsApp service.
- `MONGODB_URI` stored only in the WhatsApp service environment.
- Separate from SQL Server; holds no business data.

## 9. Domain Plan

Main domain:

```text
novellaaccessories.store
```

Suggested DNS:

```text
novellaaccessories.store        Storefront on Vercel
www.novellaaccessories.store    Storefront on Vercel
api.novellaaccessories.store    Optional API custom domain if configured
admin.novellaaccessories.store  Optional admin custom domain if configured
```

If API/admin custom domains are not configured at first, use provider-generated URLs and environment variables.

## 10. CORS

API must allow:

- Storefront Vercel origin.
- Admin Vercel origin.
- Local dev origins.

Do not allow wildcard origins in production.

## 11. Secrets

Do not commit:

- Connection strings (SQL Server and MongoDB `MONGODB_URI`).
- JWT secrets.
- Cloudinary secrets.
- Payment secrets.
- WhatsApp `INTERNAL_API_KEY` and `PAIRING_ADMIN_TOKEN`.
- Admin password.

Use provider environment variables.

## 12. Deployment Notes

### Storefront on Vercel

- Set API URL.
- Set site URL.
- Configure domain.
- Ensure sitemap and robots are generated.

### Admin on Vercel

- Set API URL.
- Protect admin routes through login.

### API on SmarterASP.NET

- Deploy ASP.NET Core app.
- Configure SQL Server connection string.
- Run migrations.
- Configure CORS.
- Configure Cloudinary.
- Configure WhatsApp service URL.

### WhatsApp on Render

- Deploy the Node.js (Express + Baileys) sidecar as a separate Render service.
- Set `MONGODB_URI` (separate external MongoDB for Baileys sessions).
- Set `INTERNAL_API_KEY` and `PAIRING_ADMIN_TOKEN`.
- Keep `ENABLE_PAIRING_UI=false`; set `PORT`, send-throttle, and circuit-breaker variables.
- Verify `GET /health` (public liveness/readiness).
- Pair the WhatsApp account once (operational step via `/pair` / `/qr`), then confirm `GET /status` shows connected.
- Set `WhatsApp__BaseUrl` + `WhatsApp__InternalApiKey` on `apps/api` to point at this service.

## 13. Local Development

Suggested local ports:

```text
API:        http://localhost:5000
Storefront: http://localhost:3000
Admin:      http://localhost:5173
WhatsApp:   http://localhost:4000
```

## 14. Backups

MVP should at least document:

- SQL Server backup process.
- Cloudinary asset retention.
- Environment variable backup outside repository.

## 15. Production Checklist

- Database migrations applied.
- Admin user created.
- Default categories seeded.
- Governorates seeded.
- Static pages seeded.
- Cloudinary configured.
- MongoDB configured for the WhatsApp service (`MONGODB_URI` set, reachable).
- WhatsApp service health check passes (`GET /health`).
- WhatsApp account paired and `GET /status` shows connected.
- Storefront domain configured.
- CORS configured.
- SEO metadata verified.
- robots.txt verified.
- sitemap.xml verified.
- Test order placed.
- Test WhatsApp OTP sent.
- Test product discount and coupon calculation.
