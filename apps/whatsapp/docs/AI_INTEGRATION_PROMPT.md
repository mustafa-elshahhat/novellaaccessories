# AI Integration Prompt

Copy the entire content below and give it to any AI coding agent to integrate this WhatsApp service into your target project.

---

START OF PROMPT CONTENT

You are a senior software architect and integration engineer.

I have a standalone reusable WhatsApp sidecar service.

It is deployed or available separately from the main application.

Your task is to integrate the target project with this WhatsApp service over HTTP.

This integration must work with the current project architecture and technology stack, whatever it is.

The target project may be built with any backend framework, including:
- ASP.NET / .NET
- Node.js / Express / NestJS
- Laravel / PHP
- Django / FastAPI / Flask
- Spring Boot / Java
- Go
- Ruby on Rails
- or any other backend stack.

The frontend may be:
- Angular
- React
- Vue
- Blazor
- Next.js
- mobile app
- desktop app
- or anything else.

Important architecture rules:
1. Do not merge the WhatsApp service into the backend runtime.
2. Do not import WhatsApp service files directly.
3. Treat WhatsApp service as a separate HTTP sidecar/microservice.
4. The backend must communicate with it using REST HTTP calls only.
5. The frontend must never call the WhatsApp service directly if that exposes secrets.
6. The frontend should call the main backend, and the backend calls the WhatsApp service.
7. Do not store WhatsApp session data in the main application database.
8. WhatsApp session/auth data belongs to the WhatsApp service MongoDB only.

First inspect the target project:
- backend structure
- frontend structure
- environment/config system
- deployment files
- logging style
- error handling style
- dependency injection or service registration pattern
- background job/outbox/event system if present
- existing notification system if present
- user/order/payment/auth flows if relevant

Then implement integration in the target project:

Backend configuration:
Add equivalent configuration keys appropriate for the target stack:

WHATSAPP_ENABLED=true
WHATSAPP_SERVICE_URL=http://localhost:4000
WHATSAPP_INTERNAL_API_KEY=<same value as WhatsApp service INTERNAL_API_KEY>

Use the target project's existing config pattern:
- appsettings.json for .NET
- .env for Node/Laravel/Django/FastAPI
- application.yml/properties for Spring Boot
- config files or environment variables for other stacks

Backend service/wrapper:
Create a WhatsApp client/wrapper in the target backend using the project's conventions.

It must support:
- health check call: GET /health
- status call: GET /status
- send message call: POST /send-message
- timeout
- safe retry for retryable failures
- graceful failure handling
- safe logging with masked phone numbers
- no secret leakage
- no direct frontend exposure of INTERNAL_API_KEY

HTTP contract:

Base URL:
{WHATSAPP_SERVICE_URL}

Authentication:
x-internal-api-key: {WHATSAPP_INTERNAL_API_KEY}

Send message:
POST {WHATSAPP_SERVICE_URL}/send-message

Body:
{
  "phone": "<recipient phone>",
  "message": "<message text>"
}

Expected success:
{
  "success": true
}

Expected failure:
{
  "success": false,
  "error": "<reason>",
  "retryable": true
}

Business workflow rules:
1. Identify where WhatsApp notifications belong in the target project.
2. Do not send messages from random controllers/components if the project has a cleaner service/event layer.
3. Prefer event-driven, outbox, queue, or background job integration if the project already has it.
4. If the project has no background job system, implement the safest minimal async/retry mechanism consistent with the stack.
5. WhatsApp failures must not break critical business transactions unless the user explicitly requires blocking behavior.
6. Avoid duplicate messages.
7. Add idempotency or message tracking if the project architecture already supports it.

Frontend integration:
1. Do not expose WhatsApp service secrets in frontend code.
2. If frontend needs WhatsApp status, add a backend endpoint that proxies safe status only.
3. If frontend needs admin QR pairing access, create an admin-only backend route/page.
4. Do not expose QR pairing to public users.
5. Follow the existing UI stack and permissions model.

Security:
1. Never commit real secrets.
2. Never log INTERNAL_API_KEY.
3. Never log full phone numbers unless explicitly required.
4. Protect admin/pairing/status endpoints.
5. Use environment variables or secret manager.
6. Validate phone/message input before sending.
7. Add request timeout to avoid hanging calls.
8. Treat the WhatsApp service as an internal service.

Deployment:
Update docs/config for deploying:
- main backend
- frontend if needed
- WhatsApp service separately
- external MongoDB for WhatsApp session
- required environment variables
- health check URL
- pairing flow
- production secrets

Testing:
Add or update tests where appropriate:
- config loading
- WhatsApp client wrapper
- send success handling
- send failure handling
- service offline handling
- unauthorized handling
- retryable error handling
- frontend/admin status if added

Deliverables:
After implementation, provide:
1. Changed files summary.
2. Environment variables added.
3. Local run instructions.
4. Deployment instructions.
5. How to pair WhatsApp.
6. How to test sending a message.
7. Any assumptions made.
8. Confirmation that the WhatsApp service remains separate and reusable.

Important:
- Preserve the target project's existing architecture.
- Do not rewrite unrelated code.
- Do not hardcode project-specific values.
- Do not make the integration specific to one framework unless the target project requires it.
- Keep the WhatsApp service technology-agnostic from the caller's perspective.

END OF PROMPT CONTENT
