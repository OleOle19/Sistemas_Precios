import { cookies } from "next/headers";
import { redirect, notFound } from "next/navigation";
import { cache } from "react";
import type { Session } from "./permissions";
import type {
  ComparisonHistoryItem,
  CurrentComparison,
  DashboardSummary,
  DocumentDetail,
  DocumentSummary,
  Supplier,
} from "./types";
const base = process.env.API_BASE_URL ?? "http://localhost:8080";
export const requireSession = cache(() => requestJson<Session>("/auth/me"));
export const getTeamUsers = () => requestJson<{ id: string; fullName: string; email: string; role: string }[]>("/team/users");
async function requestJson<T>(path: string): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${base}${path}`, {
      cache: "no-store",
      headers: { cookie: (await cookies()).toString() },
      signal: AbortSignal.timeout(10000),
    });
  } catch {
    throw new Error("El servicio no está disponible. Intenta nuevamente.");
  }
  if (response.status === 401) redirect("/login");
  if (response.status === 404) notFound();
  if (!response.ok)
    throw new Error("No se pudieron cargar los datos. Intenta nuevamente.");
  return response.json() as Promise<T>;
}
export const getDashboardSummary = () =>
  requestJson<DashboardSummary>("/dashboard/summary");
export const getSuppliers = () => requestJson<Supplier[]>("/suppliers");
export const getDocuments = () => requestJson<DocumentSummary[]>("/documents");
export const getDocumentDetail = (id: string) =>
  requestJson<DocumentDetail>(`/documents/${encodeURIComponent(id)}`);
export const getCurrentComparisons = () =>
  requestJson<CurrentComparison[]>("/comparisons/current");
export const getComparisonHistory = () =>
  requestJson<ComparisonHistoryItem[]>("/comparisons/history");
export const getExtractionSettings = () =>
  requestJson<{ configured: boolean; model: string; provider: string }>("/settings/extraction");
