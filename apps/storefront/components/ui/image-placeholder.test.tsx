import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import { ImagePlaceholder } from "./image-placeholder";

describe("ImagePlaceholder", () => {
  it("renders a branded monogram SVG, never a bare 'novella' text box", () => {
    const { container } = render(<ImagePlaceholder />);
    expect(container.querySelector("svg")).not.toBeNull();
    expect(container.textContent ?? "").not.toContain("novella");
  });
});
