"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { bff } from "@/lib/api/bff-client";
import type { Cart } from "@/lib/api/types";
import { useAuth } from "@/features/auth/auth-provider";

interface CartState {
  cart: Cart | null;
  count: number;
  loading: boolean;
  refresh: () => Promise<void>;
  setCart: (cart: Cart | null) => void;
}

const CartContext = createContext<CartState | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const { customer, loading: authLoading } = useAuth();
  const [cart, setCart] = useState<Cart | null>(null);
  const [loading, setLoading] = useState(false);

  const refresh = useCallback(async () => {
    if (!customer) {
      setCart(null);
      return;
    }
    setLoading(true);
    try {
      const data = await bff<Cart>("/api/cart");
      setCart(data);
    } catch {
      // keep prior cart on transient failure
    } finally {
      setLoading(false);
    }
  }, [customer]);

  useEffect(() => {
    if (!authLoading) {
      // Sync the cart from the backend once auth settles. State is set only after the awaited
      // fetch resolves (external-system sync), so set-state-in-effect does not apply here.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      void refresh();
    }
  }, [authLoading, refresh]);

  const count = cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;

  return (
    <CartContext.Provider value={{ cart, count, loading, refresh, setCart }}>
      {children}
    </CartContext.Provider>
  );
}

export function useCart(): CartState {
  const ctx = useContext(CartContext);
  if (!ctx) {
    throw new Error("useCart must be used within a CartProvider");
  }
  return ctx;
}
