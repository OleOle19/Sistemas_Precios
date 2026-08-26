"use client";

import { useState } from "react";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080";

export default function LoginPage() {
  const [message, setMessage] = useState("Usa las credenciales seed del backend.");

  async function handleSubmit(formData: FormData) {
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    try {
      const response = await fetch(`${apiBaseUrl}/auth/login`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ email, password })
      });

      if (!response.ok) {
        setMessage("No se pudo iniciar sesion. Revisa credenciales o el estado de la API.");
        return;
      }

      setMessage("Sesion iniciada. Ya puedes usar los endpoints protegidos.");
    } catch {
      setMessage("No se pudo conectar con la API. Verifica que el backend este corriendo en localhost:8080.");
    }
  }

  return (
    <div className="centered-page">
      <form action={handleSubmit} className="card auth-card">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Acceso interno</p>
            <h2>Iniciar sesion</h2>
          </div>
        </div>
        <label className="field">
          <span>Email</span>
          <input name="email" type="email" defaultValue="admin@precios.local" />
        </label>
        <label className="field">
          <span>Password</span>
          <input name="password" type="password" defaultValue="Admin123!" />
        </label>
        <button type="submit" className="primary-button">
          Entrar
        </button>
        <p className="muted">{message}</p>
      </form>
    </div>
  );
}
