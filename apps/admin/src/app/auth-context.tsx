import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "@/lib/api/auth";
import { onForbidden, onUnauthorized, setAccessToken } from "@/lib/api/client";
import type { AdminProfile } from "@/lib/api/types";

const storageKey = "novella.admin.sessionToken";

type AuthState = {
  admin: AdminProfile | null;
  status: "loading" | "authenticated" | "anonymous" | "forbidden";
  message: string | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  clearMessage: () => void;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [admin, setAdmin] = useState<AdminProfile | null>(null);
  const [status, setStatus] = useState<AuthState["status"]>(() => sessionStorage.getItem(storageKey) ? "loading" : "anonymous");
  const [message, setMessage] = useState<string | null>(null);

  const clearAuth = useCallback((reason?: string) => {
    setAccessToken(null);
    sessionStorage.removeItem(storageKey);
    setAdmin(null);
    setStatus("anonymous");
    void queryClient.clear();
    if (reason) setMessage(reason);
  }, [queryClient]);

  useEffect(() => {
    onUnauthorized(() => clearAuth("Your session has expired. Please sign in again."));
    onForbidden(() => setStatus("forbidden"));
  }, [clearAuth]);

  useEffect(() => {
    let cancelled = false;
    const token = sessionStorage.getItem(storageKey);
    if (!token) return;
    setAccessToken(token);
    authApi.me()
      .then((profile) => {
        if (cancelled) return;
        setAdmin(profile);
        setStatus("authenticated");
      })
      .catch(() => {
        if (!cancelled) clearAuth("Your session has expired. Please sign in again.");
      });
    return () => {
      cancelled = true;
    };
  }, [clearAuth]);

  const value = useMemo<AuthState>(() => ({
    admin,
    status,
    message,
    async login(username, password) {
      const result = await authApi.login({ username, password });
      setAccessToken(result.token);
      sessionStorage.setItem(storageKey, result.token);
      const profile = await authApi.me();
      setAdmin(profile);
      setStatus("authenticated");
      setMessage(null);
    },
    async logout() {
      try {
        await authApi.logout();
      } finally {
        clearAuth();
      }
    },
    clearMessage() {
      setMessage(null);
    }
  }), [admin, message, status, clearAuth]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
