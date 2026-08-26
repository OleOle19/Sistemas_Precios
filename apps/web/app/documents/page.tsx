import Link from "next/link";
import { DocumentUploadForm } from "../../components/document-upload-form";
import { getDocuments, getSuppliers } from "../../lib/api";

export const dynamic = "force-dynamic";

export default async function DocumentsPage() {
  const [suppliers, documents] = await Promise.all([getSuppliers(), getDocuments()]);

  return (
    <div className="page-stack">
      <DocumentUploadForm suppliers={suppliers} />

      <section className="card">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Pipeline</p>
            <h2>Documentos recientes</h2>
          </div>
        </div>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Proveedor</th>
                <th>Archivo</th>
                <th>Estado</th>
                <th>Lineas</th>
                <th>Subido</th>
              </tr>
            </thead>
            <tbody>
              {documents.map((document) => (
                <tr key={document.id}>
                  <td>{document.supplierName}</td>
                  <td>
                    <Link href={`/documents/${document.id}`}>{document.fileName}</Link>
                  </td>
                  <td>{document.status}</td>
                  <td>{document.extractedLineCount}</td>
                  <td>{new Date(document.uploadedAt).toLocaleString("es-PE")}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
