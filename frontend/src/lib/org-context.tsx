"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import type { LoginOrganization, StoredAuth } from "./types";
import { getStoredAuth, setStoredAuth } from "./auth";

const SELECTED_ORG_KEY = "credvault.selectedOrg";

interface OrgContextValue {
  organizations: LoginOrganization[];
  org: LoginOrganization | null;
  setOrg: (slug: string) => void;
  permissions: string[];
}

const OrgContext = createContext<OrgContextValue | null>(null);

export function OrgProvider({
  auth,
  children,
}: {
  auth: StoredAuth;
  children: ReactNode;
}) {
  const [slug, setSlug] = useState<string>(() => {
    if (typeof window === "undefined") return auth.organizations[0]?.slug ?? "";
    const stored = window.localStorage.getItem(SELECTED_ORG_KEY);
    if (stored && auth.organizations.some((o) => o.slug === stored)) return stored;
    return auth.organizations[0]?.slug ?? "";
  });

  useEffect(() => {
    if (slug && typeof window !== "undefined") {
      window.localStorage.setItem(SELECTED_ORG_KEY, slug);
    }
  }, [slug]);

  const org = auth.organizations.find((o) => o.slug === slug) ?? null;

  return (
    <OrgContext.Provider
      value={{
        organizations: auth.organizations,
        org,
        setOrg: setSlug,
        permissions: auth.permissions,
      }}
    >
      {children}
    </OrgContext.Provider>
  );
}

export function useOrg(): OrgContextValue {
  const ctx = useContext(OrgContext);
  if (!ctx) throw new Error("useOrg must be used inside an OrgProvider.");
  return ctx;
}

/** Returns the current org slug or throws — convenience for pages that require one. */
export function useOrgSlug(): string {
  const { org } = useOrg();
  if (!org) throw new Error("No organisation selected.");
  return org.slug;
}

// Re-export so call sites don't need to import from auth.ts directly.
export { getStoredAuth, setStoredAuth };
