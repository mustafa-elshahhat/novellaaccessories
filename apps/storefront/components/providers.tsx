"use client";

import type { ReactNode } from "react";
import { AuthProvider } from "@/features/auth/auth-provider";
import { CartProvider } from "@/features/cart/cart-provider";
import { ToastProvider } from "@/components/ui/toast";
import { AnalyticsProvider } from "@/features/analytics/analytics-provider";

export function Providers({ children }: { children: ReactNode }) {
  return (
    <AuthProvider>
      <CartProvider>
        <ToastProvider>
          <AnalyticsProvider>{children}</AnalyticsProvider>
        </ToastProvider>
      </CartProvider>
    </AuthProvider>
  );
}
