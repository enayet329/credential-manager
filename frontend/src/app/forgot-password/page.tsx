"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { Auth } from "@/lib/endpoints";
import { describeError } from "@/lib/problem";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    setError(null);
    try {
      await Auth.forgotPassword(email);
      setSent(true);
    } catch (err: unknown) {
      // The backend always returns 204 anyway, so the only failure path here is a
      // network outage. Show a generic message rather than describe the error.
      setError(describeError(err, "Could not reach the server. Try again."));
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
          {sent ? (
            <>
              <h1 className="text-2xl font-semibold">Check your inbox</h1>
              <p className="mt-2 text-sm text-slate-400">
                If an account exists for <span className="font-medium text-slate-200">{email}</span>,
                we&apos;ve sent you a reset link. It&apos;s valid for the next 60 minutes.
              </p>
              <p className="mt-4 text-xs text-slate-500">
                Didn&apos;t get it? Check spam, then{" "}
                <button
                  type="button"
                  className="text-emerald-300 underline-offset-4 hover:underline"
                  onClick={() => setSent(false)}
                >
                  try again
                </button>
                .
              </p>
              <Link
                href="/login"
                className="mt-8 block w-full rounded-md border border-slate-700 px-4 py-2.5 text-center text-sm font-semibold text-slate-100 transition hover:border-emerald-500/50 hover:bg-emerald-500/5"
              >
                Back to sign in
              </Link>
            </>
          ) : (
            <>
              <h1 className="text-2xl font-semibold">Forgot your password?</h1>
              <p className="mt-1 text-sm text-slate-400">
                Enter the email on your account and we&apos;ll send you a link to reset it.
              </p>

              <form onSubmit={onSubmit} className="mt-7 space-y-4">
                <div>
                  <label
                    htmlFor="email"
                    className="mb-1.5 block text-sm font-medium text-slate-300"
                  >
                    Email
                  </label>
                  <input
                    id="email"
                    type="email"
                    autoComplete="email"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="you@example.com"
                    className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-sm text-slate-100 placeholder:text-slate-600 focus:border-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
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
                  {submitting ? "Sending…" : "Send reset link"}
                </button>
              </form>

              <p className="mt-6 text-center text-sm text-slate-500">
                Remembered it?{" "}
                <Link href="/login" className="text-emerald-300 hover:underline">
                  Back to sign in
                </Link>
              </p>
            </>
          )}
        </div>
      </div>
    </main>
  );
}
