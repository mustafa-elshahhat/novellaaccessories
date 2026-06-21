import { describe, it, expect } from "vitest";
import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";

/** Next.js 16 conventions: the proxy file replaces middleware; no root middleware.ts exists. */
describe("Next.js conventions", () => {
  const root = process.cwd();

  it("uses proxy.ts (the Next 16 convention)", () => {
    expect(existsSync(join(root, "proxy.ts"))).toBe(true);
  });

  it("has no root middleware.ts (deprecated for this project)", () => {
    expect(existsSync(join(root, "middleware.ts"))).toBe(false);
    expect(existsSync(join(root, "middleware.tsx"))).toBe(false);
  });

  it("exports a proxy() function and a matcher config", () => {
    const src = readFileSync(join(root, "proxy.ts"), "utf8");
    expect(src).toMatch(/export function proxy\(/);
    expect(src).toMatch(/export const config\s*=/);
  });

  it("excludes API, Next internals, and static/public files from the proxy matcher", () => {
    const src = readFileSync(join(root, "proxy.ts"), "utf8");
    for (const token of ["api", "_next/static", "_next/image", "favicon.ico", "robots.txt", "sitemap.xml"]) {
      expect(src).toContain(token);
    }
  });

  it("does not use the deprecated `next lint` command", () => {
    const pkg = JSON.parse(readFileSync(join(root, "package.json"), "utf8"));
    expect(pkg.scripts.lint).toBe("eslint .");
    expect(JSON.stringify(pkg.scripts)).not.toContain("next lint");
  });

  it("keeps API_BASE_URL server-only (no NEXT_PUBLIC_API_BASE_URL anywhere)", () => {
    const example = readFileSync(join(root, ".env.example"), "utf8");
    expect(example).toContain("API_BASE_URL=");
    expect(example).not.toContain("NEXT_PUBLIC_API_BASE_URL");
  });
});
