import { describe, expect, it, vi } from "vitest";
import { api, apiFetch, setAccessToken } from "./client";
import { ApiError, sanitizeMessage } from "./errors";

describe("admin api client", () => {
  it("attaches bearer token centrally", async () => {
    setAccessToken("token-123");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200, headers: { "Content-Type": "application/json" } }));
    await api.get<{ ok: boolean }>("/api/admin/auth/me");
    const request = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(new Headers(request.headers).get("Authorization")).toBe("Bearer token-123");
  });

  it("rejects non-API paths", async () => {
    await expect(apiFetch("https://example.com/sidecar" as any)).rejects.toBeInstanceOf(ApiError);
  });

  it("rejects non-admin API paths", async () => {
    await expect(apiFetch("/api/public/home")).rejects.toMatchObject({ code: "ADMIN_API_ONLY" });
  });

  it("sanitizes raw provider/database messages", () => {
    expect(sanitizeMessage("SqlException at server path", 500)).toBe("The request could not be completed.");
  });
});
