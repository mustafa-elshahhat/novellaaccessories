import { absoluteUrl } from "./metadata";

/** Renders a JSON-LD script. `<` is escaped to prevent script-breakout. */
export function JsonLd({ data }: { data: object }) {
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{
        __html: JSON.stringify(data).replace(/</g, "\\u003c"),
      }}
    />
  );
}

export function organizationJsonLd(name: string) {
  return {
    "@context": "https://schema.org",
    "@type": "Organization",
    name,
    url: absoluteUrl("/"),
    logo: absoluteUrl("/icon.png"),
  };
}

export function websiteJsonLd(name: string) {
  return {
    "@context": "https://schema.org",
    "@type": "WebSite",
    name,
    url: absoluteUrl("/"),
    inLanguage: ["ar", "en"],
  };
}

export function breadcrumbJsonLd(items: Array<{ name: string; url: string }>) {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: item.url,
    })),
  };
}

/** schema.org availability from a customer-safe availability flag (no stock counts). */
export function availabilitySchema(isAvailable: boolean): string {
  return isAvailable
    ? "https://schema.org/InStock"
    : "https://schema.org/OutOfStock";
}

export function productJsonLd(opts: {
  name: string;
  description?: string | null;
  images: string[];
  url: string;
  price: number;
  isAvailable: boolean;
  sku?: string;
}) {
  return {
    "@context": "https://schema.org",
    "@type": "Product",
    name: opts.name,
    description: opts.description ?? undefined,
    image: opts.images.length ? opts.images : undefined,
    sku: opts.sku,
    brand: { "@type": "Brand", name: "Novella" },
    offers: {
      "@type": "Offer",
      url: opts.url,
      priceCurrency: "EGP",
      price: opts.price,
      availability: availabilitySchema(opts.isAvailable),
    },
  };
}

/** CollectionPage for a listing (categories, a category's products, or offers). */
export function collectionJsonLd(opts: {
  name: string;
  url: string;
  description?: string | null;
}) {
  return {
    "@context": "https://schema.org",
    "@type": "CollectionPage",
    name: opts.name,
    description: opts.description ?? undefined,
    url: opts.url,
  };
}

export function faqJsonLd(items: Array<{ question: string; answer: string }>) {
  return {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: items.map((item) => ({
      "@type": "Question",
      name: item.question,
      acceptedAnswer: { "@type": "Answer", text: item.answer },
    })),
  };
}
