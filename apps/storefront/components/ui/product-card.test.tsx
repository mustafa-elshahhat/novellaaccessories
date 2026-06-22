import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithIntl } from "@/lib/test-utils";
import type { PublicProductListItem } from "@/lib/api/types";

// Avoid loading next-intl's navigation (pulls in next/navigation, which vitest cannot resolve here).
vi.mock("@/lib/i18n/navigation", () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a>,
}));

const { ProductCard } = await import("./product-card");

const base: PublicProductListItem = {
  id: "1", nameAr: "خاتم لونا", nameEn: "Luna Ring", slugAr: "khatam-luna", slugEn: "luna-ring",
  originalPrice: 420, finalPrice: 357, hasDiscount: true, discountPercentage: 15,
  isAvailable: true, isFeatured: true,
  primaryImageUrl: "https://res.cloudinary.com/demo/novella/dev/products/luna-ring.webp",
};

describe("ProductCard", () => {
  it("renders the product image with the product name as alt when a primary image is present", () => {
    renderWithIntl(<ProductCard product={base} />);
    expect(screen.getByRole("img")).toHaveAttribute("alt", "Luna Ring");
  });

  it("falls back to a branded placeholder (not a bare 'novella' box) when no image", () => {
    const { container } = renderWithIntl(<ProductCard product={{ ...base, primaryImageUrl: null }} />);
    expect(screen.queryByRole("img")).toBeNull();
    expect(container.querySelector("svg")).not.toBeNull();
    // The product name renders once (heading); the image area shows no duplicate / bare 'novella' text.
    expect(screen.getAllByText("Luna Ring")).toHaveLength(1);
  });
});
