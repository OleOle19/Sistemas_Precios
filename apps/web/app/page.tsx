import { SummaryCards } from "../components/summary-cards";
import { getDashboardSummary } from "../lib/api";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const summary = await getDashboardSummary();

  return (
    <div className="page-stack">
      <section className="hero-card">
        <div>
          <p className="eyebrow">Decision intelligence</p>
          <h2>Compara proveedores a partir de fotos y listas comerciales.</h2>
        </div>
        <p className="hero-copy">
          El flujo combina OCR, matching automatico y trazabilidad historica para detectar
          mejores precios, variaciones y documentos pendientes de validacion.
        </p>
      </section>

      <SummaryCards summary={summary} />

      <section className="grid split-grid">
        <article className="card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Mejores precios</p>
              <h2>Oportunidades actuales</h2>
            </div>
          </div>
          <div className="list-stack">
            {summary.bestPrices.map((item) => (
              <div key={`${item.productName}-${item.supplierName}`} className="list-row">
                <div>
                  <strong>{item.productName}</strong>
                  <p className="muted">{item.supplierName}</p>
                </div>
                <div className="numeric-block">
                  <strong>S/ {item.bestPrice.toFixed(2)}</strong>
                  <span>{item.spreadPercentage.toFixed(1)}% spread</span>
                </div>
              </div>
            ))}
          </div>
        </article>

        <article className="card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Alertas</p>
              <h2>Movimientos fuertes</h2>
            </div>
          </div>
          <div className="list-stack">
            {summary.biggestMovers.map((item) => (
              <div key={`${item.productName}-${item.supplierName}`} className="list-row">
                <div>
                  <strong>{item.productName}</strong>
                  <p className="muted">{item.supplierName}</p>
                </div>
                <div className="numeric-block">
                  <strong>{item.variationPercentage.toFixed(2)}%</strong>
                  <span>
                    S/ {item.previousPrice.toFixed(2)} → S/ {item.currentPrice.toFixed(2)}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </article>
      </section>
    </div>
  );
}
