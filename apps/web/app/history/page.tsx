import { money } from "../../lib/display";
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
        {history.length === 0 && <p>Aún no hay precios aprobados.</p>}
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
                  <td>{money(item.price, item.currency, item.unit)}</td>
                  <td>{item.effectiveAt.slice(0, 10)}</td>
                  <td>
                    {item.variationPercentage == null
                      ? "Sin precio anterior"
                      : `${item.variationPercentage.toFixed(2)}%`}
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
