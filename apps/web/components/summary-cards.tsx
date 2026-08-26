import type { DashboardSummary } from "../lib/types";

export function SummaryCards({ summary }: { summary: DashboardSummary }) {
  const cards = [
    { label: "Documentos procesados", value: summary.documentsProcessed },
    { label: "Proveedores comparados", value: summary.suppliersCompared },
    { label: "Pendientes de revision", value: summary.pendingReviews },
    { label: "Productos trazados", value: summary.productsTracked }
  ];

  return (
    <section className="grid cards-grid">
      {cards.map((card) => (
        <article key={card.label} className="card stat-card">
          <p className="muted">{card.label}</p>
          <strong className="stat-value">{card.value}</strong>
        </article>
      ))}
    </section>
  );
}
