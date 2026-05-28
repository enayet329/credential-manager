"use client";
import { useOrg } from "@/lib/org-context";

import { useRouter } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { SchemaForm } from "@/components/SchemaForm";
import { StepUpDialog } from "@/components/StepUpDialog";
import { Credentials, Environments, Projects, Schemas, Suppliers } from "@/lib/endpoints";
import { describeError, isStepUpRequired } from "@/lib/problem";
import type {
  CredentialSchemaDto,
  EnvironmentDto,
  ProjectDto,
  SupplierDto,
} from "@/lib/types";

export default function CredentialCreatePage() {
  const router = useRouter();
  const { org } = useOrg();
  const orgSlug = org?.slug ?? "";

  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [envs, setEnvs] = useState<EnvironmentDto[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [schema, setSchema] = useState<CredentialSchemaDto | null>(null);

  const [project, setProject] = useState("");
  const [environment, setEnvironment] = useState("");
  const [supplierId, setSupplierId] = useState("");
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [fields, setFields] = useState<Record<string, string>>({});

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [stepUpOpen, setStepUpOpen] = useState(false);
  const [pendingFn, setPendingFn] = useState<((token: string) => Promise<void>) | null>(null);

  useEffect(() => {
    if (!orgSlug) return;
    Projects.list(orgSlug).then(setProjects).catch(() => {});
    Suppliers.list(orgSlug).then(setSuppliers).catch(() => {});
  }, [orgSlug]);

  useEffect(() => {
    if (!project) {
      setEnvs([]);
      setEnvironment("");
      return;
    }
    Environments.list(orgSlug, project).then(setEnvs).catch(() => setEnvs([]));
  }, [orgSlug, project]);

  useEffect(() => {
    if (!supplierId) {
      setSchema(null);
      return;
    }
    const supplier = suppliers.find((s) => s.id === supplierId);
    if (!supplier) return;
    Schemas.get(orgSlug, supplier.supplierType)
      .then((s) => {
        setSchema(s);
        const next: Record<string, string> = {};
        for (const f of s.fields) next[f.key] = "";
        setFields(next);
      })
      .catch(() => setSchema(null));
  }, [orgSlug, supplierId, suppliers]);

  async function submit(stepUpToken: string) {
    setSubmitting(true);
    try {
      await Credentials.create(
        orgSlug,
        project,
        environment,
        supplierId,
        { name, slug, fields },
        stepUpToken,
      );
      router.push("/credentials");
    } catch (err) {
      setError(describeError(err, "Failed to create credential."));
    } finally {
      setSubmitting(false);
    }
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setError(null);
    try {
      // Try without step-up first; will throw on 403 step_up_required.
      await Credentials.create(
        orgSlug,
        project,
        environment,
        supplierId,
        { name, slug, fields },
        "",
      );
      router.push("/credentials");
    } catch (err) {
      if (isStepUpRequired(err)) {
        setPendingFn(() => submit);
        setStepUpOpen(true);
      } else {
        setError(describeError(err, "Failed to create credential."));
      }
    }
  }

  return (
    <>
      <PageHeader
        title="New credential"
        description="Pick a project, environment, and supplier — the form renders from that supplier's schema."
      />

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      <Card>
        <form onSubmit={onSubmit} className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-3">
            <Select label="Project" value={project} onChange={setProject} options={projects.map((p) => ({ value: p.slug, label: p.name }))} required />
            <Select label="Environment" value={environment} onChange={setEnvironment} options={envs.map((e) => ({ value: e.slug, label: e.name }))} required disabled={!project} />
            <Select label="Supplier" value={supplierId} onChange={setSupplierId} options={suppliers.filter((s) => s.isActive).map((s) => ({ value: s.id, label: `${s.displayName} (${s.supplierType})` }))} required />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
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
                placeholder="primary-key"
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm font-mono"
              />
            </div>
          </div>

          {schema && (
            <div className="border-t border-slate-800 pt-5">
              <p className="mb-3 text-xs uppercase tracking-wide text-slate-500">
                Schema v{schema.version} · {schema.supplierType}
              </p>
              <SchemaForm
                fields={schema.fields}
                values={fields}
                onChange={(k, v) => setFields((prev) => ({ ...prev, [k]: v }))}
                disabled={submitting}
              />
            </div>
          )}

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => router.push("/credentials")}
              className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting || !schema}
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
            >
              {submitting ? "Creating…" : "Create credential"}
            </button>
          </div>
        </form>
      </Card>

      <StepUpDialog
        open={stepUpOpen}
        onResolved={async (token) => {
          setStepUpOpen(false);
          if (pendingFn) await pendingFn(token);
          setPendingFn(null);
        }}
        onCancel={() => {
          setStepUpOpen(false);
          setPendingFn(null);
        }}
      />
    </>
  );
}

function Select({
  label,
  value,
  onChange,
  options,
  required,
  disabled,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  required?: boolean;
  disabled?: boolean;
}) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-300">
        {label}
        {required && <span className="ml-0.5 text-red-400">*</span>}
      </label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        disabled={disabled}
        className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm disabled:opacity-50"
      >
        <option value="">— Select —</option>
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </div>
  );
}
