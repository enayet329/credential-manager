"use client";

import { AppShell } from "@/components/AppShell";
import { useRequireAuth } from "@/lib/hooks";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { auth, loading } = useRequireAuth();
  if (loading || !auth) {
    return (
      <div className="min-h-screen grid place-items-center text-slate-400">
        Checking your session…
      </div>
    );
  }
  return <AppShell auth={auth}>{children}</AppShell>;
}
