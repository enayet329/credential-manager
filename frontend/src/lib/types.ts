// Mirrors the API DTOs. Keep in sync with backend/src/CredVault.Api/Contracts/*.cs.

export type Role = "Owner" | "Admin" | "Developer" | "Viewer";

export type FieldType = "Text" | "Password" | "Url" | "MultiLine";

export type SupplierType =
  | "OpenAI"
  | "Anthropic"
  | "AzureOpenAI"
  | "AwsCredentials"
  | "GcpCredentials"
  | "AzureCredentials"
  | "Stripe"
  | "GitHub"
  | "GitLab"
  | "Postgres"
  | "MySql"
  | "MongoDb"
  | "Redis"
  | "GenericApiKey"
  | "Custom";

export type EnvironmentType = "Development" | "Uat" | "Staging" | "Production" | "Custom";

export type ActorType = "User" | "ServiceToken";

export type AccessMethod = "UI" | "Cli" | "ServiceTokenApi";

export type AccessOutcome = "Success" | "Denied";

// ─── Auth ──────────────────────────────────────────────────────────────────

export interface LoginOrganization {
  id: string;
  slug: string;
  name: string;
  role: Role;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  email: string;
  permissions: string[];
  organizations: LoginOrganization[];
}

export interface StepUpResponse {
  stepUpToken: string;
  expiresAtUtc: string;
}

// ─── Domain DTOs ───────────────────────────────────────────────────────────

export interface CredentialFieldSchema {
  key: string;
  displayName: string;
  fieldType: FieldType;
  isRequired: boolean;
  isSecret: boolean;
  placeholder?: string | null;
  validationRegex?: string | null;
  helpText?: string | null;
}

export interface CredentialSchemaDto {
  supplierType: SupplierType;
  version: number;
  fields: CredentialFieldSchema[];
}

export interface ProjectDto {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  createdAtUtc: string;
}

export interface EnvironmentDto {
  id: string;
  name: string;
  slug: string;
  type: EnvironmentType;
  createdAtUtc: string;
}

export interface SupplierDto {
  id: string;
  supplierType: SupplierType;
  displayName: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CredentialMetadataDto {
  id: string;
  supplierId: string;
  supplierType: SupplierType;
  environmentId: string;
  name: string;
  slug: string;
  maskedPreview: string;
  credentialSchemaVersion: number;
  kekVersion: number;
  createdAtUtc: string;
  rotatedAtUtc: string;
  expiresAtUtc?: string | null;
  lastAccessedAtUtc?: string | null;
  accessCount: number;
  isRevoked: boolean;
  revokedAtUtc?: string | null;
}

export interface CredentialAccessDto {
  id: string;
  credentialId: string;
  accessedAtUtc: string;
  actorType: ActorType;
  actorId: string;
  ipAddress: string;
  userAgent: string;
  accessMethod: AccessMethod;
  outcome: AccessOutcome;
}

export interface CredentialValueResponse {
  fields: Record<string, string>;
  access: CredentialAccessDto;
}

export interface CredentialRotationDto {
  id: string;
  credentialId: string;
  rotatedAtUtc: string;
  rotatedByUserId: string;
  previousKekVersion: number;
  reason?: string | null;
}

export interface MemberDto {
  userId: string;
  email: string;
  role: Role;
  joinedAtUtc: string;
  emailConfirmed: boolean;
}

export interface InviteMemberResponse {
  member: MemberDto;
  userCreated: boolean;
  temporaryPassword?: string | null;
}

export type StoredAuth = LoginResponse;

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
}
