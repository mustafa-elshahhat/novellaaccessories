import { cn } from "@/lib/utils/cn";

/**
 * Tasteful branded placeholder shown only when an image is genuinely missing. Renders the Novella
 * monogram on a warm cream/ivory field with a fine champagne ring — never a bare "novella" text box.
 * Decorative (aria-hidden): the surrounding card/gallery already provides the accessible name.
 */
export function ImagePlaceholder({ className }: { className?: string }) {
  return (
    <div
      aria-hidden
      className={cn(
        "flex h-full w-full items-center justify-center bg-gradient-to-b from-cream to-ivory",
        className,
      )}
    >
      <svg viewBox="0 0 120 120" className="h-2/5 max-h-28 w-2/5 max-w-28" role="presentation">
        <circle cx="60" cy="60" r="46" fill="none" stroke="#D7B08A" strokeWidth="1.5" opacity="0.7" />
        <circle cx="60" cy="60" r="40" fill="none" stroke="#C79A72" strokeWidth="0.75" opacity="0.4" />
        {/* N monogram (geometric, font-independent) */}
        <g fill="#B98563">
          <rect x="44.6" y="38" width="7" height="44" rx="2" />
          <rect x="68.4" y="38" width="7" height="44" rx="2" />
          <path d="M44.6 38 L52 38 L75.4 82 L68 82 Z" />
        </g>
        {/* small sparkle accent */}
        <path
          d="M60 20 C61 26 62 27 68 28 C62 29 61 30 60 36 C59 30 58 29 52 28 C58 27 59 26 60 20 Z"
          fill="#C79A72"
          opacity="0.8"
        />
      </svg>
    </div>
  );
}
