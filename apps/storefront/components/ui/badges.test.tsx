import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithIntl } from "@/lib/test-utils";
import { DiscountBadge, AvailabilityBadge } from "./badges";
import { PriceDisplay } from "./price";

describe("AvailabilityBadge", () => {
  it("shows availability only — never a stock count", () => {
    renderWithIntl(<AvailabilityBadge available={true} />);
    expect(screen.getByText("Available")).toBeInTheDocument();
    // The badge text is purely a status word; it must not contain digits (stock count).
    expect(screen.getByText("Available").textContent).not.toMatch(/\d/);
  });

  it("renders the unavailable state", () => {
    renderWithIntl(<AvailabilityBadge available={false} />);
    expect(screen.getByText("Unavailable")).toBeInTheDocument();
  });
});

describe("DiscountBadge", () => {
  it("renders the rounded percentage", () => {
    renderWithIntl(<DiscountBadge percent={19.6} />);
    expect(screen.getByText("-20%")).toBeInTheDocument();
  });

  it("renders nothing when there is no discount", () => {
    const { container } = renderWithIntl(<DiscountBadge percent={0} />);
    expect(container).toBeEmptyDOMElement();
  });
});

describe("PriceDisplay", () => {
  it("shows the original price struck through when discounted", () => {
    renderWithIntl(<PriceDisplay original={1000} final={800} />);
    // Both prices render; the discounted one is present.
    expect(screen.getByText(/800/)).toBeInTheDocument();
    expect(screen.getByText(/1,?000/)).toBeInTheDocument();
  });

  it("shows a single price when there is no discount", () => {
    renderWithIntl(<PriceDisplay original={500} final={500} />);
    expect(screen.queryByText(/500.*500/)).not.toBeInTheDocument();
  });
});
