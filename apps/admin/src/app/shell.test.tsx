import { describe, expect, it } from "vitest";
import { navigation } from "./shell";

describe("admin navigation", () => {
  it("contains every implemented protected module", () => {
    const links = navigation.flatMap((section) => section.items.map((item) => item.to));
    expect(links).toEqual(["/dashboard", "/products", "/categories", "/orders", "/customers", "/discounts", "/content", "/shipping", "/whatsapp", "/expenses", "/reports"]);
  });
});
