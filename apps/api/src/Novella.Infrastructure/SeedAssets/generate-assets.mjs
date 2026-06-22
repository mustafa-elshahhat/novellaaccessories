// Development seed-image generator for Novella Accessories.
//
// Produces tasteful, brand-consistent placeholder imagery (warm ivory / champagne / rose-gold /
// mocha per docs/08_BRAND_UI_GUIDELINES.md) for the hero, the four default categories, and the
// development product catalog. These are ORIGINAL, project-owned development assets (no third-party
// photography, no hotlinking) intended only to verify layouts, galleries, responsive behavior and the
// Cloudinary upload flow. They are fully replaceable from the admin dashboard.
//
// Art is built from vector shapes only (no <text>) so rasterization does not depend on system fonts.
//
// Usage (run from apps/storefront so `sharp` resolves):
//   node ../api/src/Novella.Infrastructure/SeedAssets/generate-assets.mjs
//
import { mkdir, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { createRequire } from "node:module";

const ROOT = dirname(fileURLToPath(import.meta.url));

// `sharp` lives in apps/storefront/node_modules; resolve it from there so this build-only generator
// does not require its own install. SeedAssets -> Novella.Infrastructure -> src -> api -> apps.
const require = createRequire(join(ROOT, "..", "..", "..", "..", "storefront", "package.json"));
const sharp = require("sharp");

const C = {
  ivory: "#F4E8DA",
  cream: "#FFF7EF",
  champagne: "#C79A72",
  rose: "#B98563",
  bronze: "#9C6B4F",
  mocha: "#5E4439",
  taupe: "#8A7368",
  border: "#D7B08A",
  shadow: "#E8D6C4",
  deep: "#3F2D27",
};

// ---- vector motif helpers (all stroke-based, gold/mocha) -------------------

function sparkle(cx, cy, s, color, opacity = 0.8) {
  return `<path d="M${cx} ${cy - s} C ${cx + s * 0.18} ${cy - s * 0.18} ${cx + s * 0.18} ${cy - s * 0.18} ${cx + s} ${cy}
    C ${cx + s * 0.18} ${cy + s * 0.18} ${cx + s * 0.18} ${cy + s * 0.18} ${cx} ${cy + s}
    C ${cx - s * 0.18} ${cy + s * 0.18} ${cx - s * 0.18} ${cy + s * 0.18} ${cx - s} ${cy}
    C ${cx - s * 0.18} ${cy - s * 0.18} ${cx - s * 0.18} ${cy - s * 0.18} ${cx} ${cy - s} Z"
    fill="${color}" opacity="${opacity}"/>`;
}

function leaf(cx, cy, s, color, rot = 0, opacity = 0.5) {
  return `<g transform="translate(${cx} ${cy}) rotate(${rot})" opacity="${opacity}">
    <path d="M0 ${-s} C ${s * 0.7} ${-s * 0.3} ${s * 0.7} ${s * 0.3} 0 ${s}
      C ${-s * 0.7} ${s * 0.3} ${-s * 0.7} ${-s * 0.3} 0 ${-s} Z" fill="none" stroke="${color}" stroke-width="2"/>
    <line x1="0" y1="${-s * 0.8}" x2="0" y2="${s * 0.8}" stroke="${color}" stroke-width="1.2"/>
  </g>`;
}

function monogramN(cx, cy, h, color) {
  const w = h * 0.7, t = h * 0.16;
  const x = cx - w / 2, y = cy - h / 2;
  return `<g fill="${color}">
    <rect x="${x}" y="${y}" width="${t}" height="${h}" rx="${t * 0.3}"/>
    <rect x="${x + w - t}" y="${y}" width="${t}" height="${h}" rx="${t * 0.3}"/>
    <path d="M${x} ${y} L${x + t * 1.05} ${y} L${x + w} ${y + h} L${x + w - t * 1.05} ${y + h} Z"/>
  </g>`;
}

// jewellery silhouettes (thin gold strokes) keyed by category
function ringMotif(cx, cy, r, accent) {
  return `<g fill="none" stroke="${accent}" stroke-width="6">
    <circle cx="${cx}" cy="${cy + r * 0.25}" r="${r}"/>
    ${sparkle(cx, cy - r * 0.95, r * 0.34, accent, 1)}
    <circle cx="${cx}" cy="${cy - r * 0.95}" r="${r * 0.34}" stroke-width="3"/>
  </g>`;
}
function necklaceMotif(cx, cy, r, accent) {
  return `<g fill="none" stroke="${accent}" stroke-width="5">
    <path d="M${cx - r * 1.3} ${cy - r} C ${cx - r * 0.4} ${cy + r * 1.1} ${cx + r * 0.4} ${cy + r * 1.1} ${cx + r * 1.3} ${cy - r}"/>
    <path d="M${cx} ${cy + r * 0.95} C ${cx + r * 0.5} ${cy + r * 0.95} ${cx + r * 0.5} ${cy + r * 1.8} ${cx} ${cy + r * 1.9}
      C ${cx - r * 0.5} ${cy + r * 1.8} ${cx - r * 0.5} ${cy + r * 0.95} ${cx} ${cy + r * 0.95} Z" fill="${accent}" opacity="0.18"/>
  </g>`;
}
function earringsMotif(cx, cy, r, accent) {
  const one = (ox) => `
    <line x1="${ox}" y1="${cy - r}" x2="${ox}" y2="${cy - r * 0.2}" />
    <circle cx="${ox}" cy="${cy + r * 0.4}" r="${r * 0.55}" />
    ${sparkle(ox, cy + r * 0.4, r * 0.22, accent, 1)}`;
  return `<g fill="none" stroke="${accent}" stroke-width="5">
    ${one(cx - r * 0.7)}${one(cx + r * 0.7)}
  </g>`;
}
function braceletMotif(cx, cy, r, accent) {
  return `<g fill="none" stroke="${accent}" stroke-width="6">
    <ellipse cx="${cx}" cy="${cy}" rx="${r * 1.25}" ry="${r * 0.85}"/>
    <ellipse cx="${cx}" cy="${cy}" rx="${r * 0.95}" ry="${r * 0.6}" stroke-width="2" opacity="0.6"/>
    ${sparkle(cx, cy - r * 0.85, r * 0.3, accent, 1)}
  </g>`;
}

const MOTIF = {
  rings: ringMotif,
  necklaces: necklaceMotif,
  earrings: earringsMotif,
  bracelets: braceletMotif,
};

function frame(w, h) {
  const inset = Math.round(Math.min(w, h) * 0.045);
  return `<rect x="${inset}" y="${inset}" width="${w - inset * 2}" height="${h - inset * 2}"
    rx="${inset}" fill="none" stroke="${C.border}" stroke-width="2" opacity="0.8"/>`;
}

function background(w, h) {
  return `
    <defs>
      <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0%" stop-color="${C.cream}"/>
        <stop offset="100%" stop-color="${C.ivory}"/>
      </linearGradient>
      <radialGradient id="glow" cx="50%" cy="38%" r="65%">
        <stop offset="0%" stop-color="#FFFFFF" stop-opacity="0.5"/>
        <stop offset="100%" stop-color="#FFFFFF" stop-opacity="0"/>
      </radialGradient>
    </defs>
    <rect width="${w}" height="${h}" fill="url(#bg)"/>
    <rect width="${w}" height="${h}" fill="url(#glow)"/>`;
}

function decor(w, h) {
  return `
    ${leaf(w * 0.12, h * 0.2, Math.min(w, h) * 0.07, C.rose, -25, 0.4)}
    ${leaf(w * 0.88, h * 0.82, Math.min(w, h) * 0.08, C.champagne, 150, 0.35)}
    ${sparkle(w * 0.2, h * 0.78, Math.min(w, h) * 0.018, C.champagne, 0.7)}
    ${sparkle(w * 0.82, h * 0.22, Math.min(w, h) * 0.022, C.rose, 0.7)}
    ${sparkle(w * 0.7, h * 0.7, Math.min(w, h) * 0.014, C.bronze, 0.6)}`;
}

// ---- composers -------------------------------------------------------------

function productSvg(w, h, category, accent) {
  const cx = w / 2, cy = h * 0.46, r = Math.min(w, h) * 0.18;
  const motif = MOTIF[category] ?? ringMotif;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${background(w, h)}${frame(w, h)}${decor(w, h)}
    <circle cx="${cx}" cy="${cy}" r="${r * 2.1}" fill="none" stroke="${C.border}" stroke-width="1.5" opacity="0.55"/>
    ${motif(cx, cy, r, accent)}
    ${monogramN(cx, h * 0.85, h * 0.075, C.mocha)}
  </svg>`;
}

function categorySvg(w, h, category, accent) {
  const cx = w / 2, cy = h * 0.44, r = Math.min(w, h) * 0.2;
  const motif = MOTIF[category] ?? ringMotif;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${background(w, h)}${frame(w, h)}${decor(w, h)}
    ${motif(cx, cy, r, accent)}
    <line x1="${w * 0.4}" y1="${h * 0.74}" x2="${w * 0.6}" y2="${h * 0.74}" stroke="${C.champagne}" stroke-width="2"/>
    ${monogramN(cx, h * 0.85, h * 0.08, C.mocha)}
  </svg>`;
}

function heroSvg(w, h) {
  const cx = w / 2, cy = h * 0.42;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    ${background(w, h)}${frame(w, h)}
    ${leaf(w * 0.08, h * 0.3, h * 0.16, C.rose, -20, 0.35)}
    ${leaf(w * 0.92, h * 0.7, h * 0.18, C.champagne, 160, 0.3)}
    ${leaf(w * 0.16, h * 0.78, h * 0.1, C.champagne, 30, 0.3)}
    <circle cx="${cx}" cy="${cy}" r="${h * 0.27}" fill="none" stroke="${C.border}" stroke-width="2" opacity="0.6"/>
    <circle cx="${cx}" cy="${cy}" r="${h * 0.31}" fill="none" stroke="${C.champagne}" stroke-width="1" opacity="0.4"/>
    ${ringMotif(cx, cy, h * 0.11, C.rose)}
    ${monogramN(cx, h * 0.75, h * 0.16, C.mocha)}
    <line x1="${cx - w * 0.07}" y1="${h * 0.86}" x2="${cx + w * 0.07}" y2="${h * 0.86}" stroke="${C.champagne}" stroke-width="2"/>
    ${sparkle(w * 0.3, h * 0.2, h * 0.02, C.champagne, 0.8)}
    ${sparkle(w * 0.7, h * 0.24, h * 0.025, C.rose, 0.8)}
    ${sparkle(w * 0.62, h * 0.62, h * 0.016, C.bronze, 0.7)}
  </svg>`;
}

// accent per product slug so the two products in a category look distinct
const PRODUCTS = [
  { slug: "luna-ring", category: "rings", accent: C.rose },
  { slug: "luna-ring-2", category: "rings", accent: C.champagne },
  { slug: "aurora-ring", category: "rings", accent: C.bronze },
  { slug: "story-necklace", category: "necklaces", accent: C.rose },
  { slug: "grace-necklace", category: "necklaces", accent: C.champagne },
  { slug: "nova-earrings", category: "earrings", accent: C.rose },
  { slug: "petal-earrings", category: "earrings", accent: C.bronze },
  { slug: "rose-bracelet", category: "bracelets", accent: C.rose },
  { slug: "aurelia-bracelet", category: "bracelets", accent: C.champagne },
];

const CATEGORIES = [
  { slug: "rings", accent: C.rose },
  { slug: "necklaces", accent: C.bronze },
  { slug: "earrings", accent: C.rose },
  { slug: "bracelets", accent: C.champagne },
];

async function render(svg, outPath) {
  const buf = await sharp(Buffer.from(svg)).webp({ quality: 82 }).toBuffer();
  await writeFile(outPath, buf);
  return buf.length;
}

async function main() {
  const dirs = ["heroes", "categories", "products"];
  for (const d of dirs) await mkdir(join(ROOT, d), { recursive: true });

  let count = 0;
  const heroBytes = await render(heroSvg(1600, 900), join(ROOT, "heroes", "hero-1.webp"));
  console.log(`heroes/hero-1.webp (${heroBytes} bytes)`);
  count++;

  for (const c of CATEGORIES) {
    const b = await render(categorySvg(800, 600, c.slug, c.accent), join(ROOT, "categories", `${c.slug}.webp`));
    console.log(`categories/${c.slug}.webp (${b} bytes)`);
    count++;
  }

  for (const p of PRODUCTS) {
    const b = await render(productSvg(1000, 1000, p.category, p.accent), join(ROOT, "products", `${p.slug}.webp`));
    console.log(`products/${p.slug}.webp (${b} bytes)`);
    count++;
  }

  console.log(`\nGenerated ${count} development WebP assets under SeedAssets/.`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
