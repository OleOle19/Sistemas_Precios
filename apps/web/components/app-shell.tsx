import Link from "next/link";
import type { ReactNode } from "react";

const navigation = [
  { href: "/", label: "Dashboard" },
  { href: "/suppliers", label: "Proveedores" },
  { href: "/documents", label: "Documentos" },
  { href: "/comparisons", label: "Comparaciones" },
  { href: "/history", label: "Historial" },
  { href: "/login", label: "Login" }
] as const;

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">Portfolio System</p>
          <h1>Sistema de Precios</h1>
          <p className="muted">
            OCR, matching y comparacion historica para proveedores.
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
          <strong>Stack</strong>
          <span>Next.js + .NET + Rust + PostgreSQL</span>
        </div>
      </aside>
      <main className="content">{children}</main>
    </div>
  );
}
