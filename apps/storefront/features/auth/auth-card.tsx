import type { ReactNode } from "react";
import { Card } from "@/components/ui/card";

export function AuthCard({
  title,
  subtitle,
  children,
  footer,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="mx-auto flex w-full max-w-md flex-col px-4 py-10">
      <Card className="p-6 sm:p-8">
        <h1 className="text-2xl font-semibold text-deepbrown">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-taupe">{subtitle}</p>}
        <div className="mt-6">{children}</div>
      </Card>
      {footer && <div className="mt-4 text-center text-sm text-taupe">{footer}</div>}
    </div>
  );
}
