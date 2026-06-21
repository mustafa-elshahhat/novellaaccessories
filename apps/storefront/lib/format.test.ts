import { describe, it, expect } from "vitest";
import { formatPrice, formatDiscountPercent } from "./format";

describe("formatPrice", () => {
  it("formats EGP currency for both locales", () => {
    const ar = formatPrice(1500, "ar");
    const en = formatPrice(1500, "en");
    // Currency rendering differs by locale, but the amount is always present.
    expect(ar).toContain("١"); // Arabic-Indic digits
    expect(en).toMatch(/1,?500/);
  });
});

describe("formatDiscountPercent", () => {
  it("rounds to a whole number", () => {
    expect(formatDiscountPercent(19.6)).toBe(20);
    expect(formatDiscountPercent(10)).toBe(10);
  });

  it("returns 0 for nullish", () => {
    expect(formatDiscountPercent(null)).toBe(0);
    expect(formatDiscountPercent(undefined)).toBe(0);
  });
});
