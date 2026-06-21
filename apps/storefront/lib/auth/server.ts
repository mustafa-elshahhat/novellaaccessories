import "server-only";
import { me } from "@/lib/api/auth";
import { hasAuthCookie } from "@/lib/api/cookies";
import type { CustomerProfile } from "@/lib/api/types";

/**
 * Resolves the current customer from the backend (`/api/auth/me`) using the HttpOnly cookie.
 * Returns null when unauthenticated. NOTE: reads cookies, so callers become dynamically rendered —
 * use only in protected layouts/pages, never in cacheable public routes.
 */
export async function getCurrentCustomer(): Promise<CustomerProfile | null> {
  if (!(await hasAuthCookie())) return null;
  try {
    const res = await me();
    return res.customer;
  } catch {
    return null;
  }
}
