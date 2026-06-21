import { describe, it, expect } from "vitest";
import { sanitizeReturnUrl } from "./redirect";

const FB = "/ar/account";

describe("sanitizeReturnUrl", () => {
  it("accepts a safe same-origin path", () => {
    expect(sanitizeReturnUrl("/ar/checkout", FB)).toBe("/ar/checkout");
    expect(sanitizeReturnUrl("/en/account/orders?page=2", FB)).toBe(
      "/en/account/orders?page=2",
    );
  });

  it("falls back for empty / nullish input", () => {
    expect(sanitizeReturnUrl(null, FB)).toBe(FB);
    expect(sanitizeReturnUrl(undefined, FB)).toBe(FB);
    expect(sanitizeReturnUrl("   ", FB)).toBe(FB);
  });

  it("rejects values not starting with a single slash", () => {
    expect(sanitizeReturnUrl("ar/checkout", FB)).toBe(FB);
    expect(sanitizeReturnUrl("https://evil.com", FB)).toBe(FB);
  });

  it("rejects protocol-relative //host incl. encoded slash", () => {
    expect(sanitizeReturnUrl("//evil.com", FB)).toBe(FB);
    expect(sanitizeReturnUrl("/%2Fevil.com", FB)).toBe(FB);
  });

  it("keeps a leading-slash path even if it contains a colon (treated as a path segment)", () => {
    // Starts with a single "/", so it stays on-origin and is safe.
    expect(sanitizeReturnUrl("/javascript:alert(1)", FB)).toBe("/javascript:alert(1)");
  });

  it("rejects malformed percent-encoding", () => {
    expect(sanitizeReturnUrl("/%E0%A4%A", FB)).toBe(FB);
  });

  it("rejects backslashes", () => {
    expect(sanitizeReturnUrl("/ar\\evil", FB)).toBe(FB);
  });

  it("rejects embedded control characters", () => {
    expect(sanitizeReturnUrl("/ar\n/checkout", FB)).toBe(FB);
  });
});
