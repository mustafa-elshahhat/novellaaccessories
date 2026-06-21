import "server-only";
import { cookies } from "next/headers";

const AUTH_COOKIE = "novella_token";
// Matches the backend JWT lifetime default (Jwt__ExpiryDays = 7).
const MAX_AGE_SECONDS = 60 * 60 * 24 * 7;

/** Stores the backend JWT in an HttpOnly cookie. The token is never exposed to client JS. */
export async function setAuthCookie(token: string): Promise<void> {
  const store = await cookies();
  store.set(AUTH_COOKIE, token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: MAX_AGE_SECONDS,
  });
}

export async function clearAuthCookie(): Promise<void> {
  const store = await cookies();
  store.delete(AUTH_COOKIE);
}

export async function getAuthToken(): Promise<string | undefined> {
  const store = await cookies();
  return store.get(AUTH_COOKIE)?.value;
}

export async function hasAuthCookie(): Promise<boolean> {
  return Boolean(await getAuthToken());
}
