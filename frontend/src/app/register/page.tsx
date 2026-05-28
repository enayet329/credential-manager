"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { Auth } from "@/lib/endpoints";
import { setStoredAuth } from "@/lib/auth";
import { describeError } from "@/lib/problem";

export default function RegisterPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [workspace, setWorkspace] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    setError(null);
    try {
      const auth = await Auth.register({
        email,
        password,
        workspaceName: workspace || undefined,
      });
      setStoredAuth(auth);
      router.push("/dashboard");
    } catch (err) {
      setError(describeError(err, "Could not create your account."));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="relative min-h-screen overflow-hidden bg-slate-950">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-50"
        style={{
          background:
            "radial-gradient(50% 40% at 50% 0%, rgba(56,189,248,0.18) 0%, rgba(56,189,248,0) 60%)",
        }}
      />

      <div className="mx-auto flex min-h-screen max-w-md flex-col items-stretch justify-center px-4 py-10 sm:px-6 sm:py-12">
        <Link href="/" className="mb-8 flex items-center gap-2 text-lg font-semibold">
          <span className="grid h-9 w-9 place-items-center rounded-md bg-emerald-500/20 text-emerald-300">
            CV
          </span>
          CredVault
        </Link>

        <div className="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 shadow-2xl backdrop-blur sm:p-8">
          <h1 className="text-2xl font-semibold">Create your account</h1>
          <p className="mt-1 text-sm text-slate-400">
            We&apos;ll spin up a personal workspace so you have somewhere to store your
            first credential. You can invite teammates later.
          </p>

          <form onSubmit={onSubmit} className="mt-7 space-y-4">
            <Field
              id="email"
              label="Work email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={setEmail}
              required
              placeholder="you@example.com"
            />
            <Field
              id="password"
              label="Password"
              hint="At least 8 characters."
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={setPassword}
              required
              minLength={8}
              placeholder="••••••••"
            />
            <Field
              id="workspace"
              label="Workspace name"
              hint="Optional — defaults to “your-name’s workspace”."
              type="text"
              value={workspace}
              onChange={setWorkspace}
              placeholder="Acme Inc."
            />

            {error && (
              <p className="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
                {error}
              </p>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="w-full rounded-md bg-emerald-500 px-4 py-2.5 text-sm font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:opacity-60"
            >
              {submitting ? "Creating account…" : "Create account"}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-500">
            Already have an account?{" "}
            <Link href="/login" className="text-emerald-300 hover:text-emerald-200">
              Sign in
            </Link>
          </p>
        </div>

        <p className="mt-6 text-center text-xs text-slate-600">
          By creating an account you agree to keep your master key safe. CredVault
          can&apos;t recover encrypted data if you lose it.
        </p>
      </div>
    </main>
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
  placeholder?: string;
  minLength?: number;
  hint?: string;
}

function Field({
  id,
  label,
  type,
  value,
  onChange,
  autoComplete,
  required,
  placeholder,
  minLength,
  hint,
}: FieldProps) {
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-slate-300">
        {label}
      </label>
      <input
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        autoComplete={autoComplete}
        required={required}
        placeholder={placeholder}
        minLength={minLength}
        className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-sm text-slate-100 placeholder:text-slate-600 focus:border-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
      />
      {hint && <p className="mt-1 text-xs text-slate-500">{hint}</p>}
    </div>
  );
}
