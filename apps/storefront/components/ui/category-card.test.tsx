import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithIntl } from "@/lib/test-utils";
import type { PublicCategory } from "@/lib/api/types";

// Avoid loading next-intl's navigation (pulls in next/navigation, which vitest cannot resolve here).
vi.mock("@/lib/i18n/navigation", () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a>,
}));

const { CategoryCard } = await import("./category-card");

const category: PublicCategory = {
  id: "1", nameAr: "خواتم", nameEn: "Rings", slugAr: "khawatem", slugEn: "rings",
  imageUrl: null, sortOrder: 1,
};

describe("CategoryCard", () => {
  it("renders the category name only once when no image (no duplicate placeholder name)", () => {
    const { container } = renderWithIntl(<CategoryCard category={category} />);
    expect(screen.getAllByText("Rings")).toHaveLength(1);
    expect(container.querySelector("svg")).not.toBeNull();
  });

  it("uses the localized image alt text when an image is present", () => {
    renderWithIntl(
      <CategoryCard
        category={{ ...category, imageUrl: "https://res.cloudinary.com/demo/cat.webp", imageAltEn: "Novella rings collection" }}
      />,
    );
    expect(screen.getByRole("img")).toHaveAttribute("alt", "Novella rings collection");
  });
});
