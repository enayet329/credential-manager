"use client";
import { useOrg } from "@/lib/org-context";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Credentials, Environments, Projects } from "@/lib/endpoints";
import { describeError } from "@/lib/problem";
import type { CredentialMetadataDto, EnvironmentDto, ProjectDto } from "@/lib/types";

export default function CredentialsPage() {
  const { org } = useOrg();
  const orgSlug = org?.slug ?? "";
  const params = useSearchParams();
  const initialProject = params.get("project") ?? "";
  const initialEnv = params.get("environment") ?? "";

  const [creds, setCreds] = useState<CredentialMetadataDto[]>([]);
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [envs, setEnvs] = useState<EnvironmentDto[]>([]);
  const [project, setProject] = useState(initialProject);
  const [environment, setEnvironment] = useState(initialEnv);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!orgSlug) return;
    Projects.list(orgSlug).then(setProjects).catch(() => {});
  }, [orgSlug]);

  useEffect(() => {
    if (!orgSlug || !project) {
      setEnvs([]);
      return;
    }
    Environments.list(orgSlug, project).then(setEnvs).catch(() => setEnvs([]));
  }, [orgSlug, project]);

  useEffect(() => {
    if (!orgSlug) return;
    setLoading(true);
    Credentials.list(orgSlug, {
      project: project || undefined,
      environment: environment || undefined,
    })
      .then((rows) => {
        setCreds(rows);
        setError(null);
      })
      .catch((err) => setError(describeError(err, "Failed to load credentials.")))
      .finally(() => setLoading(false));
  }, [orgSlug, project, environment]);

  return (
    <>
      <PageHeader
        title="Credentials"
        description="Encrypted credential rows scoped to a project and environment."
        actions={
          <>
            <button
              type="button"
              onClick={async () => {
                if (!orgSlug) return;
                try {
                  const blob = await Credentials.exportBlob(orgSlug, {
                    project: project || undefined,
                    environment: environment || undefined,
                  });
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement("a");
                  a.href = url;
                  a.download = `credvault-${orgSlug}-${new Date().toISOString().slice(0, 10)}.xlsx`;
                  a.click();
                  URL.revokeObjectURL(url);
                } catch (err) {
                  setError(describeError(err, "Export failed."));
                }
              }}
              className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500"
            >
              ↓ Export Excel
            </button>
            <Link
              href="/credentials/new"
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
            >
              + New credential
            </Link>
          </>
        }
      />

      <Card className="mb-6">
        <div className="grid gap-3 sm:grid-cols-3">
          <div>
            <label className="mb-1 block text-xs uppercase tracking-wide text-slate-500">
              Project
            </label>
            <select
              value={project}
              onChange={(e) => {
                setProject(e.target.value);
                setEnvironment("");
              }}
              className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
            >
              <option value="">All</option>
              {projects.map((p) => (
                <option key={p.slug} value={p.slug}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs uppercase tracking-wide text-slate-500">
              Environment
            </label>
            <select
              value={environment}
              onChange={(e) => setEnvironment(e.target.value)}
              disabled={!project}
              className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm disabled:opacity-50"
            >
              <option value="">All</option>
              {envs.map((e) => (
                <option key={e.slug} value={e.slug}>
                  {e.name}
                </option>
              ))}
            </select>
          </div>
        </div>
      </Card>

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      <Card className="p-0 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-900/80 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-5 py-3">Name</th>
              <th className="px-5 py-3">Supplier</th>
              <th className="px-5 py-3">Preview</th>
              <th className="px-5 py-3">Last access</th>
              <th className="px-5 py-3">Status</th>
              <th className="px-5 py-3" />
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} className="px-5 py-8 text-center text-slate-500">
                  Loading…
                </td>
              </tr>
            ) : creds.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-5 py-8 text-center text-slate-500">
                  No credentials yet.
                </td>
              </tr>
            ) : (
              creds.map((c) => (
                <tr key={c.id} className="border-t border-slate-800">
                  <td className="px-5 py-3">
                    <Link href={`/credentials/${c.id}`} className="text-emerald-300 hover:underline">
                      {c.name}
                    </Link>
                    <div className="font-mono text-xs text-slate-500">{c.slug}</div>
                  </td>
                  <td className="px-5 py-3 font-mono text-xs">{c.supplierType}</td>
                  <td className="px-5 py-3 font-mono text-xs">{c.maskedPreview}</td>
                  <td className="px-5 py-3 text-xs text-slate-500">
                    {c.lastAccessedAtUtc
                      ? new Date(c.lastAccessedAtUtc).toLocaleString()
                      : "—"}
                  </td>
                  <td className="px-5 py-3">
                    <span
                      className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        c.isRevoked
                          ? "bg-red-500/20 text-red-300"
                          : "bg-emerald-500/20 text-emerald-300"
                      }`}
                    >
                      {c.isRevoked ? "Revoked" : "Active"}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-right">
                    <Link
                      href={`/credentials/${c.id}`}
                      className="text-xs text-slate-300 hover:text-slate-100"
                    >
                      Open →
                    </Link>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </Card>
    </>
  );
}
