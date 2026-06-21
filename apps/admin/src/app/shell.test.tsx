import { describe, expect, it } from "vitest";
import { navigation } from "./shell";

describe("admin navigation", () => {
  it("contains every implemented protected module", () => {
    const links = navigation.flatMap((section) => section.items.map((item) => item.to));
    expect(links).toEqual(expect.arrayContaining(["/dashboard", "/products", "/categories", "/orders", "/customers", "/coupons", "/coupons/two-order-settings", "/shipping", "/heroes", "/whatsapp/settings", "/whatsapp/logs", "/payments", "/expenses", "/reports", "/analytics", "/pages", "/seo", "/settings"]));
  });
});
