import {
  mockComparisons,
  mockDashboard,
  mockDocumentDetail,
  mockDocuments,
  mockHistory,
  mockSuppliers
} from "./mock-data";
import type {
  ComparisonHistoryItem,
  CurrentComparison,
  DashboardSummary,
  DocumentDetail,
  DocumentSummary,
  Supplier
} from "./types";

const API_BASE_URL =
  process.env.API_BASE_URL ??
  process.env.NEXT_PUBLIC_API_BASE_URL ??
  "http://localhost:8080";

async function requestJson<T>(path: string, init?: RequestInit, fallback?: T): Promise<T> {
  try {
    const response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      cache: "no-store",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        ...(init?.headers ?? {})
      }
    });

    if (!response.ok) {
      throw new Error(`Request failed with status ${response.status}`);
    }

    return (await response.json()) as T;
  } catch {
    if (fallback !== undefined) {
      return fallback;
    }

    throw new Error(`No se pudo obtener ${path}`);
  }
}

export async function getDashboardSummary(): Promise<DashboardSummary> {
  return requestJson("/dashboard/summary", undefined, mockDashboard);
}

export async function getSuppliers(): Promise<Supplier[]> {
  return requestJson("/suppliers", undefined, mockSuppliers);
}

export async function getDocuments(): Promise<DocumentSummary[]> {
  return requestJson("/documents", undefined, mockDocuments);
}

export async function getDocumentDetail(id: string): Promise<DocumentDetail> {
  return requestJson(`/documents/${id}`, undefined, {
    ...mockDocumentDetail,
    id
  });
}

export async function getCurrentComparisons(): Promise<CurrentComparison[]> {
  return requestJson("/comparisons/current", undefined, mockComparisons);
}

export async function getComparisonHistory(): Promise<ComparisonHistoryItem[]> {
  return requestJson("/comparisons/history", undefined, mockHistory);
}
