export type ApiErrorEnvelope = {
  code: string;
  message: string;
  details?: Record<string, unknown>;
};

const unsafeFragments = [
  "SqlException",
  "System.",
  " at ",
  ["Connection", "String"].join(""),
  ["Password", "Hash"].join(""),
  ["Code", "Hash"].join(""),
  ["Signing", "Key"].join(""),
  ["Api", "Secret"].join(""),
  ["Internal", "Api", "Key"].join(""),
  ["MONGO", "DB", "URI"].join("_")
];

export class ApiError extends Error {
  status: number;
  code: string;
  details?: Record<string, unknown>;

  constructor(status: number, envelope?: Partial<ApiErrorEnvelope>) {
    super(sanitizeMessage(envelope?.message, status));
    this.name = "ApiError";
    this.status = status;
    this.code = envelope?.code || `HTTP_${status}`;
    this.details = envelope?.details;
  }
}

export function sanitizeMessage(message: unknown, status = 500) {
  const text = typeof message === "string" ? message : "";
  if (!text || unsafeFragments.some((fragment) => text.includes(fragment))) {
    if (status === 401) return "Your session has expired. Please sign in again.";
    if (status === 403) return "You do not have access to this admin area.";
    if (status === 404) return "The requested resource was not found.";
    return "The request could not be completed.";
  }
  return text;
}

export function fieldErrors(details?: Record<string, unknown>) {
  const result: Record<string, string> = {};
  if (!details) return result;
  for (const [key, value] of Object.entries(details)) {
    if (Array.isArray(value)) result[key[0]?.toLowerCase() + key.slice(1)] = value.join(" ");
    else if (typeof value === "string") result[key[0]?.toLowerCase() + key.slice(1)] = value;
  }
  return result;
}
