"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useState, type FormEvent } from "react";
import { Auth } from "@/lib/endpoints";
import { describeError } from "@/lib/problem";

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={<Shell><p className="text-sm text-slate-400">Loading…</p></Shell>}>
      <ResetPasswordInner />
    </Suspense>
  );
}

function ResetPasswordInner() {
  const router = useRouter();
  const params = useSearchParams();
  const token = params.get("token") ?? "";

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (submitting) return;

    if (password.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }
    if (password !== confirm) {
      setError("Passwords don't match.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await Auth.resetPassword({ token, newPassword: password });
      setDone(true);
      setTimeout(() => router.push("/login"), 2000);
    } catch (err: unknown) {
      setError(describeError(err, "Could not reset your password."));
    } finally {
      setSubmitting(false);
    }
  }

  if (!token) {
    return (
      <Shell>
        <h1 className="text-2xl font-semibold">Missing reset token</h1>
        <p className="mt-2 text-sm text-slate-400">
          This page needs a <code className="rounded bg-slate-800 px-1.5 py-0.5">?token=…</code>{" "}
          query parameter. Use the link from your password-reset email.
        </p>
        <Link
          href="/forgot-password"
          className="mt-6 inline-flex w-full justify-center rounded-md border border-slate-700 px-4 py-2.5 text-sm font-semibold text-slate-100 transition hover:border-emerald-500/50 hover:bg-emerald-500/5"
        >
          Request a new link
        </Link>
      </Shell>
    );
  }

  if (done) {
    return (
      <Shell>
        <h1 className="text-2xl font-semibold">Password updated</h1>
        <p className="mt-2 text-sm text-slate-400">
          You can now sign in with your new password. Redirecting you to the sign-in page…
        </p>
        <Link
          href="/login"
          className="mt-6 inline-flex w-full justify-center rounded-md bg-emerald-500 px-4 py-2.5 text-sm font-semibold text-slate-950 transition hover:bg-emerald-400"
        >
          Go to sign in
        </Link>
      </Shell>
    );
  }

  return (
    <Shell>
      <h1 className="text-2xl font-semibold">Choose a new password</h1>
      <p className="mt-1 text-sm text-slate-400">
        Pick something at least 8 characters. You&apos;ll be signed out of all other sessions.
      </p>

      <form onSubmit={onSubmit} className="mt-7 space-y-4">
        <div>
          <label htmlFor="pw" className="mb-1.5 block text-sm font-medium text-slate-300">
            New password
          </label>
          <input
            id="pw"
            type="password"
            autoComplete="new-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-sm text-slate-100 focus:border-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
          />
        </div>
        <div>
          <label htmlFor="pw2" className="mb-1.5 block text-sm font-medium text-slate-300">
            Confirm new password
          </label>
          <input
            id="pw2"
            type="password"
            autoComplete="new-password"
            required
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-sm text-slate-100 focus:border-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
          />
        </div>

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
          {submitting ? "Updating…" : "Reset password"}
        </button>
      </form>
    </Shell>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <main className="relative min-h-screen overflow-hidden bg-slate-950">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-50"
        style={{
          background:
            "radial-gradient(50% 40% at 50% 0%, rgba(16,185,129,0.18) 0%, rgba(16,185,129,0) 60%)",
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
          {children}
        </div>
      </div>
    </main>
  );
}
