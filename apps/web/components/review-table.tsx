"use client";

import { useMemo, useState } from "react";
import type { DocumentDetail } from "../lib/types";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080";

export function ReviewTable({ document }: { document: DocumentDetail }) {
  const [message, setMessage] = useState("Corrige y aprueba las lineas con baja confianza.");
  const suggestedPayload = useMemo(
    () =>
      document.lines.map((line) => ({
        extractedLineId: line.id,
        canonicalName: line.approvedCanonicalProductName ?? line.suggestedName ?? line.rawText,
        unit: line.approvedUnit ?? line.suggestedUnit ?? "UN",
        quantity: line.approvedQuantity ?? line.suggestedQuantity ?? 1,
        price: line.approvedPrice ?? line.suggestedPrice ?? 0
      })),
    [document.lines]
  );

  async function approveDocument() {
    try {
      const response = await fetch(`${apiBaseUrl}/documents/${document.id}/review`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ lines: suggestedPayload })
      });

      if (!response.ok) {
        throw new Error("review failed");
      }

      setMessage("Documento aprobado y snapshots recalculados.");
    } catch {
      setMessage(
        "Modo demo: la tabla y el payload de revision ya estan listos, pero la API no estaba disponible."
      );
    }
  }

  return (
    <div className="card">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Revision humana</p>
          <h2>{document.fileName}</h2>
        </div>
        <button type="button" className="primary-button" onClick={approveDocument}>
          Aprobar documento
        </button>
      </div>
      <div className="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>#</th>
              <th>Texto OCR</th>
              <th>Producto sugerido</th>
              <th>Precio</th>
              <th>Confianza</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            {document.lines.map((line) => (
              <tr key={line.id}>
                <td>{line.lineNumber}</td>
                <td>{line.rawText}</td>
                <td>{line.approvedCanonicalProductName ?? line.suggestedName ?? "Sin match"}</td>
                <td>S/ {(line.approvedPrice ?? line.suggestedPrice ?? 0).toFixed(2)}</td>
                <td>{Math.round(line.confidenceScore * 100)}%</td>
                <td>
                  <span className={line.needsReview ? "badge warning" : "badge success"}>
                    {line.needsReview ? "Revisar" : "Listo"}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="muted">{message}</p>
    </div>
  );
}
