"use client";

import { useCallback, useEffect, useState } from "react";

/** Simple countdown timer (seconds) for OTP resend cooldowns. */
export function useCooldown() {
  const [seconds, setSeconds] = useState(0);

  useEffect(() => {
    if (seconds <= 0) return;
    const id = setTimeout(() => setSeconds((s) => s - 1), 1000);
    return () => clearTimeout(id);
  }, [seconds]);

  const start = useCallback((value: number) => setSeconds(Math.max(0, Math.ceil(value))), []);

  return { seconds, start, active: seconds > 0 };
}
