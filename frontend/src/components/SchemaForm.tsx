"use client";

import { useState } from "react";
import type { CredentialFieldSchema } from "@/lib/types";

interface Props {
  fields: CredentialFieldSchema[];
  values: Record<string, string>;
  onChange: (key: string, value: string) => void;
  disabled?: boolean;
}

export function SchemaForm({ fields, values, onChange, disabled }: Props) {
  return (
    <div className="space-y-4">
      {fields.map((field) => (
        <FieldInput
          key={field.key}
          field={field}
          value={values[field.key] ?? ""}
          onChange={(v) => onChange(field.key, v)}
          disabled={disabled}
        />
      ))}
    </div>
  );
}

function FieldInput({
  field,
  value,
  onChange,
  disabled,
}: {
  field: CredentialFieldSchema;
  value: string;
  onChange: (v: string) => void;
  disabled?: boolean;
}) {
  const [show, setShow] = useState(false);
  const label = (
    <label htmlFor={field.key} className="mb-1 flex items-center justify-between text-sm">
      <span className="font-medium text-slate-300">
        {field.displayName}
        {field.isRequired && <span className="ml-0.5 text-red-400">*</span>}
      </span>
      {field.isSecret && (
        <button
          type="button"
          className="text-xs text-slate-400 hover:text-slate-200"
          onClick={() => setShow((s) => !s)}
        >
          {show ? "hide" : "show"}
        </button>
      )}
    </label>
  );

  const sharedProps = {
    id: field.key,
    name: field.key,
    value,
    placeholder: field.placeholder ?? undefined,
    required: field.isRequired,
    disabled,
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      onChange(e.target.value),
    className:
      "block w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 focus:border-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30",
  };

  let input: React.ReactNode;
  if (field.fieldType === "MultiLine") {
    input = <textarea rows={4} {...sharedProps} />;
  } else {
    const type =
      field.fieldType === "Password" && !show
        ? "password"
        : field.fieldType === "Url"
          ? "url"
          : "text";
    input = <input type={type} autoComplete="off" {...sharedProps} />;
  }

  return (
    <div>
      {label}
      {input}
      {field.helpText && (
        <p className="mt-1 text-xs text-slate-500">{field.helpText}</p>
      )}
    </div>
  );
}
