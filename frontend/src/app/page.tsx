import Link from "next/link";

export default function LandingPage() {
  return (
    <main className="relative min-h-screen overflow-hidden bg-slate-950 text-slate-100">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-60"
        style={{
          background:
            "radial-gradient(60% 50% at 50% 0%, rgba(16,185,129,0.18) 0%, rgba(16,185,129,0) 60%), radial-gradient(40% 30% at 100% 100%, rgba(56,189,248,0.12) 0%, rgba(56,189,248,0) 60%)",
        }}
      />

      <header className="border-b border-slate-800/60 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link href="/" className="flex items-center gap-2 text-lg font-semibold">
            <span className="grid h-8 w-8 place-items-center rounded-md bg-emerald-500/20 text-emerald-300">
              CV
            </span>
            CredVault
          </Link>
          <nav className="flex items-center gap-2">
            <Link
              href="/login"
              className="rounded-md px-4 py-2 text-sm font-medium text-slate-300 hover:text-white"
            >
              Sign in
            </Link>
            <Link
              href="/register"
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-emerald-400"
            >
              Get started
            </Link>
          </nav>
        </div>
      </header>

      <section className="mx-auto max-w-6xl px-6 py-20 lg:py-28">
        <div className="grid items-center gap-16 lg:grid-cols-2">
          <div>
            <p className="inline-flex items-center gap-2 rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-xs font-medium text-emerald-300">
              <span className="h-1.5 w-1.5 rounded-full bg-emerald-400" />
              v0.1 · self-hostable
            </p>
            <h1 className="mt-6 text-balance text-4xl font-semibold leading-tight sm:text-5xl lg:text-6xl">
              The team secrets manager that
              <span className="bg-gradient-to-r from-emerald-300 to-sky-400 bg-clip-text text-transparent">
                {" "}doesn&apos;t get in your way.
              </span>
            </h1>
            <p className="mt-6 max-w-xl text-lg leading-relaxed text-slate-300">
              Encrypted credentials, dynamic supplier schemas, CLI-first DX, and an
              audit log that meets SOC 2. Everything your developers need, nothing
              they don&apos;t.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <Link
                href="/register"
                className="group inline-flex items-center gap-2 rounded-md bg-emerald-500 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-emerald-400"
              >
                Create your free account
                <span className="transition group-hover:translate-x-0.5">→</span>
              </Link>
              <Link
                href="/login"
                className="inline-flex items-center gap-2 rounded-md border border-slate-700 bg-slate-900/50 px-5 py-3 text-sm font-semibold text-slate-100 transition hover:border-slate-500 hover:bg-slate-800/60"
              >
                I already have an account
              </Link>
            </div>
            <p className="mt-4 text-xs text-slate-500">
              Free to self-host · no credit card · roll your own KEK
            </p>
          </div>

          <Feature />
        </div>
      </section>

      <section className="border-t border-slate-800/60 bg-slate-950/60">
        <div className="mx-auto max-w-6xl px-6 py-16">
          <h2 className="text-2xl font-semibold">How it works</h2>
          <div className="mt-8 grid gap-6 sm:grid-cols-3">
            <Step
              n={1}
              title="Create your workspace"
              body="Sign up and we automatically spin up a personal organisation for you. Invite teammates by email later."
            />
            <Step
              n={2}
              title="Add a supplier + a credential"
              body="OpenAI, Stripe, AWS, your own custom type — the form fields are rendered from a schema you control."
            />
            <Step
              n={3}
              title="Share securely"
              body="Generate a signed link with a permission and expiry. Every reveal is audit-logged and rate-limited."
            />
          </div>
        </div>
      </section>

      <footer className="border-t border-slate-800/60 bg-slate-950">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-3 px-6 py-6 text-sm text-slate-500 sm:flex-row">
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
    <div className="relative rounded-2xl border border-slate-800 bg-slate-900/60 p-7 shadow-2xl">
      <div
        aria-hidden
        className="absolute inset-x-7 -top-px h-px bg-gradient-to-r from-transparent via-emerald-400/40 to-transparent"
      />
      <ul className="grid gap-5">
        {items.map((item) => (
          <li key={item.title} className="flex gap-4">
            <span className="mt-1 grid h-5 w-5 shrink-0 place-items-center rounded-full bg-emerald-500/20 text-[10px] text-emerald-300">
              ✓
            </span>
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

function Step({ n, title, body }: { n: number; title: string; body: string }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-6 transition hover:border-slate-700">
      <span className="grid h-8 w-8 place-items-center rounded-full bg-slate-950 font-mono text-sm text-emerald-300">
        {n}
      </span>
      <h3 className="mt-4 text-lg font-semibold">{title}</h3>
      <p className="mt-2 text-sm text-slate-400">{body}</p>
    </div>
  );
}
