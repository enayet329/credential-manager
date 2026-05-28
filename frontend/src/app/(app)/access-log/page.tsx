"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { AccessLog } from "@/lib/endpoints";
import { useOrg } from "@/lib/org-context";
import { describeError } from "@/lib/problem";
import type { CredentialAccessDto } from "@/lib/types";

export default function AccessLogPage() {
  const { org } = useOrg();
  const [rows, setRows] = useState<CredentialAccessDto[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!org) return;
    setLoading(true);
    AccessLog.org(org.slug, { limit: 50 })
      .then((p) => {
        setRows(p.items);
        setNextCursor(p.nextCursor ?? null);
        setError(null);
      })
      .catch((err) => setError(describeError(err, "Failed to load access log.")))
      .finally(() => setLoading(false));
  }, [org]);

  async function loadMore() {
    if (!org || !nextCursor || loadingMore) return;
    setLoadingMore(true);
    try {
      const page = await AccessLog.org(org.slug, { limit: 50, cursor: nextCursor });
      setRows((prev) => [...prev, ...page.items]);
      setNextCursor(page.nextCursor ?? null);
    } catch (err) {
      setError(describeError(err, "Failed to load more."));
    } finally {
      setLoadingMore(false);
    }
  }

  return (
    <>
      <PageHeader
        title="Access log"
        description="Every successful credential decrypt and every denied attempt across the organisation. Append-only — rows can never be edited or removed."
      />

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      <Card className="p-0 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-900/80 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-5 py-3">When</th>
              <th className="px-5 py-3">Credential</th>
              <th className="px-5 py-3">Actor</th>
              <th className="px-5 py-3">Method</th>
              <th className="px-5 py-3">Outcome</th>
              <th className="px-5 py-3">IP</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="px-5 py-8 text-center text-slate-500">
                  Loading…
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-5 py-8 text-center text-slate-500">
                  No access yet.
                </td>
              </tr>
            ) : (
              rows.map((r) => (
                <tr key={r.id} className="border-t border-slate-800">
                  <td className="px-5 py-3 text-xs text-slate-400">
                    {new Date(r.accessedAtUtc).toLocaleString()}
                  </td>
                  <td className="px-5 py-3">
                    <Link
                      href={`/credentials/${r.credentialId}`}
                      className="font-mono text-xs text-emerald-300 hover:underline"
                    >
                      {r.credentialId.slice(0, 8)}…
                    </Link>
                  </td>
                  <td className="px-5 py-3 font-mono text-xs">
                    <span className="rounded-full border border-slate-700 px-2 py-0.5 text-[10px]">
                      {r.actorType}
                    </span>{" "}
                    {r.actorId.slice(0, 8)}…
                  </td>
                  <td className="px-5 py-3 text-xs">{r.accessMethod}</td>
                  <td className="px-5 py-3">
                    <span
                      className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        r.outcome === "Success"
                          ? "bg-emerald-500/20 text-emerald-300"
                          : "bg-red-500/20 text-red-300"
                      }`}
                    >
                      {r.outcome}
                    </span>
                  </td>
                  <td className="px-5 py-3 font-mono text-xs text-slate-500">
                    {r.ipAddress || "—"}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </Card>

      {nextCursor && (
        <div className="mt-4 text-center">
          <button
            type="button"
            onClick={loadMore}
            disabled={loadingMore}
            className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500 disabled:opacity-60"
          >
            {loadingMore ? "Loading…" : "Load more"}
          </button>
        </div>
      )}
    </>
  );
}
