# SEO / AEO / GEO Plan — Novella Accessories

## 1. Goal

Prepare the storefront and backend content model for search engines, answer engines, and generative AI systems.

Definitions in this project:

- SEO: Search Engine Optimization for Google and traditional search.
- AEO: Answer Engine Optimization for direct answers, snippets, and FAQ-style responses.
- GEO: Generative Engine Optimization for AI systems that summarize stores, products, and brand information.

## 2. Supported Languages

- Arabic.
- English.

Each indexed page should have localized metadata.

## 3. Required SEO Fields

For products:

- Meta title Arabic.
- Meta title English.
- Meta description Arabic.
- Meta description English.
- Slug Arabic.
- Slug English.
- Open Graph title.
- Open Graph description.
- Image alt Arabic.
- Image alt English.

For categories:

- Meta title Arabic.
- Meta title English.
- Meta description Arabic.
- Meta description English.
- Slug Arabic.
- Slug English.
- AEO summary.
- GEO content.

For static pages:

- Title.
- Slug.
- Meta title.
- Meta description.
- Content.
- AEO summary.
- GEO content.

## 4. URL Structure

Suggested:

```text
/ar
/en
/ar/category/{slug}
/en/category/{slug}
/ar/product/{slug}
/en/product/{slug}
/ar/page/{slug}
/en/page/{slug}
```

## 5. Technical SEO

Required:

- Server-rendered metadata.
- Canonical URLs.
- hreflang for Arabic and English.
- robots.txt.
- sitemap.xml.
- Clean localized slugs.
- Optimized images.
- Fast mobile performance.
- 404 page.
- No indexing of cart, checkout, account, admin pages.

## 6. Structured Data

Required schema types:

- Organization.
- WebSite.
- BreadcrumbList.
- Product.
- Offer.
- FAQPage.
- CollectionPage for categories.

Product structured data should include:

- Name.
- Description.
- Image.
- Brand.
- SKU if applicable.
- Availability.
- Price.
- Currency.

Do not expose purchase price in structured data.

## 7. AEO Requirements

Answer blocks should exist for:

- Product guidance.
- Category guidance.
- Shipping questions.
- Returns/exchange questions.
- Payment questions.
- Brand story questions.

Example product AEO questions:

- What is this piece suitable for?
- Is it available for delivery in Egypt?
- How can I order it?
- How do returns work?

## 8. GEO Requirements

Generative AI systems should understand:

- What Novella sells.
- Which categories exist.
- How to order.
- Which payment methods are supported/planned.
- How shipping works.
- How returns are handled.
- Brand tone and uniqueness.

GEO content should be clear, factual, and not keyword-stuffed.

## 9. Static Pages Required

- About Us.
- Contact Us.
- Privacy Policy.
- Terms and Conditions.
- Return and Exchange Policy.
- Shipping and Delivery Policy.
- FAQ.

## 10. Product Content Template

Each product should support:

```text
Short description
Detailed description
Material/care notes
Styling suggestion
Delivery note
Return/exchange note
FAQ/answer block
```

## 11. Category Content Template

Each category should support:

```text
Intro paragraph
Who this category is for
How to choose
Featured materials/styles
Delivery note
FAQ/answer block
```

## 12. Robots and Indexing

Index:

- Home.
- Categories.
- Products.
- Static content pages.

Noindex:

- Cart.
- Checkout.
- Account.
- Login/register.
- Admin.
- API routes.

## 13. Sitemap

Sitemap should include:

- Home per locale.
- Active categories per locale.
- Active products per locale.
- Active static pages per locale.

## 14. Performance SEO

- Use compressed images.
- Use proper image dimensions.
- Avoid layout shift.
- Optimize mobile-first.
- Keep JS bundle under control.
- Use caching for public product/category pages where safe.

## 15. Admin SEO Workflow

Admin should be able to edit SEO/AEO/GEO fields for:

- Products.
- Categories.
- Static pages.
- Home page.

Suggested helper:

- Show character count for meta titles/descriptions.
- Preview search result snippet.
- Warn when metadata is missing.
