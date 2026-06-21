import type { ReactNode } from "react";
import { publicEnv } from "@/lib/env";
import { WhatsAppIcon } from "@/components/icons";
import { cn } from "@/lib/utils/cn";

/**
 * Public WhatsApp support link. Uses NEXT_PUBLIC_WHATSAPP_SUPPORT_URL (a wa.me link),
 * never the internal WhatsApp sidecar. Renders nothing when unconfigured.
 */
export function WhatsAppLink({
  children,
  className,
  showIcon = true,
}: {
  children: ReactNode;
  className?: string;
  showIcon?: boolean;
}) {
  const url = publicEnv.whatsappSupportUrl;
  if (!url) return null;
  return (
    <a
      href={url}
      target="_blank"
      rel="noopener noreferrer"
      className={cn("inline-flex items-center gap-2", className)}
    >
      {showIcon && <WhatsAppIcon className="h-5 w-5" />}
      <span>{children}</span>
    </a>
  );
}
