# Novella Accessories — Planning Pack

Release date: 2026-06-21

This planning pack defines the full product idea for **Novella Accessories** before implementation.

Target domain:

```text
novellaaccessories.store
```

Brand type:

```text
Soft luxury jewelry and accessories e-commerce store.
```

Default product categories:

- Rings
- Necklaces
- Earrings
- Bracelets

The project is a complete full-stack e-commerce system, not only a storefront and API prototype. It includes customer authentication, product and variant management, discounts, coupons, shipping fees, payments readiness, WhatsApp OTP and messaging, analytics, profit reporting, SEO/AEO/GEO, static content pages, and admin management.

## Planned Monorepo Apps

```text
apps/api          ASP.NET Core backend + SQL Server
apps/storefront   Next.js customer storefront
apps/admin        React admin dashboard
apps/whatsapp     Express + Baileys WhatsApp Web sidecar, adapted from whatsapp-service-template
```

`apps/whatsapp` is a standalone Express + Baileys (WhatsApp Web) sidecar adapted from
[`whatsapp-service-template`](https://github.com/mustafa-elshahhat/whatsapp-service-template). It is deployed
separately on Render, uses its own external MongoDB only for Baileys session/auth storage, runs locally on
port `4000`, and is called **only** by `apps/api` — never by the storefront or admin frontends.

## Deployment Targets

```text
Storefront:       Vercel
Admin:            Vercel
API:              SmarterASP.NET
WhatsApp:         Render
Main database:    SQL Server (all business data)
WhatsApp sessions: MongoDB (Baileys auth/session storage only)
Images:           Cloudinary
```

## Documents

1. `01_PRD.md` — Product Requirements Document.
2. `02_BUSINESS_RULES.md` — Business rules and edge cases.
3. `03_ARCHITECTURE.md` — Monorepo and technical architecture.
4. `04_DATABASE_MODEL.md` — SQL Server data model.
5. `05_API_PLAN.md` — API planning and endpoint groups.
6. `06_STOREFRONT_UX.md` — Customer storefront UX plan.
7. `07_ADMIN_DASHBOARD.md` — Admin dashboard plan.
8. `08_BRAND_UI_GUIDELINES.md` — Brand colors, tone, and UI direction based on the provided references.
9. `09_SEO_AEO_GEO_PLAN.md` — Search, answer-engine, and generative-engine optimization plan.
10. `10_WHATSAPP_SERVICE_PLAN.md` — WhatsApp OTP and messaging service plan.
11. `11_PAYMENTS_SHIPPING_PLAN.md` — Payment gateway readiness and shipping rules.
12. `12_REPORTS_ANALYTICS_PROFIT.md` — Profit, reports, and first-party analytics plan.
13. `13_DEPLOYMENT_ENV.md` — Deployment and environment variables.
14. `14_MVP_BACKLOG.md` — Implementation backlog by phase.
15. `15_REFERENCES.md` — External service references and verification notes.
16. `16_IMPLEMENTATION_PROMPT.md` — A developer/agent implementation prompt.
17. `17_FOLDER_PREPARATION_PLAN.md` — Monorepo folder and environment preparation plan.
18. `18_BACKEND_IMPLEMENTATION_PLAN.md` — Backend implementation plan.
19. `19_STOREFRONT_IMPLEMENTATION_PLAN.md` — Storefront implementation plan.
20. `20_ADMIN_IMPLEMENTATION_PLAN.md` — Admin dashboard implementation plan.

## MVP Principle

Keep the first version simple and production-oriented. Do not introduce Redis, queues, or unnecessary infrastructure at the start. The only separate service is `apps/whatsapp`, the Baileys WhatsApp Web sidecar. The system should still be designed with clean abstractions so payments and shipping companies can be integrated later, and so the WhatsApp transport could later move from Baileys to the official WhatsApp Business API if needed (a real adaptation, not a drop-in provider swap).
