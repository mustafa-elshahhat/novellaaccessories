import { describe, it, expect } from "vitest";
import {
  ApiError,
  mapStatusToCode,
  errorTranslationKey,
  KNOWN_ERROR_CODES,
} from "./errors";

describe("ApiError.from", () => {
  it("passes ApiError instances through", () => {
    const e = new ApiError("OTP_EXPIRED", "expired", 400);
    expect(ApiError.from(e)).toBe(e);
  });

  it("wraps unknown errors as INTERNAL_ERROR 500", () => {
    const e = ApiError.from(new Error("boom"));
    expect(e.code).toBe("INTERNAL_ERROR");
    expect(e.status).toBe(500);
  });
});

describe("ApiError.fieldErrors", () => {
  it("normalizes string[] and string detail values", () => {
    const e = new ApiError("VALIDATION_ERROR", "invalid", 400, {
      phoneNumber: ["required"],
      password: "too short",
    });
    expect(e.fieldErrors()).toEqual({
      phoneNumber: ["required"],
      password: ["too short"],
    });
  });

  it("returns empty object when no details", () => {
    expect(new ApiError("NOT_FOUND", "x", 404).fieldErrors()).toEqual({});
  });
});

describe("mapStatusToCode", () => {
  it("maps known statuses", () => {
    expect(mapStatusToCode(400)).toBe("VALIDATION_ERROR");
    expect(mapStatusToCode(401)).toBe("UNAUTHORIZED");
    expect(mapStatusToCode(403)).toBe("FORBIDDEN");
    expect(mapStatusToCode(404)).toBe("NOT_FOUND");
    expect(mapStatusToCode(409)).toBe("CONFLICT");
    expect(mapStatusToCode(503)).toBe("INTERNAL_ERROR");
  });
});

describe("errorTranslationKey", () => {
  it("returns the code when known", () => {
    for (const code of KNOWN_ERROR_CODES) {
      expect(errorTranslationKey(code)).toBe(code);
    }
  });

  it("returns 'generic' for unknown codes", () => {
    expect(errorTranslationKey("SOME_NEW_CODE")).toBe("generic");
  });
});
