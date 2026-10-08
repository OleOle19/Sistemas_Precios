"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { submit } from "../lib/submit";
import { roleLabel } from "../lib/permissions";

type Member = { id: string; fullName: string; email: string; role: string };
export function TeamForm({ users, currentId }: { users: Member[]; currentId: string }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  async function create(data: FormData) {
    setBusy(true); setMessage("");
    try {
      await submit("/team/users", { fullName: String(data.get("fullName")), email: String(data.get("email")), password: String(data.get("password")), role: String(data.get("role")) });
      setMessage("Cuenta creada. El integrante ya puede ingresar a este negocio."); router.refresh();
    } catch (e) { setMessage((e as Error).message); }
    finally { setBusy(false); }
  }
  async function changeRole(id: string, data: FormData) {
    setBusy(true); setMessage("");
    try { await submit(`/team/users/${id}/role`, { role: String(data.get("role")) }); setMessage("Acceso actualizado."); router.refresh(); }
    catch (e) { setMessage((e as Error).message); }
    finally { setBusy(false); }
  }
  return <div className="page-stack">
    <form action={create} className="card form-card">
      <h3>Agregar integrante</h3>
      <label className="field"><span>Nombre completo</span><input name="fullName" required maxLength={150} autoComplete="name" /></label>
      <label className="field"><span>Correo del integrante</span><input name="email" type="email" required maxLength={250} autoComplete="off" /></label>
      <label className="field"><span>Contraseña inicial</span><input name="password" type="password" required minLength={12} maxLength={200} autoComplete="new-password" /></label>
      <label className="field"><span>Rol del integrante</span><select name="role" defaultValue="Analyst"><option value="Analyst">Analista</option><option value="Viewer">Consulta</option><option value="Admin">Administrador</option></select></label>
      <p className="muted">Todos los integrantes comparten proveedores, documentos, precios e historial. La persona puede cambiar su contraseña desde Mi cuenta.</p>
      <button className="primary-button" disabled={busy}>{busy ? "Guardando…" : "Crear cuenta"}</button>
    </form>
    <section className="card">
      <h3>Integrantes del negocio</h3>
      <div className="table-wrapper"><table><thead><tr><th>Nombre</th><th>Correo</th><th>Rol y acceso</th></tr></thead><tbody>
        {users.map(user => <tr key={`${user.id}-${user.role}`}><td>{user.fullName}</td><td>{user.email}</td><td>
          {user.id === currentId ? <span>{roleLabel(user.role)} · Tu cuenta</span> : <form action={data => changeRole(user.id, data)}>
            <select name="role" aria-label={`Rol de ${user.email}`} defaultValue={user.role} disabled={busy}>
              {["Admin", "Analyst", "Viewer", "Disabled"].map(role => <option key={role} value={role}>{roleLabel(role)}</option>)}
            </select>
            <button type="submit" className="nav-link" disabled={busy} aria-label={`Guardar acceso de ${user.email}`}>Guardar acceso</button>
          </form>}
        </td></tr>)}
      </tbody></table></div>
    </section>
    <p role="status">{message}</p>
  </div>;
}
