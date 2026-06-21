import { describe, it, expect } from "vitest";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";

/**
 * Leakage contract: the storefront's customer-facing types and view-models must NEVER mention
 * cost, profit, exact stock, provider payloads, or any internal secret. These are forbidden as
 * identifiers anywhere in lib/ and features/ (comments are stripped before scanning so the
 * intentional "do not include …" doc note in types.ts does not trip the check).
 */
const FORBIDDEN = [
  "basePurchasePrice",
  "purchasePriceOverride",
  "purchaseCostPerUnit",
  "lineCost",
  "grossProfit",
  "netProfit",
  "actualShippingCost",
  "shippingMargin",
  "stockQuantity",
  "providerResponse",
  "codeHash",
  "passwordHash",
  "internalApiKey",
  "apiSecret",
  "signingKey",
  "webhookSecret",
  "mongodb_uri",
  "pairing_admin_token",
];

function stripComments(src: string): string {
  return src.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");
}

function walk(dir: string): string[] {
  const out: string[] = [];
  for (const name of readdirSync(dir)) {
    if (name === "node_modules" || name === ".next") continue;
    const full = join(dir, name);
    if (statSync(full).isDirectory()) out.push(...walk(full));
    else if (/\.(ts|tsx)$/.test(name) && !name.endsWith(".test.ts") && !name.endsWith(".test.tsx"))
      out.push(full);
  }
  return out;
}

describe("leakage contract", () => {
  const files = [...walk(join(process.cwd(), "lib")), ...walk(join(process.cwd(), "features"))];

  it("scans a non-trivial number of source files", () => {
    expect(files.length).toBeGreaterThan(20);
  });

  it("contains no forbidden cost/stock/profit/secret identifiers", () => {
    const hits: string[] = [];
    for (const file of files) {
      const src = stripComments(readFileSync(file, "utf8")).toLowerCase();
      for (const token of FORBIDDEN) {
        if (src.includes(token.toLowerCase())) {
          hits.push(`${file} -> ${token}`);
        }
      }
    }
    expect(hits).toEqual([]);
  });
});
