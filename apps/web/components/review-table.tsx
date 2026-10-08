"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { DocumentDetail } from "../lib/types";
import { submit } from "../lib/submit";
import { statusLabel } from "../lib/display";
export function ReviewTable({ document }: { document: DocumentDetail }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [rows, setRows] = useState(() =>
    document.lines.map((l) => ({
      extractedLineId: l.id,
      canonicalName: l.approvedCanonicalProductName ?? l.suggestedName ?? "",
      unit: l.approvedUnit ?? l.suggestedUnit ?? "",
      quantity: String(l.approvedQuantity ?? l.suggestedQuantity ?? ""),
      price: String(l.approvedPrice ?? l.suggestedPrice ?? ""),
      currency: l.approvedCurrency ?? l.suggestedCurrency ?? "",
      excluded: l.excluded,
    })),
  );
  useEffect(() => {
    if (!["Uploaded", "Processing"].includes(document.status)) return;
    const timer = setInterval(() => router.refresh(), 2500);
    return () => clearInterval(timer);
  }, [document.status, router]);
  useEffect(() => {
    setRows(
      document.lines.map((l) => ({
        extractedLineId: l.id,
        canonicalName: l.approvedCanonicalProductName ?? l.suggestedName ?? "",
        unit: l.approvedUnit ?? l.suggestedUnit ?? "",
        quantity: String(l.approvedQuantity ?? l.suggestedQuantity ?? ""),
        price: String(l.approvedPrice ?? l.suggestedPrice ?? ""),
        currency: l.approvedCurrency ?? l.suggestedCurrency ?? "",
        excluded: l.excluded,
      })),
    );
  }, [document.id, document.lines]);
  const editable = document.status === "NeedsReview";
  function change(index: number, key: string, value: string | boolean) {
    setRows((old) =>
      old.map((r, i) => (i === index ? { ...r, [key]: value } : r)),
    );
  }
  async function approve(data: FormData) {
    if (data.get("confirmed") !== "on") return;
    setBusy(true);
    setMessage("");
    try {
      await submit(`/documents/${document.id}/review`, {
        lines: rows.map((r) => ({
          ...r,
          quantity: Number(r.quantity),
          price: Number(r.price),
        })),
      });
      setMessage("Precios aprobados. Ya están disponibles para comparar.");
      router.refresh();
    } catch (e) {
      setMessage((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function retry() {
    setBusy(true);
    try {
      await submit(`/documents/${document.id}/reprocess`);
      setMessage("Archivo enviado nuevamente para lectura.");
      router.refresh();
    } catch (e) {
      setMessage((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="card">
      <h2>{document.fileName}</h2>
      <p>
        {document.supplierName} · {statusLabel(document.status)} · Precio
        observado: {document.observedAt.slice(0, 10)}
      </p>
      <a
        className="file-link"
        href={`/api/documents/${document.id}/file`}
        target="_blank"
        rel="noreferrer"
      >
        Abrir archivo original para verificar
      </a>
      {document.contentType.startsWith("image/") && (
        <img
          className="document-preview"
          src={`/api/documents/${document.id}/file`}
          alt="Archivo original con precios"
        />
      )}
      {document.failureReason && <p role="alert">{document.failureReason}</p>}
      {["Uploaded", "Processing"].includes(document.status) && (
        <p role="status">
          Leyendo el archivo. Esta página se actualizará automáticamente.
        </p>
      )}
      {document.status === "Failed" && (
        <button disabled={busy} className="primary-button" onClick={retry}>
          Volver a leer (consume saldo de API)
        </button>
      )}
      {rows.length > 0 && (
        <form action={approve}>
          <p className="muted">
            Verifica cada valor con la foto. El precio es de una presentación;
            contenido indica cuántos KG, G, L, ML, M o unidades contiene. Usa el
            mismo nombre solo para productos equivalentes en marca, variante y
            calidad. Comprueba impuestos y condiciones comerciales antes de
            aprobar. Descarta filas que no sean precios regulares.
          </p>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Incluir</th>
                  <th>Evidencia</th>
                  <th>Producto equivalente</th>
                  <th>Contenido</th>
                  <th>Unidad</th>
                  <th>Precio presentación</th>
                  <th>Moneda</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r, i) => (
                  <tr key={r.extractedLineId}>
                    <td>
                      <input
                        aria-label={`Incluir fila ${i + 1}`}
                        type="checkbox"
                        checked={!r.excluded}
                        disabled={!editable || busy}
                        onChange={(e) =>
                          change(i, "excluded", !e.target.checked)
                        }
                      />
                    </td>
                    <td>{document.lines[i]?.rawText}</td>
                    <td>
                      <input
                        aria-label={`Producto fila ${i + 1}`}
                        value={r.canonicalName}
                        maxLength={250}
                        required={!r.excluded}
                        disabled={!editable || r.excluded || busy}
                        onChange={(e) =>
                          change(i, "canonicalName", e.target.value)
                        }
                      />
                    </td>
                    <td>
                      <input
                        aria-label={`Contenido fila ${i + 1}`}
                        type="number"
                        min="0.000001"
                        max="1000000"
                        step="any"
                        value={r.quantity}
                        required={!r.excluded}
                        disabled={!editable || r.excluded || busy}
                        onChange={(e) => change(i, "quantity", e.target.value)}
                      />
                    </td>
                    <td>
                      <select
                        aria-label={`Unidad fila ${i + 1}`}
                        value={r.unit}
                        required={!r.excluded}
                        disabled={!editable || r.excluded || busy}
                        onChange={(e) => change(i, "unit", e.target.value)}
                      >
                        <option value="">Seleccionar</option>
                        {["UN", "KG", "G", "L", "ML", "M"].map((u) => (
                          <option key={u}>{u}</option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        aria-label={`Precio fila ${i + 1}`}
                        type="number"
                        min="0.000001"
                        max="1000000"
                        step="any"
                        value={r.price}
                        required={!r.excluded}
                        disabled={!editable || r.excluded || busy}
                        onChange={(e) => change(i, "price", e.target.value)}
                      />
                    </td>
                    <td>
                      <select
                        aria-label={`Moneda fila ${i + 1}`}
                        value={r.currency}
                        required={!r.excluded}
                        disabled={!editable || r.excluded || busy}
                        onChange={(e) => change(i, "currency", e.target.value)}
                      >
                        <option value="">Seleccionar</option>
                        {["PEN", "USD", "EUR"].map((c) => (
                          <option key={c}>{c}</option>
                        ))}
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {editable && (
            <>
              <label className="confirmation">
                <input
                  type="checkbox"
                  name="confirmed"
                  required
                  disabled={busy}
                />{" "}
                Verifiqué los valores con el archivo y las condiciones del
                precio.
              </label>
              <button className="primary-button" disabled={busy}>
                {busy ? "Guardando…" : "Aprobar precios"}
              </button>
              <button type="button" disabled={busy} onClick={retry}>
                Volver a leer (consume saldo de API)
              </button>
            </>
          )}
        </form>
      )}
      <p role="status">{message}</p>
    </div>
  );
}
