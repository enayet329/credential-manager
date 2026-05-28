import Link from "next/link";

export default function LandingPage() {
  return (
    <main className="min-h-screen flex flex-col">
      <header className="border-b border-slate-800/60 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link href="/" className="flex items-center gap-2 text-lg font-semibold">
            <span className="grid h-8 w-8 place-items-center rounded-md bg-emerald-500/20 text-emerald-300">
              CV
            </span>
            CredVault
          </Link>
          <nav className="flex items-center gap-3">
            <Link
              href="/login"
              className="rounded-md border border-slate-700 px-4 py-2 text-sm font-medium hover:border-slate-500 hover:bg-slate-800"
            >
              Sign in
            </Link>
          </nav>
        </div>
      </header>

      <section className="flex-1">
        <div className="mx-auto max-w-6xl px-6 py-20 lg:py-28">
          <div className="grid items-center gap-12 lg:grid-cols-2">
            <div>
              <p className="inline-flex items-center gap-2 rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-xs font-medium text-emerald-300">
                v0.1 · self-hostable
              </p>
              <h1 className="mt-6 text-4xl font-semibold leading-tight sm:text-5xl">
                The team secrets manager that
                <span className="text-emerald-400">
                  &nbsp;doesn&apos;t get in your way.
                </span>
              </h1>
              <p className="mt-6 text-lg leading-relaxed text-slate-300">
                Encrypted credentials, dynamic supplier schemas, CLI-first DX, and an
                audit log that meets SOC 2. Everything your developers need, nothing
                they don&apos;t.
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <Link
                  href="/login"
                  className="rounded-md bg-emerald-500 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-emerald-400"
                >
                  Sign in to your vault
                </Link>
                <Link
                  href="https://github.com/anthropics"
                  className="rounded-md border border-slate-700 px-5 py-3 text-sm font-semibold hover:border-slate-500 hover:bg-slate-800"
                >
                  Read the docs
                </Link>
              </div>
            </div>
            <Feature />
          </div>
        </div>
      </section>

      <footer className="border-t border-slate-800/60 bg-slate-950">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-6 text-sm text-slate-500">
          <span>© CredVault</span>
          <span>Built on .NET 10 + Next.js</span>
        </div>
      </footer>
    </main>
  );
}

function Feature() {
  const items: { title: string; body: string }[] = [
    {
      title: "Envelope encryption",
      body: "AES-256-GCM with per-credential data keys wrapped by a versioned KEK.",
    },
    {
      title: "Dynamic supplier schemas",
      body: "Add new vendors at runtime without redeploying — the UI renders the form for you.",
    },
    {
      title: "Audit-grade history",
      body: "Every read, rotation, and access grant lands in an append-only log.",
    },
    {
      title: "CLI-first developer flow",
      body: "credvault auth → credvault env exec — secrets land in your shell, never on disk.",
    },
  ];

  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl">
      <ul className="grid gap-5">
        {items.map((item) => (
          <li key={item.title} className="flex gap-4">
            <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-emerald-400" aria-hidden />
            <div>
              <p className="font-semibold text-slate-100">{item.title}</p>
              <p className="text-sm text-slate-400">{item.body}</p>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
