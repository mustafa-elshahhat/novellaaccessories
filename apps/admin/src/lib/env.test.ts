import { describe, expect, it } from "vitest";
import { validateEnv } from "./env";

describe("admin env validation", () => {
  it("accepts only public API and app name variables", () => {
    const env = validateEnv({ VITE_API_BASE_URL: "https://api.example.com/", VITE_APP_NAME: "Novella Admin" } as any);
    expect(env.apiBaseUrl).toBe("https://api.example.com");
    expect(env.appName).toBe("Novella Admin");
  });

  it("rejects secret-like Vite keys", () => {
    expect(() => validateEnv({ VITE_API_BASE_URL: "https://api.example.com", VITE_Jwt__SigningKey: "secret" } as any)).toThrow(/Forbidden/);
  });
});
