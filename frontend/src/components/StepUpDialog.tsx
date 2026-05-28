"use client";

import { useState, type FormEvent } from "react";
import { Auth } from "@/lib/endpoints";
import { describeError } from "@/lib/problem";

interface Props {
  open: boolean;
  onResolved: (stepUpToken: string) => void;
  onCancel: () => void;
}

export function StepUpDialog({ open, onResolved, onCancel }: Props) {
  const [code, setCode] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!open) return null;

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const resp = await Auth.stepUp(code);
      onResolved(resp.stepUpToken);
      setCode("");
    } catch (err) {
      setError(describeError(err, "Invalid MFA code."));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-slate-950/80 backdrop-blur">
      <div className="w-full max-w-sm rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
        <h2 className="text-lg font-semibold">Multi-factor step-up</h2>
        <p className="mt-1 text-sm text-slate-400">
          Enter your 6-digit MFA code to authorize this action. Step-up tokens are
          valid for 5 minutes.
        </p>
        <form onSubmit={onSubmit} className="mt-5 space-y-3">
          <input
            type="text"
            inputMode="numeric"
            pattern="\d*"
            placeholder="123456"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-center text-lg tracking-widest text-slate-100 focus:border-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
            required
          />
          {error && (
            <p className="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
              {error}
            </p>
          )}
          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onCancel}
              className="rounded-md border border-slate-700 px-4 py-2 text-sm font-medium hover:border-slate-500"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting || code.length < 6}
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
            >
              {submitting ? "Verifying…" : "Verify"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
