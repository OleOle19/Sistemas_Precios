import { redirect } from "next/navigation";
import { getTeamUsers, requireSession } from "../../../lib/api";
import { TeamForm } from "../../../components/team-form";

export const dynamic = "force-dynamic";
export default async function TeamPage() {
  const session = await requireSession();
  if (session.role !== "Admin") redirect("/");
  const users = await getTeamUsers();
  return <div className="page-stack">
    <section className="section-heading"><div><p className="eyebrow">{session.workspaceName}</p><h2>Equipo</h2><p>Agrega cuentas para las personas que trabajan en este negocio.</p></div></section>
    <section className="card"><p><strong>Administrador:</strong> gestiona el equipo y trabaja con los precios.</p><p><strong>Analista:</strong> registra proveedores, sube archivos, revisa y aprueba precios.</p><p><strong>Consulta:</strong> ve documentos, comparaciones e historial.</p><p><strong>Sin acceso:</strong> la cuenta queda deshabilitada y sus sesiones dejan de funcionar.</p></section>
    <TeamForm users={users} currentId={session.id} />
  </div>;
}
