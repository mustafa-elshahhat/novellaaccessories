import DOMPurify from "isomorphic-dompurify";
import { cn } from "@/lib/utils/cn";

/** Renders trusted-but-sanitized backend HTML. Never pass unsanitized content here. */
export function SafeHtml({ html, className }: { html: string; className?: string }) {
  const clean = DOMPurify.sanitize(html, { USE_PROFILES: { html: true } });
  return (
    <div
      className={cn(
        "prose-novella max-w-none leading-relaxed text-mocha [&_a]:text-bronze [&_a]:underline [&_h2]:mt-6 [&_h2]:mb-2 [&_h2]:text-lg [&_h2]:font-semibold [&_h2]:text-deepbrown [&_li]:my-1 [&_p]:my-3 [&_ul]:list-disc [&_ul]:ps-6",
        className,
      )}
      dangerouslySetInnerHTML={{ __html: clean }}
    />
  );
}
