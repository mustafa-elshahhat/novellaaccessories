import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DataTable, SecretStatus } from "./ui";
import { renderWithProviders } from "@/test/test-utils";

describe("security boundary rendering", () => {
  it("renders configured secret status without raw values", () => {
    renderWithProviders(<SecretStatus label="Internal API key" configured />);
    expect(screen.getByText("Internal API key")).toBeInTheDocument();
    expect(screen.getByText("configured")).toBeInTheDocument();
    expect(screen.queryByText(/PAIRING|MONGODB|secret-value/i)).not.toBeInTheDocument();
  });

  it("can render admin-only cost and profit fields inside admin tables", () => {
    renderWithProviders(<DataTable caption="Admin costs" rows={[{ id: "1", basePurchasePrice: "EGP 10", lineGrossProfit: "EGP 5", actualShippingCost: "EGP 2", stockQuantity: 3 }]} columns={[{ key: "basePurchasePrice", header: "Purchase cost" }, { key: "lineGrossProfit", header: "Gross profit" }, { key: "actualShippingCost", header: "Actual shipping cost" }, { key: "stockQuantity", header: "Exact stock" }]} />);
    expect(screen.getByText("Purchase cost")).toBeInTheDocument();
    expect(screen.getByText("Gross profit")).toBeInTheDocument();
    expect(screen.getByText("Actual shipping cost")).toBeInTheDocument();
    expect(screen.getByText("Exact stock")).toBeInTheDocument();
  });

  it("does not persist tokens in localStorage during tests", () => {
    localStorage.setItem("unrelated", "ok");
    expect(localStorage.getItem("novella.admin.sessionToken")).toBeNull();
  });
});
