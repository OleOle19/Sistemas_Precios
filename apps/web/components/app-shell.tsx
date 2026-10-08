import Link from "next/link";
import { LogoutButton } from "./logout-button";
import type { ReactNode } from "react";

const navigation = [
  { href: "/", label: "Resumen" },
  { href: "/suppliers", label: "Proveedores" },
  { href: "/documents", label: "Documentos" },
  { href: "/comparisons", label: "Comparaciones" },
  { href: "/history", label: "Historial" },
  { href: "/login", label: "Ingresar" },
] as const;

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">Compras informadas</p>
          <h1>Sistema de Precios</h1>
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
        </nav>
        <div className="sidebar-card">
          <strong>Verifica antes de decidir</strong>
          <span>Los precios aprobados conservan el documento original.</span>
          <LogoutButton />
        </div>
      </aside>
      <main className="content">{children}</main>
    </div>
  );
}
