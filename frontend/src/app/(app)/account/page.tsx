"use client";

import { useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Auth } from "@/lib/endpoints";
import { getStoredAuth } from "@/lib/auth";
import { describeError } from "@/lib/problem";

export default function AccountPage() {
  const auth = getStoredAuth();

  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setError(null);
    setSuccess(false);
    if (next !== confirm) {
      setError("New password and confirmation don't match.");
      return;
    }
    setSubmitting(true);
    try {
      await Auth.changePassword({ currentPassword: current, newPassword: next });
      setSuccess(true);
      setCurrent("");
      setNext("");
      setConfirm("");
    } catch (err) {
      setError(describeError(err, "Failed to change password."));
    } finally {
      setSubmitting(false);
    }
  }

  if (!auth) return null;

  return (
    <>
      <PageHeader
        title="Your account"
        description="Update your password. Sessions stay active until they expire — sign out and back in to refresh permissions."
      />

      <div className="grid gap-6 sm:grid-cols-2">
        <Card>
          <h3 className="text-sm font-semibold text-slate-300">Profile</h3>
          <dl className="mt-4 grid grid-cols-3 gap-y-3 text-sm">
            <dt className="text-slate-500">Email</dt>
            <dd className="col-span-2 font-mono">{auth.email}</dd>
            <dt className="text-slate-500">User id</dt>
            <dd className="col-span-2 font-mono text-xs">{auth.userId}</dd>
            <dt className="text-slate-500">Orgs</dt>
            <dd className="col-span-2">
              {auth.organizations.length} (
              {auth.organizations.map((o) => o.name).join(", ")})
            </dd>
          </dl>
        </Card>

        <Card>
          <h3 className="text-sm font-semibold text-slate-300">Change password</h3>
          <form onSubmit={onSubmit} className="mt-4 space-y-4">
            <Field
              id="current"
              label="Current password"
              type="password"
              autoComplete="current-password"
              value={current}
              onChange={setCurrent}
              required
            />
            <Field
              id="next"
              label="New password (8+ characters)"
              type="password"
              autoComplete="new-password"
              value={next}
              onChange={setNext}
              required
              minLength={8}
            />
            <Field
              id="confirm"
              label="Confirm new password"
              type="password"
              autoComplete="new-password"
              value={confirm}
              onChange={setConfirm}
              required
              minLength={8}
            />

            {error && (
              <p className="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
                {error}
              </p>
            )}
            {success && (
              <p className="rounded-md border border-emerald-500/30 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-200">
                Password changed.
              </p>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
            >
              {submitting ? "Updating…" : "Update password"}
            </button>
          </form>
        </Card>
      </div>
    </>
  );
}

interface FieldProps {
  id: string;
  label: string;
  type: string;
  value: string;
  onChange: (v: string) => void;
  autoComplete?: string;
  required?: boolean;
  minLength?: number;
}

function Field({
  id,
  label,
  type,
  value,
  onChange,
  autoComplete,
  required,
  minLength,
}: FieldProps) {
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-sm font-medium text-slate-300">
        {label}
      </label>
      <input
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        autoComplete={autoComplete}
        required={required}
        minLength={minLength}
        className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
      />
    </div>
  );
}
