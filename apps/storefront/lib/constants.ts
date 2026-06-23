/** Static page slugs (match backend seed: slug === key for both locales). */
export const PAGE_SLUGS = {
  about: "about",
  contact: "contact",
  privacy: "privacy",
  terms: "terms",
  returns: "returns",
  shipping: "shipping",
} as const;

export type PageSlug = (typeof PAGE_SLUGS)[keyof typeof PAGE_SLUGS];

export const BRAND = {
  nameAr: "نوفيلا أكسسوارات",
  nameEn: "Novella Accessories",
  // Visible brand tagline. Drives the home page's automatically generated description.
  taglineAr: "إكسسوارات أنيقة بجودة عالية.",
  taglineEn: "Elegant, high-quality accessories.",
} as const;

/** Routes that must never be indexed (transactional / auth / account). */
export const NOINDEX_PREFIXES = [
  "/cart",
  "/checkout",
  "/account",
  "/login",
  "/register",
  "/verify-phone",
  "/forgot-password",
  "/change-phone",
  "/order-success",
] as const;
