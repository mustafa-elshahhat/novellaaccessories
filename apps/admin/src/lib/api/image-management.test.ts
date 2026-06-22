import { describe, expect, it, vi } from "vitest";
import { productsApi } from "./products";
import { uploadsApi } from "./uploads";
import { setAccessToken } from "./client";

function mockJson(body: unknown) {
  return vi.spyOn(globalThis, "fetch").mockResolvedValue(
    new Response(JSON.stringify(body), { status: 200, headers: { "Content-Type": "application/json" } }),
  );
}

describe("admin image management api", () => {
  it("updateImage PATCHes alt text and the primary flag to the product image route", async () => {
    setAccessToken("token");
    const fetchMock = mockJson({ id: "img1", url: "u", sortOrder: 0, isPrimary: true });
    await productsApi.updateImage("p1", "img1", { altAr: "بديل", altEn: "Luna Ring", isPrimary: true });
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(String(url)).toContain("/api/admin/products/p1/images/img1");
    expect(init.method).toBe("PATCH");
    expect(JSON.parse(init.body as string)).toMatchObject({ altEn: "Luna Ring", isPrimary: true });
    fetchMock.mockRestore();
  });

  it("uploadsApi.image posts multipart form data tagged with the entity type (category/hero/product)", async () => {
    setAccessToken("token");
    const fetchMock = mockJson({ url: "https://res.cloudinary.com/demo/rings.webp", publicId: "novella/dev/categories/rings" });
    const file = new File([new Uint8Array([1, 2, 3])], "rings.webp", { type: "image/webp" });
    const result = await uploadsApi.image(file, "categories", "cat1");
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(String(url)).toContain("/api/admin/uploads/image");
    expect(init.method).toBe("POST");
    expect(init.body).toBeInstanceOf(FormData);
    expect((init.body as FormData).get("entityType")).toBe("categories");
    expect(result.publicId).toBe("novella/dev/categories/rings");
    fetchMock.mockRestore();
  });
});
