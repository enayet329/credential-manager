import axios, { AxiosError } from "axios";
import type { ApiProblem } from "./types";

export function asProblem(err: unknown): ApiProblem | null {
  if (!axios.isAxiosError(err)) return null;
  const axiosError = err as AxiosError<ApiProblem>;
  return axiosError.response?.data ?? null;
}

export function isStepUpRequired(err: unknown): boolean {
  if (!axios.isAxiosError(err)) return false;
  const axiosError = err as AxiosError<ApiProblem>;
  if (axiosError.response?.status !== 403) return false;
  return axiosError.response.data?.code === "step_up_required";
}

export function describeError(err: unknown, fallback: string): string {
  const problem = asProblem(err);
  if (problem?.detail) return problem.detail;
  if (problem?.title) return problem.title;
  if (axios.isAxiosError(err) && !err.response) return "Cannot reach the API.";
  return fallback;
}
