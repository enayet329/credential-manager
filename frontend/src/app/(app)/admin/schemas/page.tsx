"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Schemas } from "@/lib/endpoints";
import { useOrg } from "@/lib/org-context";
import { describeError } from "@/lib/problem";
import type {
  CredentialFieldSchema,
  CredentialSchemaDto,
  FieldType,
  SupplierType,
} from "@/lib/types";

const supplierTypes: SupplierType[] = [
  "OpenAI",
  "Anthropic",
  "AzureOpenAI",
  "AwsCredentials",
  "GcpCredentials",
  "AzureCredentials",
  "Stripe",
  "GitHub",
  "GitLab",
  "Postgres",
  "MySql",
  "MongoDb",
  "Redis",
  "GenericApiKey",
  "Custom",
];

const fieldTypes: FieldType[] = ["Text", "Password", "Url", "MultiLine"];

function emptyField(): CredentialFieldSchema {
  return {
    key: "",
    displayName: "",
    fieldType: "Text",
    isRequired: true,
    isSecret: false,
    placeholder: "",
    validationRegex: "",
    helpText: "",
  };
}

export default function AdminSchemasPage() {
  const { org, permissions } = useOrg();
  const canManage = permissions.includes("admin:schemas");

  const [schemas, setSchemas] = useState<CredentialSchemaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showCreate, setShowCreate] = useState(false);
  const [supplierType, setSupplierType] = useState<SupplierType>("Custom");
  const [version, setVersion] = useState(1);
  const [fields, setFields] = useState<CredentialFieldSchema[]>([emptyField()]);
  const [submitting, setSubmitting] = useState(false);

  async function refresh() {
    if (!org) return;
    setLoading(true);
    try {
      setSchemas(await Schemas.list(org.slug));
      setError(null);
    } catch (err) {
      setError(describeError(err, "Failed to load schemas."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [org?.slug]);

  function patchField(idx: number, patch: Partial<CredentialFieldSchema>) {
    setFields((prev) => prev.map((f, i) => (i === idx ? { ...f, ...patch } : f)));
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    setError(null);
    try {
      await Schemas.register({
        supplierType,
        version,
        fields: fields.map((f) => ({
          ...f,
          placeholder: f.placeholder || null,
          validationRegex: f.validationRegex || null,
          helpText: f.helpText || null,
        })),
      });
      setShowCreate(false);
      setFields([emptyField()]);
      setVersion((v) => v + 1);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to register schema."));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <PageHeader
        title="Credential schemas"
        description="Every credential conforms to its supplier's schema. Schemas are immutable per version — adding a field means bumping the version. New credentials use the latest; existing credentials remain readable under their original version."
        actions={
          canManage ? (
            <button
              type="button"
              onClick={() => setShowCreate(true)}
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
            >
              Register new version
            </button>
          ) : null
        }
      />

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      {showCreate && (
        <Card className="mb-6">
          <form onSubmit={submit} className="space-y-5">
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-300">
                  Supplier type
                </label>
                <select
                  value={supplierType}
                  onChange={(e) => setSupplierType(e.target.value as SupplierType)}
                  className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
                >
                  {supplierTypes.map((t) => (
                    <option key={t} value={t}>
                      {t}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-300">
                  Version (strictly greater than latest)
                </label>
                <input
                  type="number"
                  min={1}
                  required
                  value={version}
                  onChange={(e) => setVersion(Number(e.target.value))}
                  className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
                />
              </div>
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-slate-300">Fields</h3>
                <button
                  type="button"
                  onClick={() => setFields((p) => [...p, emptyField()])}
                  className="text-xs text-emerald-300 hover:text-emerald-200"
                >
                  + Add field
                </button>
              </div>
              <div className="space-y-3">
                {fields.map((f, idx) => (
                  <div
                    key={idx}
                    className="rounded-lg border border-slate-800 bg-slate-950 p-4"
                  >
                    <div className="grid gap-3 sm:grid-cols-2">
                      <Input
                        label="Key (machine, e.g. api_key)"
                        value={f.key}
                        required
                        pattern="[a-z][a-z0-9_]*"
                        onChange={(v) => patchField(idx, { key: v })}
                      />
                      <Input
                        label="Display name"
                        value={f.displayName}
                        required
                        onChange={(v) => patchField(idx, { displayName: v })}
                      />
                      <div>
                        <label className="mb-1 block text-sm font-medium text-slate-300">
                          Field type
                        </label>
                        <select
                          value={f.fieldType}
                          onChange={(e) =>
                            patchField(idx, { fieldType: e.target.value as FieldType })
                          }
                          className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
                        >
                          {fieldTypes.map((t) => (
                            <option key={t} value={t}>
                              {t}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="flex items-end gap-4">
                        <Checkbox
                          label="Required"
                          checked={f.isRequired}
                          onChange={(v) => patchField(idx, { isRequired: v })}
                        />
                        <Checkbox
                          label="Secret"
                          checked={f.isSecret}
                          onChange={(v) => patchField(idx, { isSecret: v })}
                        />
                      </div>
                      <Input
                        label="Placeholder (optional)"
                        value={f.placeholder ?? ""}
                        onChange={(v) => patchField(idx, { placeholder: v })}
                      />
                      <Input
                        label="Validation regex (optional)"
                        value={f.validationRegex ?? ""}
                        onChange={(v) => patchField(idx, { validationRegex: v })}
                      />
                      <div className="sm:col-span-2">
                        <Input
                          label="Help text (optional)"
                          value={f.helpText ?? ""}
                          onChange={(v) => patchField(idx, { helpText: v })}
                        />
                      </div>
                    </div>
                    {fields.length > 1 && (
                      <button
                        type="button"
                        onClick={() => setFields((p) => p.filter((_, i) => i !== idx))}
                        className="mt-3 text-xs text-red-400 hover:text-red-300"
                      >
                        Remove field
                      </button>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div className="flex justify-end gap-2">
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
                {submitting ? "Registering…" : "Register schema"}
              </button>
            </div>
          </form>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {loading ? (
          <p className="text-sm text-slate-500">Loading…</p>
        ) : (
          schemas.map((s) => (
            <Card key={`${s.supplierType}:${s.version}`}>
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold">{s.supplierType}</h3>
                <span className="rounded-full border border-slate-700 px-2 py-0.5 font-mono text-xs text-slate-400">
                  v{s.version}
                </span>
              </div>
              <ul className="mt-3 space-y-2 text-sm">
                {s.fields.map((f) => (
                  <li
                    key={f.key}
                    className="flex items-center justify-between rounded-md border border-slate-800 bg-slate-950 px-3 py-2"
                  >
                    <div>
                      <p className="font-medium">{f.displayName}</p>
                      <p className="font-mono text-xs text-slate-500">{f.key}</p>
                    </div>
                    <div className="flex flex-wrap items-center gap-1.5">
                      <Tag>{f.fieldType}</Tag>
                      {f.isRequired && <Tag>required</Tag>}
                      {f.isSecret && (
                        <Tag className="bg-amber-500/20 text-amber-300">secret</Tag>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            </Card>
          ))
        )}
      </div>
    </>
  );
}

function Input({
  label,
  value,
  onChange,
  required,
  pattern,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  required?: boolean;
  pattern?: string;
}) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-300">{label}</label>
      <input
        value={value}
        required={required}
        pattern={pattern}
        onChange={(e) => onChange(e.target.value)}
        className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
      />
    </div>
  );
}

function Checkbox({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <label className="flex items-center gap-2 text-sm">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="h-4 w-4 rounded border-slate-700 bg-slate-950"
      />
      {label}
    </label>
  );
}

function Tag({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <span
      className={`rounded-full border border-slate-700 px-2 py-0.5 font-mono text-[10px] ${className ?? "text-slate-300"}`}
    >
      {children}
    </span>
  );
}
