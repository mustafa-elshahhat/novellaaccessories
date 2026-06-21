import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { trackEvents } from "@/lib/api/analytics";
import type { AnalyticsEvent } from "@/lib/api/types";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const body = await readJson<{ events: AnalyticsEvent[] }>(request);
  return jsonResult(() => trackEvents(body.events ?? []));
}
