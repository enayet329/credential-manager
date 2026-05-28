"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { describeError } from "@/lib/problem";

interface RedeemedShare {
  credentialId: string;
  name: string;
  slug: string;
  supplierType: string;
  maskedPreview: string;
  allowReveal: boolean;
  fields?: Record<string, string> | null;
}

export default function SharedCredentialPage() {
  const params = useParams<{ token: string }>();
  const token = params.token;
  const [data, setData] = useState<RedeemedShare | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<RedeemedShare>(`/shares/${encodeURIComponent(token)}`)
      .then((r) => setData(r.data))
      .catch((err) => setError(describeError(err, "Share link is invalid or expired.")));
  }, [token]);

  return (
    <main className="min-h-screen grid place-items-center px-6">
      <div className="w-full max-w-xl">
        <Link href="/" className="mb-8 flex items-center gap-2 text-lg font-semibold">
          <span className="grid h-8 w-8 place-items-center rounded-md bg-emerald-500/20 text-emerald-300">
            CV
          </span>
          CredVault
        </Link>

        <div className="rounded-2xl border border-slate-800 bg-slate-900/60 p-8 shadow-xl">
          <p className="text-xs uppercase tracking-wide text-slate-500">Shared credential</p>

          {error ? (
            <p className="mt-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-3 text-sm text-red-300">
              {error}
            </p>
          ) : !data ? (
            <p className="mt-4 text-sm text-slate-400">Verifying link…</p>
          ) : (
            <>
              <h1 className="mt-1 text-2xl font-semibold">{data.name}</h1>
              <p className="mt-0.5 font-mono text-xs text-slate-400">
                {data.supplierType} · {data.slug}
              </p>

              <div className="mt-6 space-y-3">
                {data.allowReveal && data.fields ? (
                  Object.entries(data.fields).map(([k, v]) => (
                    <div
                      key={k}
                      className="rounded-md border border-slate-800 bg-slate-950 p-3"
                    >
                      <p className="text-xs uppercase tracking-wide text-slate-500">{k}</p>
                      <p className="mt-1 break-all font-mono text-sm">{v}</p>
                    </div>
                  ))
                ) : (
                  <div className="rounded-md border border-slate-800 bg-slate-950 p-3">
                    <p className="text-xs uppercase tracking-wide text-slate-500">Masked preview</p>
                    <p className="mt-1 font-mono text-sm">{data.maskedPreview}</p>
                    <p className="mt-3 text-xs text-amber-300">
                      Sharer chose metadata-only — the value isn&apos;t included in this link.
                    </p>
                  </div>
                )}
              </div>
            </>
          )}
        </div>

        <p className="mt-6 text-center text-xs text-slate-500">
          This link was issued by someone with access to the credential and is signed by CredVault.
          It will expire automatically.
        </p>
      </div>
    </main>
  );
}
