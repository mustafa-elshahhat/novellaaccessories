import { describe, it, expect } from "vitest";
import { isTrustedOrigin } from "./csrf";

// In the test env NEXT_PUBLIC_SITE_URL is unset, so the configured site host
// defaults to "localhost:3000" (see lib/env.ts).
function req(headers: Record<string, string>): Request {
  return new Request("http://localhost:3000/api/cart", {
    method: "POST",
    headers,
  });
}

describe("isTrustedOrigin", () => {
  it("accepts a same-origin request (origin host === site host)", () => {
    expect(isTrustedOrigin(req({ origin: "http://localhost:3000" }))).toBe(true);
  });

  it("rejects a foreign origin", () => {
    expect(isTrustedOrigin(req({ origin: "https://evil.com" }))).toBe(false);
  });

  it("rejects a missing origin header", () => {
    expect(isTrustedOrigin(req({}))).toBe(false);
  });

  it("rejects a malformed origin header", () => {
    expect(isTrustedOrigin(req({ origin: "not-a-url" }))).toBe(false);
  });
});
