"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { submit } from "../../lib/submit";
export default function LoginPage() {
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const router = useRouter();
  async function login(data: FormData) {
    setBusy(true);
    try {
      await submit("/auth/login", {
        email: String(data.get("email")),
        password: String(data.get("password")),
      });
      router.push("/");
      router.refresh();
    } catch {
      setMessage(
        "No se pudo iniciar sesión. Revisa tu correo, contraseña y que el servicio esté disponible.",
      );
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="centered-page">
      <form action={login} className="card auth-card">
        <h2>Iniciar sesión</h2>
        <label className="field">
          <span>Correo</span>
          <input name="email" type="email" required autoComplete="username" />
        </label>
        <label className="field">
          <span>Contraseña</span>
          <input
            name="password"
            type="password"
            required
            autoComplete="current-password"
          />
        </label>
        <button className="primary-button" disabled={busy}>
          {busy ? "Ingresando…" : "Entrar"}
        </button>
        <p role="alert">{message}</p>
      </form>
    </div>
  );
}
