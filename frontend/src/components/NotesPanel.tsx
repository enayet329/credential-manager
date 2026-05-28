"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Card } from "@/components/Card";
import { Notes } from "@/lib/endpoints";
import { useOrg } from "@/lib/org-context";
import { describeError } from "@/lib/problem";
import type { CredentialNoteDto } from "@/lib/types";

interface Props {
  credentialId: string;
  /** True when the viewer holds credentials:read:value — otherwise hide the panel. */
  canRead: boolean;
}

/**
 * Encrypted runbook-style notes attached to a credential. Listing returns DECRYPTED content,
 * so only show this to people with credentials:read:value.
 */
export function NotesPanel({ credentialId, canRead }: Props) {
  const { org } = useOrg();
  const [notes, setNotes] = useState<CredentialNoteDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [content, setContent] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const refresh = useCallback(async () => {
    if (!org || !canRead) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      setNotes(await Notes.list(org.slug, credentialId));
      setError(null);
    } catch (err) {
      setError(describeError(err, "Failed to load notes."));
    } finally {
      setLoading(false);
    }
  }, [org, canRead, credentialId]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  async function add(event: FormEvent) {
    event.preventDefault();
    if (!org || submitting || !content.trim()) return;
    setSubmitting(true);
    try {
      await Notes.create(org.slug, credentialId, content);
      setContent("");
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to add note."));
    } finally {
      setSubmitting(false);
    }
  }

  async function remove(note: CredentialNoteDto) {
    if (!org) return;
    if (!confirm("Delete this note?")) return;
    try {
      await Notes.remove(org.slug, credentialId, note.id);
      await refresh();
    } catch (err) {
      setError(describeError(err, "Failed to delete note."));
    }
  }

  if (!canRead) {
    return (
      <Card>
        <h3 className="text-sm font-semibold text-slate-300">Notes</h3>
        <p className="mt-3 text-sm text-slate-500">
          Notes are encrypted alongside the credential. Your current role doesn&apos;t
          let you read them.
        </p>
      </Card>
    );
  }

  return (
    <Card>
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-300">Notes</h3>
        <span className="text-xs text-slate-500">{notes.length}</span>
      </div>
      <p className="mt-1 text-xs text-slate-500">
        Runbook-style content (rotation steps, gotchas). Encrypted with the same envelope as the credential.
      </p>

      {error && (
        <p className="mt-3 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
          {error}
        </p>
      )}

      <form onSubmit={add} className="mt-4 space-y-2">
        <textarea
          rows={3}
          value={content}
          onChange={(e) => setContent(e.target.value)}
          placeholder="To rotate this, log into the OpenAI dashboard…"
          className="block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm"
        />
        <div className="flex justify-end">
          <button
            type="submit"
            disabled={submitting || !content.trim()}
            className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
          >
            {submitting ? "Saving…" : "+ Add note"}
          </button>
        </div>
      </form>

      <ul className="mt-5 space-y-3">
        {loading ? (
          <li className="text-sm text-slate-500">Loading…</li>
        ) : notes.length === 0 ? (
          <li className="text-sm text-slate-500">No notes yet.</li>
        ) : (
          notes.map((n) => (
            <li
              key={n.id}
              className="rounded-md border border-slate-800 bg-slate-950 p-3"
            >
              <p className="whitespace-pre-wrap text-sm">{n.content}</p>
              <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
                <span>{new Date(n.createdAtUtc).toLocaleString()}</span>
                <button
                  type="button"
                  onClick={() => remove(n)}
                  className="text-red-400 hover:text-red-300"
                >
                  Delete
                </button>
              </div>
            </li>
          ))
        )}
      </ul>
    </Card>
  );
}
