"use client";
import { useOrg } from "@/lib/org-context";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Environments, Projects } from "@/lib/endpoints";
import { describeError } from "@/lib/problem";
import type { EnvironmentDto, EnvironmentType, ProjectDto } from "@/lib/types";

const envTypes: EnvironmentType[] = ["Development", "Uat", "Staging", "Production", "Custom"];

export default function ProjectDetailPage() {
  const params = useParams<{ projectSlug: string }>();
  const projectSlug = params.projectSlug;
  const { org } = useOrg();
  const orgSlug = org?.slug ?? "";

  const [project, setProject] = useState<ProjectDto | null>(null);
  const [envs, setEnvs] = useState<EnvironmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [type, setType] = useState<EnvironmentType>("Development");
  const [submitting, setSubmitting] = useState(false);

  async function refresh() {
    if (!orgSlug || !projectSlug) return;
    setLoading(true);
    try {
      const [p, e] = await Promise.all([
        Projects.get(orgSlug, projectSlug),
        Environments.list(orgSlug, projectSlug),
      ]);
      setProject(p);
      setEnvs(e);
      setError(null);
    } catch (err) {
      setError(describeError(err, "Failed to load project."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orgSlug, projectSlug]);

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    try {
      await Environments.create(orgSlug, projectSlug, { name, slug, type });
      setName("");
      setSlug("");
      setShowCreate(false);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to create environment."));
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return <p className="text-sm text-slate-500">Loading…</p>;
  if (!project) return <p className="text-sm text-red-400">{error ?? "Not found."}</p>;

  return (
    <>
      <PageHeader
        title={project.name}
        description={project.description ?? "Environments and credentials live under this project."}
        actions={
          <button
            type="button"
            onClick={() => setShowCreate(true)}
            className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
          >
            New environment
          </button>
        }
      />

      <p className="mb-6 font-mono text-xs text-slate-500">{project.slug}</p>

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      {showCreate && (
        <Card className="mb-6">
          <form onSubmit={onCreate} className="grid gap-4 sm:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">Name</label>
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">Slug</label>
              <input
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                required
                pattern="[a-z0-9-]{3,50}"
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm font-mono"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">Type</label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as EnvironmentType)}
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              >
                {envTypes.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-3 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
              >
                {submitting ? "Creating…" : "Create"}
              </button>
            </div>
          </form>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {envs.length === 0 ? (
          <p className="text-sm text-slate-500">No environments yet.</p>
        ) : (
          envs.map((env) => (
            <Link
              key={env.id}
              href={`/credentials?project=${project.slug}&environment=${env.slug}`}
              className="rounded-xl border border-slate-800 bg-slate-900/50 p-5 transition hover:border-slate-700 hover:bg-slate-900"
            >
              <p className="text-xs uppercase tracking-wide text-slate-500">{env.type}</p>
              <h3 className="mt-1 text-lg font-semibold">{env.name}</h3>
              <p className="mt-0.5 font-mono text-xs text-slate-400">{env.slug}</p>
            </Link>
          ))
        )}
      </div>
    </>
  );
}
