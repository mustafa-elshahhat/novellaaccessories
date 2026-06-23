import { describe, it, expect } from "vitest";
import {
  entityTitle,
  excerpt,
  productMetaDescription,
  categoryMetaDescription,
  pageMetaDescription,
} from "./metadata-text";

describe("entityTitle", () => {
  it("joins the localized name with the brand title template", () => {
    expect(entityTitle("en", "Luna Ring")).toBe("Luna Ring | Novella Accessories");
    expect(entityTitle("ar", "خاتم لونا")).toBe("خاتم لونا | نوفيلا أكسسوارات");
  });

  it("falls back to the brand name when the entity name is empty", () => {
    expect(entityTitle("en", "   ")).toBe("Novella Accessories");
  });
});

describe("excerpt", () => {
  it("strips HTML, normalizes whitespace and keeps short content intact", () => {
    expect(excerpt("<p>Hello   <strong>world</strong></p>\n\nagain")).toBe("Hello world again");
  });

  it("tolerates unmatched/malformed markup without leaking angle brackets", () => {
    expect(excerpt("Safe text <broken")).toBe("Safe text");
    const stray = excerpt("a < b and c > d");
    expect(stray).not.toContain("<");
    expect(stray).not.toContain(">");
  });

  it("truncates long content at a word boundary with an ellipsis", () => {
    const long = "word ".repeat(60).trim();
    const result = excerpt(long, 40);
    expect(result.length).toBeLessThanOrEqual(41); // 40 + ellipsis
    expect(result.endsWith("…")).toBe(true);
    expect(result).not.toContain("wor…"); // never cuts mid-word
  });

  it("returns empty string for empty input", () => {
    expect(excerpt("")).toBe("");
    expect(excerpt(null)).toBe("");
    expect(excerpt(undefined)).toBe("");
  });
});

describe("productMetaDescription", () => {
  it("uses the product description when present", () => {
    expect(productMetaDescription("en", "Luna Ring", "A delicate ring.")).toBe("A delicate ring.");
  });

  it("generates a localized fallback (name + Egypt delivery + brand) when empty", () => {
    const en = productMetaDescription("en", "Luna Ring", "   ");
    expect(en).toContain("Luna Ring");
    expect(en).toContain("Novella Accessories");
    expect(en).toContain("Egypt");

    const ar = productMetaDescription("ar", "خاتم لونا", null);
    expect(ar).toContain("خاتم لونا");
    expect(ar).toContain("مصر");
  });
});

describe("categoryMetaDescription", () => {
  it("uses the category description when present", () => {
    expect(categoryMetaDescription("en", "Rings", "Our rings.")).toBe("Our rings.");
  });

  it("generates a localized fallback when empty", () => {
    const en = categoryMetaDescription("en", "Rings", null);
    expect(en).toContain("Rings");
    expect(en).toContain("Novella Accessories");
  });
});

describe("pageMetaDescription", () => {
  it("derives an excerpt from page content", () => {
    expect(pageMetaDescription("en", "<p>About <b>Novella</b>.</p>", "About")).toBe(
      "About Novella.",
    );
  });

  it("falls back to title + brand when content is empty", () => {
    expect(pageMetaDescription("en", "", "Contact")).toBe("Contact — Novella Accessories.");
  });
});
