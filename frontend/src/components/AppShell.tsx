"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { clearStoredAuth, type StoredAuth } from "@/lib/auth";
import { OrgProvider, useOrg } from "@/lib/org-context";

const navLinks: { href: string; label: string; permission?: string }[] = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/credentials", label: "Credentials" },
  { href: "/projects", label: "Projects" },
  { href: "/suppliers", label: "Suppliers" },
  { href: "/members", label: "Members" },
  { href: "/access-log", label: "Audit" },
  { href: "/account", label: "Account" },
  { href: "/admin/schemas", label: "Schemas", permission: "admin:schemas" },
];

export function AppShell({ auth, children }: { auth: StoredAuth; children: ReactNode }) {
  return (
    <OrgProvider auth={auth}>
      <Inner auth={auth}>{children}</Inner>
    </OrgProvider>
  );
}

function Inner({ auth, children }: { auth: StoredAuth; children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { organizations, org, setOrg, permissions } = useOrg();
  const [mobileOpen, setMobileOpen] = useState(false);

  // Auto-close drawer on route change.
  useEffect(() => {
    setMobileOpen(false);
  }, [pathname]);

  function signOut() {
    clearStoredAuth();
    router.push("/login");
  }

  const visibleLinks = navLinks.filter(
    (l) => !l.permission || permissions.includes(l.permission),
  );

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-30 border-b border-slate-800/60 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-3 px-4 py-3 sm:px-6 sm:py-4">
          <div className="flex items-center gap-3 md:gap-8">
            <button
              type="button"
              onClick={() => setMobileOpen((v) => !v)}
              className="inline-flex h-9 w-9 items-center justify-center rounded-md border border-slate-800 text-slate-300 hover:bg-slate-900 md:hidden"
              aria-label="Toggle navigation"
              aria-expanded={mobileOpen}
            >
              {mobileOpen ? <CloseIcon /> : <MenuIcon />}
            </button>

            <Link href="/dashboard" className="flex items-center gap-2 text-base font-semibold sm:text-lg">
              <span className="grid h-8 w-8 place-items-center rounded-md bg-emerald-500/20 text-emerald-300">
                CV
              </span>
              <span className="hidden sm:inline">CredVault</span>
            </Link>

            <nav className="hidden gap-1 md:flex">
              {visibleLinks.map((link) => {
                const active =
                  pathname === link.href || pathname.startsWith(`${link.href}/`);
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    className={`rounded-md px-3 py-1.5 text-sm transition ${
                      active
                        ? "bg-slate-800 text-slate-100"
                        : "text-slate-400 hover:bg-slate-900 hover:text-slate-200"
                    }`}
                  >
                    {link.label}
                  </Link>
                );
              })}
            </nav>
          </div>

          <div className="flex items-center gap-2 text-sm sm:gap-3">
            {organizations.length > 0 && (
              <select
                value={org?.slug ?? ""}
                onChange={(e) => setOrg(e.target.value)}
                className="max-w-[140px] truncate rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-xs sm:max-w-none"
                title="Switch organisation"
              >
                {organizations.map((o) => (
                  <option key={o.slug} value={o.slug}>
                    {o.name} · {o.role}
                  </option>
                ))}
              </select>
            )}
            <span className="hidden text-slate-400 lg:block">{auth.email}</span>
            <button
              type="button"
              onClick={signOut}
              className="rounded-md border border-slate-700 px-2.5 py-1.5 text-xs hover:border-slate-500 hover:bg-slate-800 sm:px-3 sm:text-sm"
            >
              Sign out
            </button>
          </div>
        </div>

        {mobileOpen && (
          <nav className="border-t border-slate-800/60 bg-slate-950 md:hidden">
            <div className="mx-auto flex max-w-6xl flex-col px-4 py-2">
              {visibleLinks.map((link) => {
                const active =
                  pathname === link.href || pathname.startsWith(`${link.href}/`);
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    className={`rounded-md px-3 py-2.5 text-sm transition ${
                      active
                        ? "bg-slate-800 text-slate-100"
                        : "text-slate-300 hover:bg-slate-900"
                    }`}
                  >
                    {link.label}
                  </Link>
                );
              })}
              <div className="mt-2 border-t border-slate-800/60 px-3 pt-3 pb-1 text-xs text-slate-500">
                Signed in as <span className="text-slate-300">{auth.email}</span>
              </div>
            </div>
          </nav>
        )}
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8">{children}</main>
    </div>
  );
}

function MenuIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
      <line x1="3" y1="6" x2="21" y2="6" />
      <line x1="3" y1="12" x2="21" y2="12" />
      <line x1="3" y1="18" x2="21" y2="18" />
    </svg>
  );
}

function CloseIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
      <line x1="18" y1="6" x2="6" y2="18" />
      <line x1="6" y1="6" x2="18" y2="18" />
    </svg>
  );
}
