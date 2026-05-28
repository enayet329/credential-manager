"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { PageHeader } from "@/components/PageHeader";
import { Members } from "@/lib/endpoints";
import { useOrg } from "@/lib/org-context";
import { describeError } from "@/lib/problem";
import type { MemberDto, Role } from "@/lib/types";

const roleOptions: Role[] = ["Viewer", "Developer", "Admin", "Owner"];

export default function MembersPage() {
  const { org, permissions } = useOrg();
  const canManage = permissions.includes("admin:schemas");

  const [members, setMembers] = useState<MemberDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [showInvite, setShowInvite] = useState(false);
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<Role>("Developer");
  const [initialPassword, setInitialPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [issuedPassword, setIssuedPassword] = useState<string | null>(null);

  async function refresh() {
    if (!org) return;
    setLoading(true);
    try {
      setMembers(await Members.list(org.slug));
      setError(null);
    } catch (err) {
      setError(describeError(err, "Failed to load members."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [org?.slug]);

  async function invite(event: FormEvent) {
    event.preventDefault();
    if (submitting || !org) return;
    setSubmitting(true);
    try {
      const resp = await Members.invite(org.slug, {
        email,
        role,
        initialPassword: initialPassword || undefined,
      });
      setIssuedPassword(resp.temporaryPassword ?? null);
      setEmail("");
      setInitialPassword("");
      setShowInvite(false);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to invite member."));
    } finally {
      setSubmitting(false);
    }
  }

  async function changeRole(userId: string, newRole: Role) {
    if (!org) return;
    try {
      await Members.updateRole(org.slug, userId, newRole);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to change role."));
    }
  }

  async function remove(member: MemberDto) {
    if (!org) return;
    if (!confirm(`Remove ${member.email} from ${org.name}?`)) return;
    try {
      await Members.remove(org.slug, member.userId);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to remove member."));
    }
  }

  return (
    <>
      <PageHeader
        title="Members"
        description={
          canManage
            ? "Invite teammates and assign roles. Roles map directly to permission claims on each user's JWT."
            : "Read-only — your role doesn't let you manage members."
        }
        actions={
          canManage ? (
            <button
              type="button"
              onClick={() => setShowInvite(true)}
              className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-400"
            >
              Invite member
            </button>
          ) : null
        }
      />

      {error && (
        <div className="mb-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </div>
      )}

      {issuedPassword && (
        <Card className="mb-6 border-emerald-500/30 bg-emerald-500/10">
          <p className="text-sm font-semibold text-emerald-200">
            Temporary password (shown once):
          </p>
          <p className="mt-1 font-mono text-lg text-emerald-100">{issuedPassword}</p>
          <button
            type="button"
            onClick={() => setIssuedPassword(null)}
            className="mt-3 text-xs text-emerald-200 underline"
          >
            Dismiss
          </button>
        </Card>
      )}

      {showInvite && (
        <Card className="mb-6">
          <form onSubmit={invite} className="grid gap-4 sm:grid-cols-3">
            <div className="sm:col-span-2">
              <label className="mb-1 block text-sm font-medium text-slate-300">Email</label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-300">Role</label>
              <select
                value={role}
                onChange={(e) => setRole(e.target.value as Role)}
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
              >
                {roleOptions.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-3">
              <label className="mb-1 block text-sm font-medium text-slate-300">
                Initial password (optional)
              </label>
              <input
                type="text"
                value={initialPassword}
                onChange={(e) => setInitialPassword(e.target.value)}
                placeholder="Leave blank to auto-generate"
                className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm font-mono"
              />
              <p className="mt-1 text-xs text-slate-500">
                Only used when creating a new user. Existing users keep their password.
              </p>
            </div>
            <div className="sm:col-span-3 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setShowInvite(false)}
                className="rounded-md border border-slate-700 px-4 py-2 text-sm hover:border-slate-500"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
              >
                {submitting ? "Inviting…" : "Invite"}
              </button>
            </div>
          </form>
        </Card>
      )}

      <Card className="p-0 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-900/80 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-5 py-3">Email</th>
              <th className="px-5 py-3">Role</th>
              <th className="px-5 py-3">Joined</th>
              <th className="px-5 py-3" />
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={4} className="px-5 py-8 text-center text-slate-500">
                  Loading…
                </td>
              </tr>
            ) : members.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-5 py-8 text-center text-slate-500">
                  No members.
                </td>
              </tr>
            ) : (
              members.map((m) => (
                <tr key={m.userId} className="border-t border-slate-800">
                  <td className="px-5 py-3">
                    {m.email}
                    {!m.emailConfirmed && (
                      <span className="ml-2 rounded-full bg-amber-500/20 px-2 py-0.5 text-xs text-amber-300">
                        unconfirmed
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3">
                    {canManage ? (
                      <select
                        value={m.role}
                        onChange={(e) => changeRole(m.userId, e.target.value as Role)}
                        className="rounded-md border border-slate-700 bg-slate-950 px-2 py-1 text-xs"
                      >
                        {roleOptions.map((r) => (
                          <option key={r} value={r}>
                            {r}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <span className="font-mono text-xs">{m.role}</span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-xs text-slate-500">
                    {new Date(m.joinedAtUtc).toLocaleDateString()}
                  </td>
                  <td className="px-5 py-3 text-right">
                    {canManage && (
                      <button
                        type="button"
                        onClick={() => remove(m)}
                        className="text-xs text-red-400 hover:text-red-300"
                      >
                        Remove
                      </button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </Card>
    </>
  );
}
