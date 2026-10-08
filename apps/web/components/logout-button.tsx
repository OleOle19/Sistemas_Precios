"use client";
import { submit } from "../lib/submit";
import { usePathname } from "next/navigation";
export function LogoutButton() {
  const pathname = usePathname();
  if (pathname === "/login") return null;
  return (
    <button
      className="nav-link"
      onClick={async () => {
        await submit("/auth/logout");
        window.location.assign("/login");
      }}
    >
      Cerrar sesión
    </button>
  );
}
