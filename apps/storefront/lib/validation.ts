/** Light client-side validation. The backend remains authoritative. */

export function validatePhone(value: string): boolean {
  const v = value.replace(/[\s-]/g, "");
  // Egyptian mobile: 01[0,1,2,5] + 8 digits, with optional +20 / 20 / leading 0.
  return /^(?:\+?20)?01[0125]\d{8}$/.test(v);
}

export function validatePasswordLength(value: string): boolean {
  return value.length >= 6;
}

export function isOtpComplete(value: string, length = 6): boolean {
  return new RegExp(`^\\d{${length}}$`).test(value);
}
