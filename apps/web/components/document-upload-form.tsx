"use client";

import { useState } from "react";
import type { Supplier } from "../lib/types";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080";

export function DocumentUploadForm({ suppliers }: { suppliers: Supplier[] }) {
  const [message, setMessage] = useState<string>("Aun no se envio ningun archivo.");

  async function handleSubmit(formData: FormData) {
    const supplierId = formData.get("supplierId");
    const file = formData.get("file");

    if (!supplierId || !(file instanceof File) || file.size === 0) {
      setMessage("Selecciona un proveedor y un archivo valido.");
      return;
    }

    try {
      const payload = new FormData();
      payload.append("supplierId", String(supplierId));
      payload.append("file", file);

      const response = await fetch(`${apiBaseUrl}/documents`, {
        method: "POST",
        body: payload,
        credentials: "include"
      });

      if (!response.ok) {
        if (response.status === 401) {
          setMessage("La sesion no esta activa. Inicia sesion y vuelve a intentar.");
          return;
        }

        const responseText = await response.text();
        setMessage(`La API respondio con error ${response.status}. ${responseText.slice(0, 180)}`);
        return;
      }

      setMessage(`Archivo ${file.name} enviado. El backend lo pondra en cola.`);
    } catch {
      setMessage("No se pudo conectar con la API. Verifica que el backend este corriendo en localhost:8080.");
    }
  }

  return (
    <form action={handleSubmit} className="card form-card">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Ingreso</p>
          <h2>Subir lista de precios</h2>
        </div>
      </div>
      <label className="field">
        <span>Proveedor</span>
        <select name="supplierId" defaultValue={suppliers[0]?.id}>
          {suppliers.map((supplier) => (
            <option key={supplier.id} value={supplier.id}>
              {supplier.name}
            </option>
          ))}
        </select>
      </label>
      <label className="field">
        <span>Foto o PDF</span>
        <input name="file" type="file" accept="image/*,application/pdf" />
      </label>
      <label className="field">
        <span>Texto de apoyo OCR (opcional)</span>
        <textarea
          name="ocrText"
          rows={8}
          placeholder="Pega aqui las lineas esperadas del documento para una prueba controlada."
        />
      </label>
      <button type="submit" className="primary-button">
        Enviar a procesamiento
      </button>
      <p className="muted">{message}</p>
    </form>
  );
}
