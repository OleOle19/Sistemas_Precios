export type Session = { id: string; fullName: string; email: string; role: "Admin" | "Analyst" | "Viewer"; workspaceName: string };
export const canEditPrices = (session: Session) => session.role === "Admin" || session.role === "Analyst";
export const roleLabel = (role: string) => ({ Admin: "Administrador", Analyst: "Analista", Viewer: "Consulta", Disabled: "Sin acceso" }[role] ?? role);
