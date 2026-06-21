export type ClassValue = string | false | null | undefined;

/** Minimal className combiner (avoids pulling in clsx/tailwind-merge). */
export function cn(...classes: ClassValue[]): string {
  return classes.filter(Boolean).join(" ");
}
