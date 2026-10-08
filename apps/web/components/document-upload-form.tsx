"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import type { Supplier } from "../lib/types";
import { submit } from "../lib/submit";
export function DocumentUploadForm({
  suppliers,
  configured,
  provider,
}: {
  suppliers: Supplier[];
  configured: boolean;
  provider: string;
}) {
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const router = useRouter();
  async function send(data: FormData) {
    const file = data.get("file");
    if (
      !(file instanceof File) ||
      file.size === 0 ||
      file.size > 10 * 1024 * 1024
    ) {
      setMessage("Selecciona una foto o PDF de hasta 10 MB.");
      return;
    }
    setBusy(true);
    try {
      const doc = await submit("/documents", data);
      router.push(`/documents/${doc.id}`);
      router.refresh();
    } catch (e) {
      setMessage((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form action={send} className="card form-card">
      <h2>Subir foto o cotización</h2>
      {!configured && (
        <p role="status">
          La lectura de fotos requiere configurar la clave de {provider} en el servidor.
        </p>
      )}
      {!suppliers.length && (
        <p>Registra primero un proveedor o tienda en Proveedores.</p>
      )}
      <label className="field">
        <span>Proveedor o tienda</span>
        <select name="supplierId" required>
          {suppliers.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
      </label>
      <label className="field">
        <span>Tipo de fuente</span>
        <select name="sourceKind">
          <option value="quotation">Lista o cotización</option>
          <option value="label">Etiqueta de tienda</option>
        </select>
      </label>
      <label className="field">
        <span>Fecha del precio observado</span>
        <input
          name="observedDate"
          type="date"
          required
          defaultValue={new Date().toLocaleDateString("sv-SE")}
          max={new Date().toLocaleDateString("sv-SE")}
        />
      </label>
      <label className="field">
        <span>Foto o PDF (máximo 10 MB)</span>
        <input
          name="file"
          type="file"
          required
          accept="image/jpeg,image/png,image/webp,application/pdf"
        />
      </label>
      <p className="muted">
        Se enviará este archivo a {provider} para leer sus precios. {provider === "Gemini"
          ? "Cada lectura utiliza la cuota del proyecto. En el nivel gratuito Google puede usar el contenido para mejorar sus productos; prueba con documentos ficticios."
          : "Cada lectura consume saldo de tu API."} Usa fotos enfocadas.
      </p>
      <button
        className="primary-button"
        disabled={busy || !configured || !suppliers.length}
      >
        {busy ? "Enviando…" : "Enviar para leer precios"}
      </button>
      <p role="status">{message}</p>
    </form>
  );
}
