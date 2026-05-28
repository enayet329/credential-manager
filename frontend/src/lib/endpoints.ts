// Thin typed wrappers over each backend endpoint.
import { api } from "./api";
import type {
  CredentialAccessDto,
  CredentialFieldSchema,
  CredentialMetadataDto,
  CredentialNoteDto,
  CredentialRotationDto,
  CredentialSchemaDto,
  CredentialValueResponse,
  CursorPage,
  EnvironmentDto,
  EnvironmentType,
  InviteMemberResponse,
  LoginResponse,
  MemberDto,
  ProjectDto,
  Role,
  StepUpResponse,
  SupplierDto,
  SupplierType,
} from "./types";

export const Auth = {
  login: (email: string, password: string) =>
    api.post<LoginResponse>("/auth/login", { email, password }).then((r) => r.data),
  register: (body: { email: string; password: string; workspaceName?: string }) =>
    api.post<LoginResponse>("/auth/register", body).then((r) => r.data),
  changePassword: (body: { currentPassword: string; newPassword: string }) =>
    api.post<void>("/auth/password", body),
  stepUp: (mfaCode: string) =>
    api.post<StepUpResponse>("/auth/step-up", { mfaCode }).then((r) => r.data),
};

export const Schemas = {
  list: (orgSlug: string) =>
    api
      .get<CredentialSchemaDto[]>(`/api/v1/orgs/${orgSlug}/supplier-schemas`)
      .then((r) => r.data),
  get: (orgSlug: string, supplierType: string) =>
    api
      .get<CredentialSchemaDto>(`/api/v1/orgs/${orgSlug}/supplier-schemas/${supplierType}`)
      .then((r) => r.data),
  register: (body: {
    supplierType: SupplierType;
    version: number;
    fields: CredentialFieldSchema[];
  }) =>
    api.post<CredentialSchemaDto>("/api/admin/credential-schemas", body).then((r) => r.data),
};

export const Members = {
  list: (orgSlug: string) =>
    api.get<MemberDto[]>(`/api/v1/orgs/${orgSlug}/members`).then((r) => r.data),
  invite: (orgSlug: string, body: { email: string; role: Role; initialPassword?: string }) =>
    api
      .post<InviteMemberResponse>(`/api/v1/orgs/${orgSlug}/members`, body)
      .then((r) => r.data),
  updateRole: (orgSlug: string, userId: string, role: Role) =>
    api
      .patch<MemberDto>(`/api/v1/orgs/${orgSlug}/members/${userId}`, { role })
      .then((r) => r.data),
  remove: (orgSlug: string, userId: string) =>
    api.delete<void>(`/api/v1/orgs/${orgSlug}/members/${userId}`),
};

export const Suppliers = {
  list: (orgSlug: string) =>
    api.get<SupplierDto[]>(`/api/v1/orgs/${orgSlug}/suppliers`).then((r) => r.data),
  create: (orgSlug: string, supplierType: SupplierType, displayName: string) =>
    api
      .post<SupplierDto>(`/api/v1/orgs/${orgSlug}/suppliers`, { supplierType, displayName })
      .then((r) => r.data),
  patch: (orgSlug: string, id: string, body: { displayName?: string; isActive?: boolean }) =>
    api.patch<SupplierDto>(`/api/v1/orgs/${orgSlug}/suppliers/${id}`, body).then((r) => r.data),
  remove: (orgSlug: string, id: string) =>
    api.delete<void>(`/api/v1/orgs/${orgSlug}/suppliers/${id}`),
};

export const Projects = {
  list: (orgSlug: string) =>
    api.get<ProjectDto[]>(`/api/v1/orgs/${orgSlug}/projects`).then((r) => r.data),
  get: (orgSlug: string, projectSlug: string) =>
    api
      .get<ProjectDto>(`/api/v1/orgs/${orgSlug}/projects/${projectSlug}`)
      .then((r) => r.data),
  create: (orgSlug: string, body: { name: string; slug: string; description?: string }) =>
    api.post<ProjectDto>(`/api/v1/orgs/${orgSlug}/projects`, body).then((r) => r.data),
  remove: (orgSlug: string, projectSlug: string) =>
    api.delete<void>(`/api/v1/orgs/${orgSlug}/projects/${projectSlug}`),
};

export const Environments = {
  list: (orgSlug: string, projectSlug: string) =>
    api
      .get<EnvironmentDto[]>(
        `/api/v1/orgs/${orgSlug}/projects/${projectSlug}/environments`,
      )
      .then((r) => r.data),
  create: (
    orgSlug: string,
    projectSlug: string,
    body: { name: string; slug: string; type: EnvironmentType },
  ) =>
    api
      .post<EnvironmentDto>(
        `/api/v1/orgs/${orgSlug}/projects/${projectSlug}/environments`,
        body,
      )
      .then((r) => r.data),
  remove: (orgSlug: string, projectSlug: string, envSlug: string) =>
    api.delete<void>(
      `/api/v1/orgs/${orgSlug}/projects/${projectSlug}/environments/${envSlug}`,
    ),
};

