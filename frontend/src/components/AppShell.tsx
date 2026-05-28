"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { clearStoredAuth, type StoredAuth } from "@/lib/auth";
import { OrgProvider, useOrg } from "@/lib/org-context";

const navLinks: { href: string; label: string; permission?: string }[] = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/credentials", label: "Credentials" },
  { href: "/projects", label: "Projects" },
  { href: "/suppliers", label: "Suppliers" },
  { href: "/members", label: "Members" },
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

  function signOut() {
    clearStoredAuth();
    router.push("/login");
  }

  const visibleLinks = navLinks.filter(
    (l) => !l.permission || permissions.includes(l.permission),
  );

  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-800/60 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div className="flex items-center gap-8">
            <Link href="/dashboard" className="flex items-center gap-2 text-lg font-semibold">
              <span className="grid h-8 w-8 place-items-center rounded-md bg-emerald-500/20 text-emerald-300">
                CV
              </span>
              CredVault
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
          <div className="flex items-center gap-3 text-sm">
            {organizations.length > 0 && (
              <select
                value={org?.slug ?? ""}
                onChange={(e) => setOrg(e.target.value)}
                className="rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-xs"
                title="Switch organisation"
              >
                {organizations.map((o) => (
                  <option key={o.slug} value={o.slug}>
                    {o.name} · {o.role}
                  </option>
                ))}
              </select>
            )}
            <span className="hidden text-slate-400 sm:block">{auth.email}</span>
            <button
              type="button"
              onClick={signOut}
              className="rounded-md border border-slate-700 px-3 py-1.5 hover:border-slate-500 hover:bg-slate-800"
            >
              Sign out
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-6 py-8">{children}</main>
    </div>
  );
}
