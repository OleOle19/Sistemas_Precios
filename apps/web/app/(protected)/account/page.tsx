import { requireSession } from "../../../lib/api";
import { roleLabel } from "../../../lib/permissions";
import { PasswordForm } from "../../../components/password-form";

export const dynamic = "force-dynamic";
export default async function AccountPage() {
  const session = await requireSession();
  return <div className="page-stack">
    <section className="card"><h2>Mi cuenta</h2><p>{session.fullName} · {session.email}</p><p>Negocio: {session.workspaceName}</p><p>Rol: {roleLabel(session.role)}</p></section>
    <PasswordForm />
  </div>;
}
