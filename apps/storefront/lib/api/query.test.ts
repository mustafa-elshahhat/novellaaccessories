import { describe, it, expect } from "vitest";
import { buildProductListQuery, parseProductListQuery } from "./query";

describe("buildProductListQuery", () => {
  it("omits undefined params", () => {
    expect(buildProductListQuery({})).toBe("");
  });

  it("serializes the offers (active-discount) filter", () => {
    expect(buildProductListQuery({ hasDiscount: true, page: 2, pageSize: 20 })).toBe(
      "?page=2&pageSize=20&hasDiscount=true",
    );
  });

  it("serializes search and category", () => {
    expect(buildProductListQuery({ search: "gold", categorySlug: "rings" })).toBe(
      "?search=gold&categorySlug=rings",
    );
  });
});

describe("parseProductListQuery", () => {
  it("clamps page to >=1 and pageSize to <=100", () => {
    expect(parseProductListQuery({ page: "-3", pageSize: "999" })).toMatchObject({
      page: 1,
      pageSize: 100,
    });
    expect(parseProductListQuery({ page: "4", pageSize: "50" })).toMatchObject({
      page: 4,
      pageSize: 50,
    });
  });

  it("reads the first value of array params", () => {
    expect(parseProductListQuery({ search: ["gold", "silver"] }).search).toBe("gold");
  });

  it("defaults when params are absent", () => {
    expect(parseProductListQuery({})).toMatchObject({ page: 1, pageSize: 20 });
  });
});
