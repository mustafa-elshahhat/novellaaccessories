/**
 * Shared, isomorphic error model. Safe to import from both server and client code
 * (no server-only imports here).
 */

export const KNOWN_ERROR_CODES = [
  "VALIDATION_ERROR",
  "NOT_FOUND",
  "UNAUTHORIZED",
  "FORBIDDEN",
  "CONFLICT",
  "INTERNAL_ERROR",
  "AUTH_INVALID_CREDENTIALS",
  "OTP_EXPIRED",
  "OTP_INVALID",
  "OTP_LOCKED",
  "OTP_RESEND_COOLDOWN",
  "OTP_RESEND_LIMIT_REACHED",
  "PHONE_ALREADY_USED",
  "PHONE_NOT_VERIFIED",
  "PRODUCT_UNAVAILABLE",
  "VARIANT_OUT_OF_STOCK",
  "COUPON_INVALID",
  "COUPON_EXPIRED",
  "COUPON_USAGE_LIMIT_REACHED",
  "COUPON_MIN_SUBTOTAL_NOT_MET",
  "ORDER_CANNOT_BE_CANCELLED",
  "ORDER_INVALID_STATUS_TRANSITION",
  "SHIPPING_GOVERNORATE_INACTIVE",
  "PAYMENT_PROVIDER_NOT_ACTIVE",
] as const;

export type FieldErrors = Record<string, string[]>;

export class ApiError extends Error {
  readonly code: string;
  readonly status: number;
  readonly details?: Record<string, unknown>;

  constructor(
    code: string,
    message: string,
    status: number,
    details?: Record<string, unknown>,
  ) {
    super(message);
    this.name = "ApiError";
    this.code = code;
    this.status = status;
    this.details = details;
  }

  static from(error: unknown): ApiError {
    if (error instanceof ApiError) return error;
    return new ApiError("INTERNAL_ERROR", "An unexpected error occurred.", 500);
  }

  /** Per-field validation errors, when present (code === VALIDATION_ERROR). */
  fieldErrors(): FieldErrors {
    const details = this.details;
    if (!details) return {};
    const out: FieldErrors = {};
    for (const [key, value] of Object.entries(details)) {
      if (Array.isArray(value)) {
        out[key] = value.map((v) => String(v));
      } else if (typeof value === "string") {
        out[key] = [value];
      }
    }
    return out;
  }
}

export function mapStatusToCode(status: number): string {
  switch (status) {
    case 400:
      return "VALIDATION_ERROR";
    case 401:
      return "UNAUTHORIZED";
    case 403:
      return "FORBIDDEN";
    case 404:
      return "NOT_FOUND";
    case 409:
      return "CONFLICT";
    default:
      return status >= 500 ? "INTERNAL_ERROR" : "INTERNAL_ERROR";
  }
}

/** Translation key (within the `errors` namespace) for a backend error code. */
export function errorTranslationKey(code: string): string {
  return (KNOWN_ERROR_CODES as readonly string[]).includes(code) ? code : "generic";
}
