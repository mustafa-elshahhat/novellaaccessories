import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DataTable } from "@/components/ui";
import { renderWithProviders } from "@/test/test-utils";

describe("WhatsApp OTP redaction", () => {
  it("does not render OTP bodies when a safe renderer is used", () => {
    const rows = [{ id: "1", messageType: "Otp", status: "Sent", messageBody: "Your code is 123456" }];
    renderWithProviders(<DataTable caption="WhatsApp messages" rows={rows} columns={[{ key: "messageType", header: "Type" }, { key: "messageBody", header: "Body", render: (m) => m.messageType === "Otp" ? "Redacted OTP message" : m.messageBody }]} />);
    expect(screen.getByText("Redacted OTP message")).toBeInTheDocument();
    expect(screen.queryByText(/123456/)).not.toBeInTheDocument();
  });
});
