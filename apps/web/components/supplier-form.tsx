"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { submit } from "../lib/submit";
export function SupplierForm() {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const router = useRouter();
  async function create(data: FormData) {
    setBusy(true);
    try {
      await submit("/suppliers", {
        name: String(data.get("name")),
        contactEmail: String(data.get("email")) || null,
      });
      setMessage("Proveedor registrado.");
      router.refresh();
    } catch (e) {
      setMessage((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form action={create} className="card form-card">
      <h2>Registrar proveedor o tienda</h2>
      <label className="field">
        <span>Nombre</span>
        <input name="name" required maxLength={250} />
      </label>
      <label className="field">
        <span>Correo (opcional)</span>
        <input name="email" type="email" maxLength={250} />
      </label>
      <button className="primary-button" disabled={busy}>
        {busy ? "Guardando…" : "Registrar"}
      </button>
      <p role="status">{message}</p>
    </form>
  );
}
