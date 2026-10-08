import Link from "next/link";
import { LogoutButton } from "./logout-button";
import type { ReactNode } from "react";
import { roleLabel, type Session } from "../lib/permissions";

const navigation = [
  { href: "/", label: "Resumen" },
  { href: "/suppliers", label: "Proveedores" },
  { href: "/documents", label: "Documentos" },
  { href: "/comparisons", label: "Comparaciones" },
  { href: "/history", label: "Historial" },
  { href: "/account", label: "Mi cuenta" },
] as const;

export function AppShell({ children, session }: { children: ReactNode; session: Session }) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">Compras informadas</p>
          <h1>Sistema de Precios</h1>
          <strong>{session.workspaceName}</strong>
          <p className="muted">
            Compara precios verificados de proveedores y tiendas.
          </p>
        </div>
        <nav className="nav">
          {navigation.map((item) => (
            <Link key={item.href} href={item.href} className="nav-link">
              {item.label}
            </Link>
          ))}
          {session.role === "Admin" && <Link href="/team" className="nav-link">Equipo</Link>}
        </nav>
        <div className="sidebar-card">
          <strong>{session.fullName}</strong>
          <span>{session.email}</span>
          <span>{roleLabel(session.role)}</span>
          <LogoutButton />
        </div>
      </aside>
      <main className="content">{children}</main>
    </div>
  );
}
