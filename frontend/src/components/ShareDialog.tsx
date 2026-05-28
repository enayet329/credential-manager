"use client";

import { useState, type FormEvent } from "react";
import { Credentials } from "@/lib/endpoints";
import { useOrg } from "@/lib/org-context";
import { describeError } from "@/lib/problem";

interface Props {
  credentialId: string;
  open: boolean;
  onClose: () => void;
}

export function ShareDialog({ credentialId, open, onClose }: Props) {
  const { org } = useOrg();
  const [expiresInMinutes, setExpiresInMinutes] = useState(60);
  const [allowReveal, setAllowReveal] = useState(true);
  const [recipientEmail, setRecipientEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<{ url: string; expiresAtUtc: string; emailed: boolean } | null>(null);

  if (!open) return null;

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!org) return;
    setBusy(true);
    setError(null);
    try {
      const resp = await Credentials.share(org.slug, credentialId, {
        expiresInMinutes,
        allowReveal,
        recipientEmail: recipientEmail || undefined,
      });
      setResult({
        url: resp.shareUrl,
        expiresAtUtc: resp.expiresAtUtc,
        emailed: !!recipientEmail,
      });
    } catch (err) {
      setError(describeError(err, "Failed to create share link."));
    } finally {
      setBusy(false);
    }
  }

  function close() {
    setResult(null);
    setError(null);
    setRecipientEmail("");
    onClose();
  }

  return (
    <div className="fixed inset-0 z-40 grid place-items-center bg-slate-950/80 backdrop-blur">
      <div className="w-full max-w-lg rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
        <h2 className="text-lg font-semibold">Share this credential</h2>
        <p className="mt-1 text-sm text-slate-400">
          Generate a signed link that anyone can open. You choose how long it lasts and whether the
          recipient can see the value or just the metadata.
        </p>

        {result ? (
          <div className="mt-5 space-y-3">
            <p className="text-sm text-emerald-300">
              ✓ Link created. Valid until {new Date(result.expiresAtUtc).toLocaleString()}.
              {result.emailed && " A copy was sent to the recipient by email."}
            </p>
            <label className="block text-sm font-medium text-slate-300">Share URL</label>
            <div className="flex gap-2">
              <input
                readOnly
                value={result.url}
                className="flex-1 rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-xs font-mono"
                onClick={(e) => (e.target as HTMLInputElement).select()}
              />
              <button
                type="button"
                onClick={() => navigator.clipboard.writeText(result.url)}
                className="rounded-md border border-slate-700 px-3 py-2 text-xs hover:border-slate-500"
              >
                Copy
              </button>
            </div>
            <div className="flex justify-end pt-3">
              <button
                type="button"
                onClick={close}
                className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950"
              >
                Done
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={submit} className="mt-5 space-y-4">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">
                Link expires in
              </label>
              <select
                value={expiresInMinutes}
                onChange={(e) => setExpiresInMinutes(Number(e.target.value))}
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              >
                <option value={15}>15 minutes</option>
                <option value={60}>1 hour</option>
                <option value={240}>4 hours</option>
                <option value={1440}>1 day</option>
                <option value={10080}>7 days (max)</option>
              </select>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">
                What can the recipient do?
              </label>
              <div className="grid grid-cols-2 gap-2">
                <button
                  type="button"
                  onClick={() => setAllowReveal(true)}
                  className={`rounded-md border px-3 py-2 text-sm transition ${
                    allowReveal
                      ? "border-emerald-500/50 bg-emerald-500/10 text-emerald-200"
                      : "border-slate-700 text-slate-300 hover:border-slate-500"
                  }`}
                >
                  See the value
                </button>
                <button
                  type="button"
                  onClick={() => setAllowReveal(false)}
                  className={`rounded-md border px-3 py-2 text-sm transition ${
                    !allowReveal
                      ? "border-emerald-500/50 bg-emerald-500/10 text-emerald-200"
                      : "border-slate-700 text-slate-300 hover:border-slate-500"
                  }`}
                >
                  Metadata only
                </button>
              </div>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">
                Email it to someone (optional)
              </label>
              <input
                type="email"
                value={recipientEmail}
                onChange={(e) => setRecipientEmail(e.target.value)}
                placeholder="recipient@example.com"
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              />
              <p className="mt-1 text-xs text-slate-500">
                Leave blank to just get the URL on screen.
              </p>
            </div>

            {error && (
              <p className="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
                {error}
              </p>
            )}

            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={close}
                className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={busy}
                className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
              >
                {busy ? "Creating…" : "Create link"}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
