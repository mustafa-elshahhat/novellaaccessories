import { describe, it, expect } from "vitest";
import { pick, pickSlug } from "./localize";
import { getDirection, isSupportedLocale } from "./routing";

describe("pick", () => {
  it("returns the locale-appropriate field", () => {
    expect(pick("ar", "عربي", "english")).toBe("عربي");
    expect(pick("en", "عربي", "english")).toBe("english");
  });

  it("falls back to the other locale when one side is missing", () => {
    expect(pick("ar", null, "english")).toBe("english");
    expect(pick("en", "عربي", undefined)).toBe("عربي");
  });

  it("returns an empty string when both are missing", () => {
    expect(pick("ar", null, null)).toBe("");
  });
});

describe("pickSlug", () => {
  it("returns locale-specific slug (never transliterated)", () => {
    expect(pickSlug("ar", "سلسلة", "necklace")).toBe("سلسلة");
    expect(pickSlug("en", "سلسلة", "necklace")).toBe("necklace");
  });
});

describe("routing helpers", () => {
  it("maps ar to rtl and en to ltr", () => {
    expect(getDirection("ar")).toBe("rtl");
    expect(getDirection("en")).toBe("ltr");
  });

  it("recognizes supported locales only", () => {
    expect(isSupportedLocale("ar")).toBe(true);
    expect(isSupportedLocale("en")).toBe(true);
    expect(isSupportedLocale("fr")).toBe(false);
  });
});
