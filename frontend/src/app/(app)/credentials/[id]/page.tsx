"use client";
import { useOrg } from "@/lib/org-context";

import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { NotesPanel } from "@/components/NotesPanel";
import { SchemaForm } from "@/components/SchemaForm";
import { ShareDialog } from "@/components/ShareDialog";
import { StepUpDialog } from "@/components/StepUpDialog";
import { getStoredAuth } from "@/lib/auth";
import { Credentials, Schemas } from "@/lib/endpoints";
import { describeError, isStepUpRequired } from "@/lib/problem";
import type {
  CredentialMetadataDto,
  CredentialRotationDto,
  CredentialSchemaDto,
} from "@/lib/types";

export default function CredentialDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const id = params.id;
  const { org } = useOrg();
  const orgSlug = org?.slug ?? "";

  const [meta, setMeta] = useState<CredentialMetadataDto | null>(null);
  const [schema, setSchema] = useState<CredentialSchemaDto | null>(null);
  const [rotations, setRotations] = useState<CredentialRotationDto[]>([]);
  const [value, setValue] = useState<Record<string, string> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [showShare, setShowShare] = useState(false);
  const [showRotate, setShowRotate] = useState(false);
  const [rotateFields, setRotateFields] = useState<Record<string, string>>({});
  const [rotateReason, setRotateReason] = useState("");

  // Step-up state machine
  const [pending, setPending] = useState<((token: string) => Promise<void>) | null>(null);
  const [stepUpOpen, setStepUpOpen] = useState(false);

  const refresh = useCallback(async () => {
    if (!orgSlug || !id) return;
    try {
      const m = await Credentials.get(orgSlug, id);
      setMeta(m);
      const [s, r] = await Promise.all([
        Schemas.get(orgSlug, m.supplierType).catch(() => null),
        Credentials.rotations(orgSlug, id).catch(() => []),
      ]);
      setSchema(s);
      setRotations(r);
      setError(null);
    } catch (err) {
      setError(describeError(err, "Failed to load credential."));
    }
  }, [orgSlug, id]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  function runWithStepUp(action: (token: string) => Promise<void>) {
    setPending(() => action);
    setStepUpOpen(true);
  }

  async function reveal(stepUpToken?: string) {
    setBusy(true);
    setError(null);
    try {
      const resp = await Credentials.value(orgSlug, id, stepUpToken);
      setValue(resp.fields);
    } catch (err) {
      if (!stepUpToken && isStepUpRequired(err)) {
        runWithStepUp(reveal);
      } else {
        setError(describeError(err, "Failed to retrieve value."));
      }
    } finally {
      setBusy(false);
    }
  }

  async function rotate(stepUpToken: string) {
    setBusy(true);
    setError(null);
    try {
      await Credentials.rotate(
        orgSlug,
        id,
        { fields: rotateFields, reason: rotateReason || undefined },
        stepUpToken,
      );
      setShowRotate(false);
      setRotateFields({});
      setRotateReason("");
      setValue(null);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to rotate credential."));
    } finally {
      setBusy(false);
    }
  }

  async function revoke(stepUpToken: string) {
    setBusy(true);
    setError(null);
    try {
      await Credentials.revoke(orgSlug, id, stepUpToken);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to revoke credential."));
    } finally {
      setBusy(false);
    }
  }

  async function remove(stepUpToken: string) {
    setBusy(true);
    setError(null);
    try {
      await Credentials.remove(orgSlug, id, stepUpToken);
      router.push("/credentials");
    } catch (err) {
      setError(describeError(err, "Failed to delete credential."));
      setBusy(false);
    }
  }

  if (!meta) {
    return <p className="text-sm text-slate-500">{error ?? "Loading…"}</p>;
  }

  return (
    <>
      <PageHeader
        title={meta.name}
        description={`${meta.supplierType} · v${meta.credentialSchemaVersion} · KEK v${meta.kekVersion}`}
        actions={
          <>
            <button
              type="button"
              onClick={() => setShowShare(true)}
              disabled={meta.isRevoked}
              className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500 disabled:opacity-50"
            >
              ↗ Share
            </button>
            <button
              type="button"
              onClick={() => reveal()}
              disabled={busy || meta.isRevoked}
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-50"
            >
              Reveal value
            </button>
            <button
              type="button"
              onClick={() => {
                if (!schema) return;
                const next: Record<string, string> = {};
                for (const f of schema.fields) next[f.key] = "";
                setRotateFields(next);
                setShowRotate(true);
              }}
              disabled={busy || meta.isRevoked || !schema}
              className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500"
            >
              Rotate
            </button>
            {!meta.isRevoked && (
              <button
                type="button"
                onClick={() => {
                  if (confirm("Revoke this credential? It will remain in the audit log but cannot be decrypted again.")) {
                    runWithStepUp(revoke);
                  }
                }}
                className="rounded-md border border-red-500/40 px-4 py-2 text-sm text-red-300 hover:bg-red-500/10"
              >
                Revoke
              </button>
            )}
          </>
        }
      />

      <p className="mb-6 font-mono text-xs text-slate-500">{meta.slug}</p>

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <h3 className="text-sm font-semibold text-slate-300">Metadata</h3>
          <dl className="mt-4 grid grid-cols-2 gap-y-3 text-sm">
            <dt className="text-slate-500">Masked preview</dt>
            <dd className="font-mono">{meta.maskedPreview}</dd>
            <dt className="text-slate-500">Created</dt>
            <dd>{new Date(meta.createdAtUtc).toLocaleString()}</dd>
            <dt className="text-slate-500">Rotated</dt>
            <dd>{new Date(meta.rotatedAtUtc).toLocaleString()}</dd>
            <dt className="text-slate-500">Expires</dt>
            <dd>
              {meta.expiresAtUtc ? new Date(meta.expiresAtUtc).toLocaleString() : "—"}
            </dd>
            <dt className="text-slate-500">Last access</dt>
            <dd>
              {meta.lastAccessedAtUtc
                ? new Date(meta.lastAccessedAtUtc).toLocaleString()
                : "—"}
            </dd>
            <dt className="text-slate-500">Access count</dt>
            <dd>{meta.accessCount}</dd>
            <dt className="text-slate-500">Status</dt>
            <dd>
              <span
                className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  meta.isRevoked
                    ? "bg-red-500/20 text-red-300"
                    : "bg-emerald-500/20 text-emerald-300"
                }`}
              >
                {meta.isRevoked ? "Revoked" : "Active"}
              </span>
            </dd>
          </dl>

          <div className="mt-6 border-t border-slate-800 pt-4">
            <button
              type="button"
              onClick={() => {
                if (confirm("Permanently delete this credential row?")) runWithStepUp(remove);
              }}
              className="text-xs text-red-400 hover:text-red-300"
            >
              Delete credential
            </button>
          </div>
        </Card>

        <Card>
          <h3 className="text-sm font-semibold text-slate-300">Decrypted value</h3>
          {value ? (
            <>
              <dl className="mt-4 space-y-2 text-sm">
                {Object.entries(value).map(([k, v]) => (
                  <div key={k} className="rounded-md border border-slate-800 bg-slate-950 p-3">
                    <p className="text-xs uppercase tracking-wide text-slate-500">{k}</p>
                    <p className="mt-1 break-all font-mono">{v}</p>
                  </div>
                ))}
              </dl>
              <button
                type="button"
                onClick={() => setValue(null)}
                className="mt-4 text-xs text-slate-400 hover:text-slate-200"
              >
                Hide
              </button>
            </>
          ) : (
            <p className="mt-4 text-sm text-slate-400">
              Click <em>Reveal value</em> to decrypt. Each reveal is rate-limited and audit-logged.
            </p>
          )}
        </Card>
      </div>

      <Card className="mt-6">
        <h3 className="text-sm font-semibold text-slate-300">Rotation history</h3>
        {rotations.length === 0 ? (
          <p className="mt-3 text-sm text-slate-500">Never rotated.</p>
        ) : (
          <ul className="mt-4 divide-y divide-slate-800 text-sm">
            {rotations.map((r) => (
              <li key={r.id} className="flex items-center justify-between py-3">
                <div>
                  <p>{new Date(r.rotatedAtUtc).toLocaleString()}</p>
                  <p className="text-xs text-slate-500">
                    {r.reason ?? "no reason"} · previous KEK v{r.previousKekVersion}
                  </p>
                </div>
                <span className="font-mono text-xs text-slate-500">{r.rotatedByUserId.slice(0, 8)}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>

      <div className="mt-6">
        <NotesPanel
          credentialId={id}
          canRead={(getStoredAuth()?.permissions ?? []).includes("credentials:read:value")}
        />
      </div>

      {showRotate && schema && (
        <div className="fixed inset-0 z-40 grid place-items-center bg-slate-950/80 backdrop-blur">
          <div className="w-full max-w-lg rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
            <h2 className="text-lg font-semibold">Rotate credential</h2>
            <p className="mt-1 text-sm text-slate-400">
              The previous payload is snapshotted into the rotation history. Old values cannot be retrieved again.
            </p>
            <div className="mt-5">
              <SchemaForm
                fields={schema.fields}
                values={rotateFields}
                onChange={(k, v) => setRotateFields((prev) => ({ ...prev, [k]: v }))}
                disabled={busy}
              />
              <div className="mt-4">
                <label className="mb-1 block text-sm font-medium text-slate-300">
                  Reason (optional)
                </label>
                <input
                  value={rotateReason}
                  onChange={(e) => setRotateReason(e.target.value)}
                  className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
                />
              </div>
            </div>
            <div className="mt-5 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setShowRotate(false)}
                className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => runWithStepUp(rotate)}
                disabled={busy}
                className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
              >
                Rotate
              </button>
            </div>
          </div>
        </div>
      )}

      <StepUpDialog
        open={stepUpOpen}
        onResolved={async (token) => {
          setStepUpOpen(false);
          if (pending) await pending(token);
          setPending(null);
        }}
        onCancel={() => {
          setStepUpOpen(false);
          setPending(null);
        }}
      />

      <ShareDialog
        credentialId={id}
        open={showShare}
        onClose={() => setShowShare(false)}
      />
    </>
  );
}
