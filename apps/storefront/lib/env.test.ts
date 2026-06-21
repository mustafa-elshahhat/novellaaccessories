import { describe, it, expect } from "vitest";
import { publicEnv, getSiteHost } from "./env";

describe("publicEnv", () => {
  it("only carries client-safe public values (never the API base URL)", () => {
    expect(publicEnv).not.toHaveProperty("apiBaseUrl");
    // No NEXT_PUBLIC_API_BASE_URL key should leak in.
    expect(JSON.stringify(publicEnv).toLowerCase()).not.toContain("api_base_url");
  });

  it("defaults locales to ar,en with ar default", () => {
    expect(publicEnv.defaultLocale).toBe("ar");
    expect(publicEnv.supportedLocales).toContain("ar");
    expect(publicEnv.supportedLocales).toContain("en");
  });
});

describe("getSiteHost", () => {
  it("returns the host of the configured site URL", () => {
    // Default in test env is http://localhost:3000.
    expect(getSiteHost()).toBe("localhost:3000");
  });
});
