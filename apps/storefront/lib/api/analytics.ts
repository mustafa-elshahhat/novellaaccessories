import "server-only";
import { apiFetch } from "./server";
import type { AnalyticsEvent } from "./types";

export interface StartSessionBody {
  anonymousId: string;
  landingPage?: string | null;
  referrer?: string | null;
  utmSource?: string | null;
  utmMedium?: string | null;
  utmCampaign?: string | null;
  deviceType?: string | null;
  language?: string | null;
}

export interface StartSessionResult {
  visitorId: string;
  sessionId: string;
}

// `auth: true` attaches the bearer token when the visitor is logged in (anonymous otherwise),
// so the backend can associate analytics with the customer.
export const startSession = (body: StartSessionBody) =>
  apiFetch<StartSessionResult>("/api/analytics/session/start", {
    method: "POST",
    auth: true,
    body,
  });

export const trackEvents = (events: AnalyticsEvent[]) =>
  apiFetch<{ received: boolean }>("/api/analytics/events", {
    method: "POST",
    auth: true,
    body: { events },
  });

export const identify = (body: { sessionId: string; visitorId: string }) =>
  apiFetch<{ success: boolean }>("/api/analytics/session/identify", {
    method: "POST",
    auth: true,
    body,
  });
