"use client";
import { useOrg } from "@/lib/org-context";

import { useEffect, useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Schemas, Suppliers } from "@/lib/endpoints";
import { describeError } from "@/lib/problem";
import type { SupplierDto, SupplierType } from "@/lib/types";

export default function SuppliersPage() {
  const { org } = useOrg();
  const orgSlug = org?.slug ?? "";
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [types, setTypes] = useState<SupplierType[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  // create form state
  const [showCreate, setShowCreate] = useState(false);
  const [newType, setNewType] = useState<SupplierType>("OpenAI");
  const [newName, setNewName] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function refresh() {
    if (!orgSlug) return;
    setLoading(true);
    try {
      const [list, schemas] = await Promise.all([
        Suppliers.list(orgSlug),
        Schemas.list(orgSlug),
      ]);
      setSuppliers(list);
      setTypes(schemas.map((s) => s.supplierType));
      setError(null);
    } catch (err) {
      setError(describeError(err, "Failed to load suppliers."));
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
    if (submitting || !orgSlug) return;
    setSubmitting(true);
    try {
      await Suppliers.create(orgSlug, newType, newName);
      setNewName("");
      setShowCreate(false);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to create supplier."));
    } finally {
      setSubmitting(false);
    }
  }

  async function toggleActive(s: SupplierDto) {
    try {
      await Suppliers.patch(orgSlug, s.id, { isActive: !s.isActive });
      refresh();
    } catch (err) {
      setError(describeError(err, "Failed to update supplier."));
    }
  }

  async function remove(s: SupplierDto) {
    if (!confirm(`Delete supplier "${s.displayName}"?`)) return;
    try {
      await Suppliers.remove(orgSlug, s.id);
      refresh();
    } catch (err) {
      setError(describeError(err, "Failed to delete supplier."));
    }
  }

  return (
    <>
      <PageHeader
        title="Suppliers"
        description="Labelled issuers grouping your credentials. CredVault never calls supplier APIs — these are metadata only."
        actions={
          <button
            type="button"
            onClick={() => setShowCreate(true)}
            className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
          >
            New supplier
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
          <form onSubmit={onCreate} className="grid gap-4 sm:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">Type</label>
              <select
                value={newType}
                onChange={(e) => setNewType(e.target.value as SupplierType)}
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              >
                {types.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2">
              <label className="mb-1 block text-sm font-medium text-slate-300">
                Display name
              </label>
              <input
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                required
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              />
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

      <Card className="p-0 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-900/80 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-5 py-3">Type</th>
              <th className="px-5 py-3">Name</th>
              <th className="px-5 py-3">Status</th>
              <th className="px-5 py-3">Created</th>
              <th className="px-5 py-3" />
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={5} className="px-5 py-8 text-center text-slate-500">
                  Loading…
                </td>
              </tr>
            ) : suppliers.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-5 py-8 text-center text-slate-500">
                  No suppliers yet.
                </td>
              </tr>
            ) : (
              suppliers.map((s) => (
                <tr key={s.id} className="border-t border-slate-800">
                  <td className="px-5 py-3 font-mono text-xs">{s.supplierType}</td>
                  <td className="px-5 py-3">{s.displayName}</td>
                  <td className="px-5 py-3">
                    <button
                      type="button"
                      onClick={() => toggleActive(s)}
                      className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        s.isActive
                          ? "bg-emerald-500/20 text-emerald-300"
                          : "bg-slate-800 text-slate-400"
                      }`}
                    >
                      {s.isActive ? "Active" : "Inactive"}
                    </button>
                  </td>
                  <td className="px-5 py-3 text-slate-500">
                    {new Date(s.createdAtUtc).toLocaleDateString()}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button
                      type="button"
                      onClick={() => remove(s)}
                      className="text-xs text-red-400 hover:text-red-300"
                    >
                      Delete
                    </button>
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
