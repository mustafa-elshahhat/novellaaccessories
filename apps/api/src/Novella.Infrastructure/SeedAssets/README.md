# Seed Assets (development imagery)

These WebP files are **original, project-owned development placeholders** for the hero, the four
default categories, and the development product catalog. They are built from vector shapes only (no
third-party photography, no hotlinked URLs) in the Novella palette (warm ivory / champagne / rose-gold /
mocha — see `docs/08_BRAND_UI_GUIDELINES.md`).

Purpose: verify storefront layouts, image galleries, responsive behavior, and the Cloudinary upload
flow without leaving blank image boxes. **They are temporary and fully replaceable from the admin
dashboard** (category/hero/product image management). Real product photography should replace them
before launch.

## Layout

```
SeedAssets/
  heroes/hero-1.webp
  categories/{rings,necklaces,earrings,bracelets}.webp
  products/<product-slug>.webp        # one per development product (+ luna-ring-2 for a gallery)
```

## How they are used

`DataSeeder.SeedDevelopmentMediaAsync` reads these from `AppContext.BaseDirectory/SeedAssets` and uploads
them to Cloudinary (folders `novella/dev/{heroes|categories|products}`) **only** when
`Seed:EnableDevelopmentCatalog` is true and Cloudinary is configured. The secure URL + public id + alt
text are then persisted. The upload is idempotent: an entity that already has an image is skipped.

## Regenerating

```
cd apps/storefront        # provides the `sharp` dependency
node ../api/src/Novella.Infrastructure/SeedAssets/generate-assets.mjs
```

Only `*.webp` files are copied into the build output; `generate-assets.mjs` is a build-time tool and is
not shipped.
