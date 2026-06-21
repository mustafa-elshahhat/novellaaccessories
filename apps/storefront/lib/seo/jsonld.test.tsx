import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import {
  JsonLd,
  availabilitySchema,
  productJsonLd,
  collectionJsonLd,
} from "./jsonld";

describe("availabilitySchema", () => {
  it("maps the customer-safe availability flag (no stock counts)", () => {
    expect(availabilitySchema(true)).toBe("https://schema.org/InStock");
    expect(availabilitySchema(false)).toBe("https://schema.org/OutOfStock");
  });
});

describe("productJsonLd", () => {
  it("emits a Product + Offer with EGP currency and no cost fields", () => {
    const data = productJsonLd({
      name: "Gold Necklace",
      images: ["https://res.cloudinary.com/x/a.jpg"],
      url: "https://novellaaccessories.store/en/product/gold-necklace",
      price: 1200,
      isAvailable: true,
    });
    expect(data["@type"]).toBe("Product");
    const offer = data.offers as Record<string, unknown>;
    expect(offer.priceCurrency).toBe("EGP");
    expect(offer.price).toBe(1200);
    expect(offer.availability).toBe("https://schema.org/InStock");
    // No cost/stock/profit leakage into structured data. ("InStock" availability is allowed,
    // so scan for the actual forbidden field names rather than the substring "stock".)
    const json = JSON.stringify(data).toLowerCase();
    expect(json).not.toContain("cost");
    expect(json).not.toContain("stockquantity");
    expect(json).not.toContain("profit");
    expect(json).not.toContain("purchase");
  });
});

describe("collectionJsonLd", () => {
  it("builds a CollectionPage", () => {
    const data = collectionJsonLd({
      name: "Offers",
      url: "https://novellaaccessories.store/en/offers",
    });
    expect(data["@type"]).toBe("CollectionPage");
  });
});

describe("JsonLd component", () => {
  it("escapes < to prevent script-tag breakout", () => {
    const { container } = render(
      <JsonLd data={{ name: "</script><script>alert(1)" }} />,
    );
    const script = container.querySelector("script");
    expect(script?.innerHTML).not.toContain("</script><script>");
    expect(script?.innerHTML).toContain("\\u003c");
  });
});
