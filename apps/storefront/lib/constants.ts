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
  defaultSeoTitleAr: "نوفيلا أكسسوارات",
  defaultSeoTitleEn: "Novella Accessories",
  defaultSeoDescriptionAr: "إكسسوارات أنيقة بجودة عالية.",
  defaultSeoDescriptionEn: "Elegant, high-quality accessories.",
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
