"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  type ReactNode,
} from "react";
import { useLocale } from "next-intl";
import { usePathname } from "@/lib/i18n/navigation";
import { publicEnv } from "@/lib/env";
import { bff } from "@/lib/api/bff-client";
import { useAuth } from "@/features/auth/auth-provider";
import type { AnalyticsEvent, AnalyticsEventType } from "@/lib/api/types";

const VISITOR_KEY = "novella_visitor_id";

interface TrackPayload {
  productId?: string | null;
  orderId?: string | null;
  metadata?: Record<string, unknown>;
}

interface AnalyticsApi {
  track: (type: AnalyticsEventType, payload?: TrackPayload) => void;
}

const AnalyticsContext = createContext<AnalyticsApi>({ track: () => {} });

function getVisitorId(): string {
  try {
    let id = localStorage.getItem(VISITOR_KEY);
    if (!id) {
      id = crypto.randomUUID();
      localStorage.setItem(VISITOR_KEY, id);
    }
    return id;
  } catch {
    return crypto.randomUUID();
  }
}

function detectDeviceType(): string {
  if (typeof navigator === "undefined") return "unknown";
  return /Mobi|Android|iPhone|iPad/i.test(navigator.userAgent) ? "mobile" : "desktop";
}

/**
 * Lightweight first-party analytics. Gated by NEXT_PUBLIC_ANALYTICS_ENABLED. All calls are
 * fire-and-forget and never block shopping. Sends only non-sensitive metadata — never
 * passwords, OTPs, phone numbers, addresses, tokens, or cart contents.
 */
export function AnalyticsProvider({ children }: { children: ReactNode }) {
  const enabled = publicEnv.analyticsEnabled;
  const locale = useLocale();
  const pathname = usePathname();
  const { customer } = useAuth();

  const session = useRef<{ sessionId: string; visitorId: string } | null>(null);
  const started = useRef(false);
  const lastPage = useRef<string | null>(null);
  const identified = useRef(false);

  const track = useCallback(
    (type: AnalyticsEventType, payload?: TrackPayload) => {
      if (!enabled || !session.current) return;
      const event: AnalyticsEvent = {
        sessionId: session.current.sessionId,
        visitorId: session.current.visitorId,
        eventType: type,
        pageUrl: typeof window !== "undefined" ? window.location.pathname : null,
        productId: payload?.productId ?? null,
        orderId: payload?.orderId ?? null,
        metadataJson: payload?.metadata ? JSON.stringify(payload.metadata) : null,
      };
      void bff("/api/analytics/events", {
        method: "POST",
        body: JSON.stringify({ events: [event] }),
      }).catch(() => {});
    },
    [enabled],
  );

  // Start a session exactly once (guards StrictMode double-invocation).
  useEffect(() => {
    if (!enabled || started.current) return;
    started.current = true;
    const visitorId = getVisitorId();
    const params = new URLSearchParams(window.location.search);
    void (async () => {
      try {
        const res = await bff<{ visitorId: string; sessionId: string }>(
          "/api/analytics/session/start",
          {
            method: "POST",
            body: JSON.stringify({
              anonymousId: visitorId,
              landingPage: window.location.pathname,
              referrer: document.referrer || null,
              utmSource: params.get("utm_source"),
              utmMedium: params.get("utm_medium"),
              utmCampaign: params.get("utm_campaign"),
              deviceType: detectDeviceType(),
              language: locale,
            }),
          },
        );
        session.current = { sessionId: res.sessionId, visitorId: res.visitorId };
        lastPage.current = window.location.pathname;
        track("PageView");
      } catch {
        // analytics is non-blocking
      }
    })();
  }, [enabled, locale, track]);

  // Track page views on route change (deduped).
  useEffect(() => {
    if (!enabled || !session.current) return;
    if (lastPage.current === pathname) return;
    lastPage.current = pathname;
    track("PageView");
  }, [enabled, pathname, track]);

  // Identify the visitor after login.
  useEffect(() => {
    if (!enabled || !session.current || !customer || identified.current) return;
    identified.current = true;
    const current = session.current;
    void bff("/api/analytics/session/identify", {
      method: "POST",
      body: JSON.stringify({
        sessionId: current.sessionId,
        visitorId: current.visitorId,
      }),
    }).catch(() => {});
  }, [enabled, customer]);

  return (
    <AnalyticsContext.Provider value={{ track }}>{children}</AnalyticsContext.Provider>
  );
}

export function useAnalytics(): AnalyticsApi {
  return useContext(AnalyticsContext);
}
