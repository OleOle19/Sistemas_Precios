import { SupplierForm } from "../../../components/supplier-form";
import { getSuppliers, requireSession } from "../../../lib/api";
import { canEditPrices } from "../../../lib/permissions";

export const dynamic = "force-dynamic";

export default async function SuppliersPage() {
  const [suppliers, session] = await Promise.all([getSuppliers(), requireSession()]);

  return (
    <div className="page-stack">
      <section className="section-heading">
        <div>
          <p className="eyebrow">Catalogo comercial</p>
          <h2>Proveedores</h2>
        </div>
      </section>
      {canEditPrices(session) && <SupplierForm />}
      <div className="grid cards-grid">
        {suppliers.map((supplier) => (
          <article key={supplier.id} className="card">
            <strong>{supplier.name}</strong>
            <p className="muted">
              {supplier.contactEmail ?? "Sin correo registrado"}
            </p>
            <p>{supplier.documentCount} documentos asociados</p>
          </article>
        ))}
      </div>
    </div>
  );
}
