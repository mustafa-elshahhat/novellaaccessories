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
import type { CustomerProfile } from "@/lib/api/types";

interface AuthState {
  customer: CustomerProfile | null;
  loading: boolean;
  refresh: () => Promise<CustomerProfile | null>;
  setCustomer: (customer: CustomerProfile | null) => void;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

/**
 * Holds the (non-sensitive) customer profile for client chrome (header, cart gating).
 * Auth state is hydrated from the BFF `/api/auth/me` so public pages stay statically cacheable.
 * The JWT itself never reaches the browser — it lives only in the HttpOnly cookie.
 */
export function AuthProvider({
  children,
  initialCustomer = null,
}: {
  children: ReactNode;
  initialCustomer?: CustomerProfile | null;
}) {
  const [customer, setCustomer] = useState<CustomerProfile | null>(initialCustomer);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    try {
      const res = await bff<{ customer: CustomerProfile }>("/api/auth/me");
      setCustomer(res.customer);
      return res.customer;
    } catch {
      setCustomer(null);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // Hydrate client chrome from the backend session on mount. State is set only after the
    // awaited fetch resolves (a genuine external-system sync), so the cascading-render warning
    // from react-hooks/set-state-in-effect does not apply here.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void refresh();
  }, [refresh]);

  const logout = useCallback(async () => {
    try {
      await bff("/api/auth/logout", { method: "POST" });
    } catch {
      // local logout proceeds regardless
    }
    setCustomer(null);
  }, []);

  return (
    <AuthContext.Provider value={{ customer, loading, refresh, setCustomer, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
