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
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th>Proveedor lider</th>
                <th>Mejor precio</th>
                <th>Promedio</th>
                <th>Maximo</th>
                <th>Spread</th>
              </tr>
            </thead>
            <tbody>
              {comparisons.map((item) => (
                <tr key={item.productId}>
                  <td>{item.productName}</td>
                  <td>{item.bestSupplier}</td>
                  <td>S/ {item.bestPrice.toFixed(2)}</td>
                  <td>S/ {item.averagePrice.toFixed(2)}</td>
                  <td>S/ {item.highestPrice.toFixed(2)}</td>
                  <td>{item.spreadPercentage.toFixed(2)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
