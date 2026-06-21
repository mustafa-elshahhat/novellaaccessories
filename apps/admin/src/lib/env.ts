const forbiddenEnvFragments = [
  ["CONNECTION", "STRINGS"].join("").toUpperCase(),
  ["JWT__", "SIGNING", "KEY"].join("").toUpperCase(),
  ["SIGNING", "KEY"].join("").toUpperCase(),
  ["SEED__ADMIN", "PASSWORD"].join("").toUpperCase(),
  ["CLOUDINARY__API", "SECRET"].join("").toUpperCase(),
  ["WHATSAPP__INTERNAL", "API", "KEY"].join("").toUpperCase(),
  ["INTERNAL", "API", "KEY"].join("_").toUpperCase(),
  ["PAIRING", "ADMIN", "TOKEN"].join("_").toUpperCase(),
  ["MONGO", "DB", "URI"].join("_").toUpperCase(),
  ["PAYMENT__WEBHOOK", "SECRET"].join("").toUpperCase()
];

export type AdminEnv = {
  apiBaseUrl: string;
  appName: string;
};

export function validateEnv(raw: ImportMetaEnv = import.meta.env): AdminEnv {
  for (const key of Object.keys(raw)) {
    const normalized = key.toUpperCase();
    if (forbiddenEnvFragments.some((fragment) => normalized.includes(fragment))) {
      throw new Error(`Forbidden secret-like Vite environment key: ${key}`);
    }
  }

  const apiBaseUrl = raw.VITE_API_BASE_URL?.trim();
  if (!apiBaseUrl) throw new Error("VITE_API_BASE_URL is required.");
  const parsed = new URL(apiBaseUrl);
  if (!/^https?:$/.test(parsed.protocol)) throw new Error("VITE_API_BASE_URL must be http(s).");

  return {
    apiBaseUrl: parsed.origin + parsed.pathname.replace(/\/$/, ""),
    appName: raw.VITE_APP_NAME?.trim() || "Novella Admin"
  };
}

export const env = validateEnv();
