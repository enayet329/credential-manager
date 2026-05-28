"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Credentials, Projects, Suppliers } from "@/lib/endpoints";
import { useOrg } from "@/lib/org-context";
import { getStoredAuth } from "@/lib/auth";

export default function DashboardPage() {
  const { org, permissions } = useOrg();
  const orgSlug = org?.slug ?? "";
  const auth = getStoredAuth();
  const [counts, setCounts] = useState({
    credentials: 0,
    suppliers: 0,
    projects: 0,
    loading: true,
  });

  useEffect(() => {
    if (!orgSlug) {
      setCounts({ credentials: 0, suppliers: 0, projects: 0, loading: false });
      return;
    }
    setCounts((c) => ({ ...c, loading: true }));
    Promise.all([
      Credentials.list(orgSlug).catch(() => []),
      Suppliers.list(orgSlug).catch(() => []),
      Projects.list(orgSlug).catch(() => []),
    ]).then(([creds, sups, projs]) =>
      setCounts({
        credentials: creds.length,
        suppliers: sups.length,
        projects: projs.length,
        loading: false,
      }),
    );
  }, [orgSlug]);

  if (!auth) return null;

  return (
    <>
      <PageHeader
        title="Welcome back."
        description={`Signed in as ${auth.email}.`}
      />

      {!org ? (
        <Card>
          <p className="text-sm text-slate-400">
            You aren&apos;t a member of any organisation yet.
          </p>
        </Card>
      ) : (
        <>
          <Card className="mb-6">
            <p className="text-xs uppercase tracking-wide text-slate-500">{org.role}</p>
            <h2 className="mt-1 text-lg font-semibold">{org.name}</h2>
            <p className="mt-0.5 font-mono text-xs text-slate-400">{org.slug}</p>
          </Card>

          <div className="grid gap-4 sm:grid-cols-3">
            <Stat label="Credentials" value={counts.credentials} href="/credentials" loading={counts.loading} />
            <Stat label="Projects" value={counts.projects} href="/projects" loading={counts.loading} />
            <Stat label="Suppliers" value={counts.suppliers} href="/suppliers" loading={counts.loading} />
          </div>
        </>
      )}

      <Card className="mt-8">
        <h3 className="text-sm font-semibold text-slate-300">Your permissions</h3>
        <ul className="mt-3 flex flex-wrap gap-2">
          {permissions.map((p) => (
            <li
              key={p}
              className="rounded-full border border-slate-700 bg-slate-950 px-3 py-1 font-mono text-xs text-slate-300"
            >
              {p}
            </li>
          ))}
        </ul>
      </Card>
    </>
  );
}

function Stat({
  label,
  value,
  href,
  loading,
}: {
  label: string;
  value: number;
  href: string;
  loading: boolean;
}) {
  return (
    <Link
      href={href}
      className="rounded-xl border border-slate-800 bg-slate-900/50 p-5 transition hover:border-slate-700 hover:bg-slate-900"
    >
      <p className="text-xs uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-2 text-3xl font-semibold">{loading ? "…" : value}</p>
    </Link>
  );
}
