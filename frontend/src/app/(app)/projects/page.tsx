"use client";
import { useOrg } from "@/lib/org-context";

import Link from "next/link";
import { useEffect, useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Projects } from "@/lib/endpoints";
import { describeError } from "@/lib/problem";
import type { ProjectDto } from "@/lib/types";

export default function ProjectsPage() {
  const { org } = useOrg();
  const orgSlug = org?.slug ?? "";
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [description, setDescription] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function refresh() {
    if (!orgSlug) return;
    setLoading(true);
    try {
      setProjects(await Projects.list(orgSlug));
      setError(null);
    } catch (err) {
      setError(describeError(err, "Failed to load projects."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orgSlug]);

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    try {
      await Projects.create(orgSlug, { name, slug, description: description || undefined });
      setName("");
      setSlug("");
      setDescription("");
      setShowCreate(false);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to create project."));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <PageHeader
        title="Projects"
        description="Each project groups environments and credentials. Projects map roughly to one deployable unit."
        actions={
          <button
            type="button"
            onClick={() => setShowCreate(true)}
            className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
          >
            New project
          </button>
        }
      />

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      {showCreate && (
        <Card className="mb-6">
          <form onSubmit={onCreate} className="grid gap-4 sm:grid-cols-2">
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
                placeholder="my-app"
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm font-mono"
              />
            </div>
            <div className="sm:col-span-2">
              <label className="mb-1 block text-sm font-medium text-slate-300">
                Description (optional)
              </label>
              <textarea
                rows={2}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              />
            </div>
            <div className="sm:col-span-2 flex justify-end gap-2">
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
        {loading ? (
          <p className="text-sm text-slate-500">Loading…</p>
        ) : projects.length === 0 ? (
          <p className="text-sm text-slate-500">No projects yet.</p>
        ) : (
          projects.map((p) => (
            <Link
              key={p.id}
              href={`/projects/${p.slug}`}
              className="rounded-xl border border-slate-800 bg-slate-900/50 p-5 transition hover:border-slate-700 hover:bg-slate-900"
            >
              <h3 className="text-lg font-semibold">{p.name}</h3>
              <p className="mt-0.5 font-mono text-xs text-slate-400">{p.slug}</p>
              {p.description && (
                <p className="mt-3 text-sm text-slate-400">{p.description}</p>
              )}
              <p className="mt-3 text-xs text-slate-500">
                Created {new Date(p.createdAtUtc).toLocaleDateString()}
              </p>
            </Link>
          ))
        )}
      </div>
    </>
  );
}
