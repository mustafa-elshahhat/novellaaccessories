import { describe, it, expect } from "vitest";
import { faqJsonLd } from "./jsonld";

describe("faqJsonLd", () => {
  it("builds a FAQPage with question/answer entities for structured data", () => {
    const data = faqJsonLd([
      { question: "How do I order from Novella?", answer: "Add a product to the cart and complete checkout." },
      { question: "Is cash on delivery available?", answer: "Yes, in the current version." },
    ]);
    expect(data["@type"]).toBe("FAQPage");
    expect(data.mainEntity).toHaveLength(2);
    expect(data.mainEntity[0]).toMatchObject({ "@type": "Question", name: "How do I order from Novella?" });
  });
});