export const Credentials = {
  list: (
    orgSlug: string,
    filters: { project?: string; environment?: string; supplierId?: string } = {},
  ) => {
    const params: Record<string, string> = {};
    if (filters.project) params.project = filters.project;
    if (filters.environment) params.environment = filters.environment;
    if (filters.supplierId) params.supplierId = filters.supplierId;
    return api
      .get<CredentialMetadataDto[]>(`/api/v1/orgs/${orgSlug}/credentials`, { params })
      .then((r) => r.data);
  },
  get: (orgSlug: string, id: string) =>
    api
      .get<CredentialMetadataDto>(`/api/v1/orgs/${orgSlug}/credentials/${id}`)
      .then((r) => r.data),
  create: (
    orgSlug: string,
    projectSlug: string,
    envSlug: string,
    supplierId: string,
    body: {
      name: string;
      slug: string;
      fields: Record<string, string>;
      expiresAtUtc?: string;
    },
    stepUpToken: string,
  ) =>
    api
      .post<CredentialMetadataDto>(
        `/api/v1/orgs/${orgSlug}/projects/${projectSlug}/environments/${envSlug}/suppliers/${supplierId}/credentials`,
        body,
        { headers: { "X-Step-Up": stepUpToken } },
      )
      .then((r) => r.data),
  value: (orgSlug: string, id: string, stepUpToken?: string) =>
    api
      .get<CredentialValueResponse>(`/api/v1/orgs/${orgSlug}/credentials/${id}/value`, {
        headers: stepUpToken ? { "X-Step-Up": stepUpToken } : undefined,
      })
      .then((r) => r.data),
  rotate: (
    orgSlug: string,
    id: string,
    body: { fields: Record<string, string>; expiresAtUtc?: string; reason?: string },
    stepUpToken: string,
  ) =>
    api
      .post<CredentialMetadataDto>(
        `/api/v1/orgs/${orgSlug}/credentials/${id}/rotate`,
        body,
        { headers: { "X-Step-Up": stepUpToken } },
      )
      .then((r) => r.data),
  revoke: (orgSlug: string, id: string, stepUpToken: string) =>
    api
      .post<CredentialMetadataDto>(
        `/api/v1/orgs/${orgSlug}/credentials/${id}/revoke`,
        {},
        { headers: { "X-Step-Up": stepUpToken } },
      )
      .then((r) => r.data),
  remove: (orgSlug: string, id: string, stepUpToken: string) =>
    api.delete<void>(`/api/v1/orgs/${orgSlug}/credentials/${id}`, {
      headers: { "X-Step-Up": stepUpToken },
    }),
  rotations: (orgSlug: string, id: string) =>
    api
      .get<CredentialRotationDto[]>(`/api/v1/orgs/${orgSlug}/credentials/${id}/rotations`)
      .then((r) => r.data),
  exportBlob: (orgSlug: string, filters: { project?: string; environment?: string } = {}) => {
    const params: Record<string, string> = {};
    if (filters.project) params.project = filters.project;
    if (filters.environment) params.environment = filters.environment;
    return api
      .get(`/api/v1/orgs/${orgSlug}/credentials/export`, {
        params,
        responseType: "blob",
      })
      .then((r) => r.data as Blob);
  },
  share: (
    orgSlug: string,
    id: string,
    body: { expiresInMinutes?: number; allowReveal?: boolean; recipientEmail?: string },
  ) =>
    api
      .post<{ shareUrl: string; expiresAtUtc: string; allowReveal: boolean }>(
        `/api/v1/orgs/${orgSlug}/credentials/${id}/share`,
        body,
      )
      .then((r) => r.data),
  accessLog: (
    orgSlug: string,
    id: string,
    params: { actorType?: "User" | "ServiceToken"; limit?: number; cursor?: string } = {},
  ) =>
    api
      .get<CursorPage<CredentialAccessDto>>(
        `/api/v1/orgs/${orgSlug}/credentials/${id}/access-log`,
        { params },
      )
      .then((r) => r.data),
};

export const Notes = {
  list: (orgSlug: string, credentialId: string) =>
    api
      .get<CredentialNoteDto[]>(`/api/v1/orgs/${orgSlug}/credentials/${credentialId}/notes`)
      .then((r) => r.data),
  create: (orgSlug: string, credentialId: string, content: string) =>
    api
      .post<CredentialNoteDto>(
        `/api/v1/orgs/${orgSlug}/credentials/${credentialId}/notes`,
        { content },
      )
      .then((r) => r.data),
  remove: (orgSlug: string, credentialId: string, noteId: string) =>
    api.delete<void>(
      `/api/v1/orgs/${orgSlug}/credentials/${credentialId}/notes/${noteId}`,
    ),
};

export const AccessLog = {
  org: (
    orgSlug: string,
    params: { credentialId?: string; actorId?: string; limit?: number; cursor?: string } = {},
  ) =>
    api
      .get<CursorPage<CredentialAccessDto>>(`/api/v1/orgs/${orgSlug}/access-log`, { params })
      .then((r) => r.data),
};
