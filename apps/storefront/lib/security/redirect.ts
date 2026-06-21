/**
 * Returns a safe, same-origin relative path or the fallback. Prevents open-redirect attacks:
 * - must begin with exactly one "/"
 * - rejects protocol-relative "//host"
 * - rejects backslashes and control characters
 * - rejects scheme-bearing values (http:, https:, javascript:, data:) including encoded forms
 */
export function sanitizeReturnUrl(
  raw: string | null | undefined,
  fallback: string,
): string {
  if (!raw) return fallback;
  const value = raw.trim();

  if (value.length === 0) return fallback;
  if (!value.startsWith("/")) return fallback;
  if (value.startsWith("//")) return fallback;
  if (value.includes("\\")) return fallback;
  // reject any ASCII control character (0x00-0x1f)
  for (let i = 0; i < value.length; i++) {
    if (value.charCodeAt(i) < 0x20) return fallback;
  }

  let decoded = value;
  try {
    decoded = decodeURIComponent(value);
  } catch {
    return fallback;
  }
  const normalized = decoded.trim().toLowerCase();
  if (normalized.startsWith("//")) return fallback;
  if (/^[a-z][a-z0-9+.-]*:/.test(normalized)) return fallback; // any scheme

  return value;
}
