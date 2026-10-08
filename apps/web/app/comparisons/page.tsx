import { money } from "../../lib/display";
import { getCurrentComparisons } from "../../lib/api";

export const dynamic = "force-dynamic";

export default async function ComparisonsPage() {
  const comparisons = await getCurrentComparisons();

  return (
    <div className="page-stack">
      <section className="card">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Comparacion actual</p>
            <h2>Ranking por producto</h2>
          </div>
        </div>
        <p className="muted">
          Último precio observado de cada proveedor por producto equivalente,
          moneda y unidad base. No se convierten monedas. La diferencia relativa
          es (máximo − mínimo) / promedio.
        </p>
        {comparisons.length === 0 && <p>Aún no hay precios aprobados.</p>}
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th>Proveedor lider</th>
                <th>Mejor precio</th>
                <th>Promedio</th>
                <th>Maximo</th>
                <th>Diferencia relativa</th>
                <th>Proveedores</th>
              </tr>
            </thead>
            <tbody>
              {comparisons.map((item) => (
                <tr key={`${item.productId}-${item.currency}`}>
                  <td>{item.productName}</td>
                  <td>{item.bestSupplier}</td>
                  <td>{money(item.bestPrice, item.currency, item.baseUnit)}</td>
                  <td>
                    {money(item.averagePrice, item.currency, item.baseUnit)}
                  </td>
                  <td>
                    {money(item.highestPrice, item.currency, item.baseUnit)}
                  </td>
                  <td>{item.spreadPercentage.toFixed(2)}%</td>
                  <td>
                    {item.supplierCount}
                    {item.supplierCount === 1 ? " (sin comparación)" : ""}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
