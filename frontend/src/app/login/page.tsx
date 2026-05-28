"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import axios, { AxiosError } from "axios";
import { api } from "@/lib/api";
import { setStoredAuth } from "@/lib/auth";
import type { ApiProblem, LoginResponse } from "@/lib/types";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    setError(null);

    try {
      const { data } = await api.post<LoginResponse>("/auth/login", { email, password });
      setStoredAuth(data);
      router.push("/dashboard");
    } catch (err: unknown) {
      let message = "Could not sign in. Check your email and password.";
      if (axios.isAxiosError(err)) {
        const axiosError = err as AxiosError<ApiProblem>;
        if (axiosError.response?.data?.detail) {
          message = axiosError.response.data.detail;
        } else if (axiosError.response?.data?.title) {
          message = axiosError.response.data.title;
        } else if (!axiosError.response) {
          message = "Cannot reach the API. Is the backend running?";
        }
      }
      setError(message);
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
          <h1 className="text-2xl font-semibold">Welcome back</h1>
          <p className="mt-1 text-sm text-slate-400">
            Sign in to your vault to manage credentials.
          </p>

          <form onSubmit={onSubmit} className="mt-7 space-y-4">
            <Field
              id="email"
              label="Email"
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
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={setPassword}
              required
              placeholder="••••••••"
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
              {submitting ? "Signing in…" : "Sign in"}
            </button>

            <p className="text-center text-xs">
              <Link
                href="/forgot-password"
                className="text-slate-400 underline-offset-4 hover:text-emerald-300 hover:underline"
              >
                Forgot your password?
              </Link>
            </p>
          </form>

          <div className="my-6 flex items-center gap-3 text-xs text-slate-600">
            <div className="h-px flex-1 bg-slate-800" />
            new here?
            <div className="h-px flex-1 bg-slate-800" />
          </div>

          <Link
            href="/register"
            className="block w-full rounded-md border border-slate-700 px-4 py-2.5 text-center text-sm font-semibold text-slate-100 transition hover:border-emerald-500/50 hover:bg-emerald-500/5"
          >
            Create your account
          </Link>
        </div>

        <p className="mt-6 text-center text-sm text-slate-500">
          <Link href="/" className="hover:text-slate-300">
            ← Back to home
          </Link>
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
}

function Field({
  id,
  label,
  type,
  value,
  autoComplete,
  required,
  placeholder,
  onChange,
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
        autoComplete={autoComplete}
        required={required}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-sm text-slate-100 placeholder:text-slate-600 focus:border-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30"
      />
    </div>
  );
}
