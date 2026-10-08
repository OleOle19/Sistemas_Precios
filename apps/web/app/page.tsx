import { money } from "../lib/display";
import { SummaryCards } from "../components/summary-cards";
import { getDashboardSummary } from "../lib/api";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const summary = await getDashboardSummary();

  return (
    <div className="page-stack">
      <section className="hero-card">
        <div>
          <p className="eyebrow">Comparación de precios</p>
          <h2>Compara proveedores a partir de fotos y listas comerciales.</h2>
        </div>
        <p className="hero-copy">
          Sube una foto, verifica sus precios y compara productos equivalentes
          por unidad y moneda. Conserva el archivo original y el historial de
          cada proveedor.
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
            {!summary.bestPrices.length && (
              <p>Aún no hay precios de dos proveedores para comparar.</p>
            )}
            {summary.bestPrices.map((item) => (
              <div
                key={`${item.productName}-${item.supplierName}-${item.currency}-${item.unit}`}
                className="list-row"
              >
                <div>
                  <strong>{item.productName}</strong>
                  <p className="muted">{item.supplierName}</p>
                </div>
                <div className="numeric-block">
                  <strong>
                    {money(item.bestPrice, item.currency, item.unit)}
                  </strong>
                  <span>
                    {item.spreadPercentage.toFixed(1)}% de diferencia relativa
                  </span>
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
            {!summary.biggestMovers.length && (
              <p>
                Las variaciones aparecerán cuando registres nuevas
                observaciones.
              </p>
            )}
            {summary.biggestMovers.map((item) => (
              <div
                key={`${item.productName}-${item.supplierName}-${item.currency}-${item.unit}`}
                className="list-row"
              >
                <div>
                  <strong>{item.productName}</strong>
                  <p className="muted">{item.supplierName}</p>
                </div>
                <div className="numeric-block">
                  <strong>{item.variationPercentage.toFixed(2)}%</strong>
                  <span>
                    {money(item.previousPrice, item.currency, item.unit)} →{" "}
                    {money(item.currentPrice, item.currency, item.unit)}
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
