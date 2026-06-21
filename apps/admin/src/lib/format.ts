export const egp = new Intl.NumberFormat("en-EG", { style: "currency", currency: "EGP" });
export const integer = new Intl.NumberFormat("en-EG");
export const percent = new Intl.NumberFormat("en-EG", { style: "percent", maximumFractionDigits: 2 });

export function formatDate(value?: string | null) {
  if (!value) return "Not set";
  return new Intl.DateTimeFormat("en-EG", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function safeText(value: unknown) {
  if (value === null || value === undefined || value === "") return "-";
  return String(value);
}

export function enumOptions<T extends readonly string[]>(values: T) {
  return values.map((value) => ({ label: value.replace(/([A-Z])/g, " $1").trim(), value }));
}
