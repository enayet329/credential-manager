"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getStoredAuth, type StoredAuth } from "./auth";

/** Guards a page: redirects to /login if no session. Returns null until the check resolves. */
export function useRequireAuth(): { auth: StoredAuth | null; loading: boolean } {
  const router = useRouter();
  const [auth, setAuth] = useState<StoredAuth | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const stored = getStoredAuth();
    if (!stored) {
      router.replace("/login");
      return;
    }
    setAuth(stored);
    setLoading(false);
  }, [router]);

  return { auth, loading };
}
