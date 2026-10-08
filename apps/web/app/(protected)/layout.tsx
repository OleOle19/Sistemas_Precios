import type { ReactNode } from "react";
import { AppShell } from "../../components/app-shell";
import { requireSession } from "../../lib/api";

export const dynamic = "force-dynamic";

export default async function ProtectedLayout({ children }: { children: ReactNode }) {
  const session = await requireSession();
  return <AppShell session={session}>{children}</AppShell>;
}
