"use client";
import { useState } from "react";
import { submit } from "../lib/submit";

export function PasswordForm() {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  async function change(data: FormData) {
    setMessage("");
    if (data.get("newPassword") !== data.get("confirmPassword")) { setMessage("Las contraseñas nuevas no coinciden."); return; }
    setBusy(true);
    try {
      await submit("/auth/password", { currentPassword: String(data.get("currentPassword")), newPassword: String(data.get("newPassword")) });
      window.location.assign("/login");
    } catch (e) { setMessage((e as Error).message); setBusy(false); }
  }
  return <form action={change} className="card form-card">
    <h3>Cambiar contraseña</h3>
    <label className="field"><span>Contraseña actual</span><input name="currentPassword" type="password" required maxLength={500} autoComplete="current-password" /></label>
    <label className="field"><span>Nueva contraseña</span><input name="newPassword" type="password" required minLength={12} maxLength={200} autoComplete="new-password" /></label>
    <label className="field"><span>Confirmar nueva contraseña</span><input name="confirmPassword" type="password" required minLength={12} maxLength={200} autoComplete="new-password" /></label>
    <p className="muted">Se cerrarán tus sesiones anteriores. Vuelve a ingresar con la contraseña nueva.</p>
    <button className="primary-button" disabled={busy}>{busy ? "Guardando…" : "Guardar contraseña"}</button>
    <p role="alert">{message}</p>
  </form>;
}
