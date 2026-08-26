import { getComparisonHistory } from "../../lib/api";

export const dynamic = "force-dynamic";

export default async function HistoryPage() {
  const history = await getComparisonHistory();

  return (
    <div className="page-stack">
      <section className="card">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Tendencias</p>
            <h2>Historial de precios</h2>
          </div>
        </div>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th>Proveedor</th>
                <th>Precio</th>
                <th>Fecha</th>
                <th>Variacion</th>
              </tr>
            </thead>
            <tbody>
              {history.map((item, index) => (
                <tr key={`${item.productName}-${index}`}>
                  <td>{item.productName}</td>
                  <td>{item.supplierName}</td>
                  <td>S/ {item.price.toFixed(2)}</td>
                  <td>{new Date(item.effectiveAt).toLocaleDateString("es-PE")}</td>
                  <td>{item.variationPercentage?.toFixed(2) ?? "N/A"}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
